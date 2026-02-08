using System.Collections.Generic;
using Match3.Unity.Services;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// Builds a single combined board mesh from modular tile pieces (V2 algorithm).
    /// Vertex-based corner detection + boundary-run edge placement.
    /// Modules (5 FBX from Board_Tile_v2.blend):
    ///   Floor, Edge, CornerOuter, CornerInner, CornerInnerFloor.
    /// Uses 2 submeshes: submesh 0 = floor, submesh 1 = walls.
    /// </summary>
    public static class BoardMeshBuilder
    {
        private const string TilePath = "Art/Board/Models/";

        private static Mesh _floorMesh;
        private static Mesh _edgeMesh;
        private static Mesh _cornerOuterMesh;
        private static Mesh _cornerInnerMesh;
        private static Mesh _cornerInnerFloorMesh;

        private static Material[] _boardMaterials;

        // Must match Blender module geometry (Board_Tile_v2.blend)
        private const float WallThickness = 0.12f;

        /// <summary>
        /// Build a board mesh from a layout mask.
        /// layout[row, col]: true = cell exists.
        /// cellSize: world units per cell.
        /// origin: world position of the board's top-left corner.
        /// height: total rows (for Y-flip).
        /// </summary>
        public static Mesh Build(bool[,] layout, float cellSize, Vector2 origin, int height)
        {
            LoadModules();

            int rows = layout.GetLength(0);
            int cols = layout.GetLength(1);

            int cellCount = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (layout[r, c]) cellCount++;

            int estimatedVerts = cellCount * 40;
            var vertices = new List<Vector3>(estimatedVerts);
            var floorLightTris = new List<int>(estimatedVerts);
            var floorDarkTris = new List<int>(estimatedVerts);
            var wallTris = new List<int>(estimatedVerts);
            var normals = new List<Vector3>(estimatedVerts);
            var uvs = new List<Vector2>(estimatedVerts);

            // 1. Floor per cell (alternating light/dark for checkerboard)
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (layout[r, c])
                    {
                        var tris = (r + c) % 2 == 0 ? floorLightTris : floorDarkTris;
                        AppendMesh(vertices, tris, normals, uvs,
                            _floorMesh, CellCenter(r, c, cellSize, origin, height), 0f, cellSize);
                    }

            // 2. Vertex-based corners
            // Grid vertices (vr, vc) where vr in [0..rows], vc in [0..cols].
            // 4 adjacent cells: NE=(vr-1,vc), NW=(vr-1,vc-1), SE=(vr,vc), SW=(vr,vc-1)
            float halfCell = cellSize * 0.5f;
            for (int vr = 0; vr <= rows; vr++)
            {
                for (int vc = 0; vc <= cols; vc++)
                {
                    bool ne = CellExists(layout, vr - 1, vc, rows, cols);
                    bool nw = CellExists(layout, vr - 1, vc - 1, rows, cols);
                    bool se = CellExists(layout, vr, vc, rows, cols);
                    bool sw = CellExists(layout, vr, vc - 1, rows, cols);
                    int count = (ne ? 1 : 0) + (nw ? 1 : 0) + (se ? 1 : 0) + (sw ? 1 : 0);

                    if (count != 1 && count != 3) continue;

                    var vertPos = new Vector3(
                        origin.x + vc * cellSize,
                        origin.y + (height - vr) * cellSize,
                        0f);

                    if (count == 1)
                    {
                        // Outer corner at the single adjacent cell's center
                        Vector3 pos;
                        float angle;
                        if (sw)      { pos = vertPos + new Vector3(-halfCell, -halfCell, 0); angle = -90f; }
                        else if (se) { pos = vertPos + new Vector3(halfCell, -halfCell, 0);  angle = 0f; }
                        else if (nw) { pos = vertPos + new Vector3(-halfCell, halfCell, 0);  angle = 180f; }
                        else         { pos = vertPos + new Vector3(halfCell, halfCell, 0);   angle = 90f; }

                        AppendMesh(vertices, wallTris, normals, uvs,
                            _cornerOuterMesh, pos, angle, cellSize);
                    }
                    else // count == 3
                    {
                        // Inner corner + floor at vertex position
                        float angle;
                        if (!ne)      angle = -90f;
                        else if (!nw) angle = 0f;
                        else if (!se) angle = 180f;
                        else          angle = 90f; // !sw

                        AppendMesh(vertices, wallTris, normals, uvs,
                            _cornerInnerMesh, vertPos, angle, cellSize);
                        AppendMesh(vertices, floorLightTris, normals, uvs,
                            _cornerInnerFloorMesh, vertPos, angle, cellSize);
                    }
                }
            }

            // 3. Edge runs (only between adjacent cells on same boundary)
            PlaceEdgeRuns(layout, rows, cols, cellSize, origin, height,
                vertices, wallTris, normals, uvs);

            var mesh = new Mesh();
            mesh.name = "BoardMesh";
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(floorLightTris, 0);
            mesh.SetTriangles(floorDarkTris, 1);
            mesh.SetTriangles(wallTris, 2);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Build a rectangular board (no holes).
        /// </summary>
        public static Mesh BuildRectangular(int width, int height, float cellSize, Vector2 origin)
        {
            var layout = new bool[height, width];
            for (int r = 0; r < height; r++)
                for (int c = 0; c < width; c++)
                    layout[r, c] = true;

            return Build(layout, cellSize, origin, height);
        }

        /// <summary>
        /// Place edge modules between adjacent cells sharing the same boundary direction.
        /// Groups boundary faces by direction, finds consecutive runs, places edges at grid lines.
        /// </summary>
        private static void PlaceEdgeRuns(bool[,] layout, int rows, int cols,
            float cellSize, Vector2 origin, int height,
            List<Vector3> vertices, List<int> wallTris,
            List<Vector3> normals, List<Vector2> uvs)
        {
            float wallOff = WallThickness * 0.5f * cellSize;

            // North boundaries: row by row, find consecutive columns
            for (int r = 0; r < rows; r++)
            {
                var boundary = new List<int>();
                for (int c = 0; c < cols; c++)
                    if (layout[r, c] && !CellExists(layout, r - 1, c, rows, cols))
                        boundary.Add(c);

                foreach (var run in FindRuns(boundary))
                    for (int i = 0; i < run.Count - 1; i++)
                    {
                        float ex = origin.x + (run[i] + 1) * cellSize;
                        float ey = origin.y + (height - r) * cellSize + wallOff;
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, new Vector3(ex, ey, 0), 0f, cellSize);
                    }
            }

            // South boundaries
            for (int r = 0; r < rows; r++)
            {
                var boundary = new List<int>();
                for (int c = 0; c < cols; c++)
                    if (layout[r, c] && !CellExists(layout, r + 1, c, rows, cols))
                        boundary.Add(c);

                foreach (var run in FindRuns(boundary))
                    for (int i = 0; i < run.Count - 1; i++)
                    {
                        float ex = origin.x + (run[i] + 1) * cellSize;
                        float ey = origin.y + (height - 1 - r) * cellSize - wallOff;
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, new Vector3(ex, ey, 0), 180f, cellSize);
                    }
            }

            // East boundaries: column by column, find consecutive rows
            for (int c = 0; c < cols; c++)
            {
                var boundary = new List<int>();
                for (int r = 0; r < rows; r++)
                    if (layout[r, c] && !CellExists(layout, r, c + 1, rows, cols))
                        boundary.Add(r);

                foreach (var run in FindRuns(boundary))
                    for (int i = 0; i < run.Count - 1; i++)
                    {
                        float ex = origin.x + (c + 1) * cellSize + wallOff;
                        float ey = origin.y + (height - run[i] - 1) * cellSize;
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, new Vector3(ex, ey, 0), -90f, cellSize);
                    }
            }

            // West boundaries
            for (int c = 0; c < cols; c++)
            {
                var boundary = new List<int>();
                for (int r = 0; r < rows; r++)
                    if (layout[r, c] && !CellExists(layout, r, c - 1, rows, cols))
                        boundary.Add(r);

                foreach (var run in FindRuns(boundary))
                    for (int i = 0; i < run.Count - 1; i++)
                    {
                        float ex = origin.x + c * cellSize - wallOff;
                        float ey = origin.y + (height - run[i] - 1) * cellSize;
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, new Vector3(ex, ey, 0), 90f, cellSize);
                    }
            }
        }

        /// <summary>Split sorted integers into consecutive runs.</summary>
        private static List<List<int>> FindRuns(List<int> sorted)
        {
            var runs = new List<List<int>>();
            if (sorted.Count == 0) return runs;

            var current = new List<int> { sorted[0] };
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == current[current.Count - 1] + 1)
                    current.Add(sorted[i]);
                else
                {
                    runs.Add(current);
                    current = new List<int> { sorted[i] };
                }
            }
            runs.Add(current);
            return runs;
        }

        private static Vector3 CellCenter(int r, int c, float cellSize, Vector2 origin, int height)
        {
            return new Vector3(
                origin.x + c * cellSize + cellSize * 0.5f,
                origin.y + (height - 1 - r) * cellSize + cellSize * 0.5f,
                0f);
        }

        private static void AppendMesh(List<Vector3> vertices, List<int> triangles,
            List<Vector3> normals, List<Vector2> uvs,
            Mesh source, Vector3 offset, float angleDeg, float scale)
        {
            if (source == null) return;

            int baseIndex = vertices.Count;
            var rot = Quaternion.Euler(0f, 0f, angleDeg);

            var srcVerts = source.vertices;
            var srcNorms = source.normals;
            var srcUVs = source.uv;
            var srcTris = source.triangles;

            for (int i = 0; i < srcVerts.Length; i++)
            {
                var v = rot * (srcVerts[i] * scale) + offset;
                vertices.Add(v);

                normals.Add(i < srcNorms.Length ? rot * srcNorms[i] : Vector3.back);
                uvs.Add(i < srcUVs.Length ? srcUVs[i] : Vector2.zero);
            }

            for (int i = 0; i < srcTris.Length; i++)
            {
                triangles.Add(srcTris[i] + baseIndex);
            }
        }

        private static bool CellExists(bool[,] layout, int r, int c, int rows, int cols)
        {
            return r >= 0 && r < rows && c >= 0 && c < cols && layout[r, c];
        }

        private static void LoadModules()
        {
            if (_floorMesh != null) return;

            _floorMesh = LoadTileMesh("Tile_Floor");
            _edgeMesh = LoadTileMesh("Tile_Edge");
            _cornerOuterMesh = LoadTileMesh("Tile_CornerOuter");
            _cornerInnerMesh = LoadTileMesh("Tile_CornerInner");
            _cornerInnerFloorMesh = LoadTileMesh("Tile_CornerInnerFloor");
        }

        private static Mesh LoadTileMesh(string name)
        {
            var prefab = ResourceService.Loader.Load<GameObject>(TilePath + name);
            if (prefab == null)
            {
                Debug.LogWarning($"[BoardMeshBuilder] {name} not found at {TilePath}{name}");
                return null;
            }

            var mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"[BoardMeshBuilder] {name} has no mesh");
                return null;
            }

            return mf.sharedMesh;
        }

        public static void ClearCache()
        {
            _floorMesh = null;
            _edgeMesh = null;
            _cornerOuterMesh = null;
            _cornerInnerMesh = null;
            _cornerInnerFloorMesh = null;
            _boardMaterials = null;
        }

        /// <summary>
        /// Get board materials: [0] = light floor, [1] = dark floor, [2] = wall. Cached after first call.
        /// </summary>
        public static Material[] GetBoardMaterials()
        {
            if (_boardMaterials != null) return _boardMaterials;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            // Light floor: bright warm cream
            var floorLightMat = new Material(shader);
            floorLightMat.name = "BoardFloor_Light";
            InitUrpSurface(floorLightMat);
            SetBaseColor(floorLightMat, new Color(0.92f, 0.88f, 0.82f));
            if (floorLightMat.HasProperty("_Smoothness"))
                floorLightMat.SetFloat("_Smoothness", 0.35f);

            // Dark floor: slightly deeper for checkerboard
            var floorDarkMat = new Material(shader);
            floorDarkMat.name = "BoardFloor_Dark";
            InitUrpSurface(floorDarkMat);
            SetBaseColor(floorDarkMat, new Color(0.87f, 0.83f, 0.77f));
            if (floorDarkMat.HasProperty("_Smoothness"))
                floorDarkMat.SetFloat("_Smoothness", 0.35f);

            // Wall: slightly deeper warm tone
            var wallMat = new Material(shader);
            wallMat.name = "BoardWall";
            InitUrpSurface(wallMat);
            SetBaseColor(wallMat, new Color(0.82f, 0.76f, 0.68f));
            if (wallMat.HasProperty("_Smoothness"))
                wallMat.SetFloat("_Smoothness", 0.30f);

            _boardMaterials = new[] { floorLightMat, floorDarkMat, wallMat };
            return _boardMaterials;
        }

        /// <summary>
        /// Ensure URP Lit surface type is properly initialized.
        /// Without this, new Material(urpLitShader) renders magenta.
        /// </summary>
        private static void InitUrpSurface(Material mat)
        {
            // Surface type: Opaque
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);

            // Render queue
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

            // Ensure opaque blend state
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", 1f); // One
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", 0f); // Zero
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
        }

        private static void SetBaseColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.SetColor("_Color", color);
        }
    }
}
