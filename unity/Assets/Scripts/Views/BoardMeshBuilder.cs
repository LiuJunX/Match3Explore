using System.Collections.Generic;
using Match3.Unity.Services;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// Builds a single combined board mesh from modular tile pieces.
    /// Supports irregular board shapes via a bool layout mask.
    /// Modules (5 FBX from Board_Tile.blend):
    ///   Floor, Edge, CornerOuter (ring), CornerFloor (cap), CornerInner.
    /// Uses 2 submeshes: submesh 0 = floor, submesh 1 = walls (edges + corners).
    /// </summary>
    public static class BoardMeshBuilder
    {
        private const string TilePath = "Art/Board/Models/";

        // Cached module meshes
        private static Mesh _floorMesh;
        private static Mesh _edgeMesh;
        private static Mesh _cornerOuterMesh;  // Quarter-ring (wall part)
        private static Mesh _cornerOuterFloorMesh;  // Floor cap at outer corner
        private static Mesh _cornerInnerMesh;

        // Cached materials
        private static Material[] _boardMaterials;

        // Must match Blender module geometry (Board_Tile.blend)
        private const float WallThickness = 0.12f;
        private const float CornerRadius = 0.3f;

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

            int estimatedVerts = cellCount * 30;
            var vertices = new List<Vector3>(estimatedVerts);
            var floorTris = new List<int>(estimatedVerts);
            var wallTris = new List<int>(estimatedVerts);
            var normals = new List<Vector3>(estimatedVerts);
            var uvs = new List<Vector2>(estimatedVerts);

            // Edge center: at cell boundary + half wall thickness (so inner face aligns with floor edge)
            float edgeOff = (0.5f + WallThickness * 0.5f) * cellSize;
            // Corner center: at cell boundary intersection (arcs extend outward by CornerRadius)
            float cornerOff = 0.5f * cellSize;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!layout[r, c]) continue;

                    // Cell center in world space (Y-flipped)
                    float worldX = origin.x + c * cellSize + cellSize * 0.5f;
                    float worldY = origin.y + (height - 1 - r) * cellSize + cellSize * 0.5f;
                    var center = new Vector3(worldX, worldY, 0f);

                    // Floor → submesh 0
                    AppendMesh(vertices, floorTris, normals, uvs,
                        _floorMesh, center, 0f, cellSize);

                    // Edges → submesh 1
                    // North (+Y in Unity = row-1 in grid)
                    if (!CellExists(layout, r - 1, c, rows, cols))
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, center + new Vector3(0, edgeOff, 0), 0f, cellSize);

                    // East (+X = col+1)
                    if (!CellExists(layout, r, c + 1, rows, cols))
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, center + new Vector3(edgeOff, 0, 0), -90f, cellSize);

                    // South (-Y = row+1)
                    if (!CellExists(layout, r + 1, c, rows, cols))
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, center + new Vector3(0, -edgeOff, 0), 180f, cellSize);

                    // West (-X = col-1)
                    if (!CellExists(layout, r, c - 1, rows, cols))
                        AppendMesh(vertices, wallTris, normals, uvs,
                            _edgeMesh, center + new Vector3(-edgeOff, 0, 0), 90f, cellSize);

                    // Corners → wall ring to submesh 1, floor cap to submesh 0
                    // Module arc goes +X to +Y in Unity (after FBX bake_space_transform)
                    // NE: 0° (arc in +X/+Y quadrant)
                    CheckCorner(layout, r, c, -1, 1, rows, cols,
                        vertices, floorTris, wallTris, normals, uvs,
                        center + new Vector3(cornerOff, cornerOff, 0), 0f, cellSize);

                    // NW: 90° (arc in -X/+Y quadrant)
                    CheckCorner(layout, r, c, -1, -1, rows, cols,
                        vertices, floorTris, wallTris, normals, uvs,
                        center + new Vector3(-cornerOff, cornerOff, 0), 90f, cellSize);

                    // SE: -90° (arc in +X/-Y quadrant)
                    CheckCorner(layout, r, c, 1, 1, rows, cols,
                        vertices, floorTris, wallTris, normals, uvs,
                        center + new Vector3(cornerOff, -cornerOff, 0), -90f, cellSize);

                    // SW: 180° (arc in -X/-Y quadrant)
                    CheckCorner(layout, r, c, 1, -1, rows, cols,
                        vertices, floorTris, wallTris, normals, uvs,
                        center + new Vector3(-cornerOff, -cornerOff, 0), 180f, cellSize);
                }
            }

            var mesh = new Mesh();
            mesh.name = "BoardMesh";
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(floorTris, 0);
            mesh.SetTriangles(wallTris, 1);
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

        private static void CheckCorner(bool[,] layout, int r, int c, int dr, int dc,
            int rows, int cols, List<Vector3> vertices, List<int> floorTris, List<int> wallTris,
            List<Vector3> normals, List<Vector2> uvs, Vector3 center, float angle, float cellSize)
        {
            bool adjRow = CellExists(layout, r + dr, c, rows, cols);
            bool adjCol = CellExists(layout, r, c + dc, rows, cols);
            bool diag = CellExists(layout, r + dr, c + dc, rows, cols);

            if (!adjRow && !adjCol)
            {
                // Both adjacent edges exposed → outer corner
                // Ring (wall thickness band) → wall submesh
                AppendMesh(vertices, wallTris, normals, uvs,
                    _cornerOuterMesh, center, angle, cellSize);
                // Floor cap (fills gap between floor edge and ring inner face) → floor submesh
                AppendMesh(vertices, floorTris, normals, uvs,
                    _cornerOuterFloorMesh, center, angle, cellSize);
            }
            else if (adjRow && adjCol && !diag)
            {
                // Both adjacent cells exist but diagonal missing → inner corner
                AppendMesh(vertices, wallTris, normals, uvs,
                    _cornerInnerMesh, center, angle, cellSize);
            }
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
            _cornerOuterFloorMesh = LoadTileMesh("Tile_CornerFloor");
            _cornerInnerMesh = LoadTileMesh("Tile_CornerInner");
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
            _cornerOuterFloorMesh = null;
            _cornerInnerMesh = null;
            _boardMaterials = null;
        }

        /// <summary>
        /// Get board materials: [0] = floor, [1] = wall. Cached after first call.
        /// </summary>
        public static Material[] GetBoardMaterials()
        {
            if (_boardMaterials != null) return _boardMaterials;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            // Floor: bright warm cream
            var floorMat = new Material(shader);
            floorMat.name = "BoardFloor";
            SetBaseColor(floorMat, new Color(0.92f, 0.88f, 0.82f));
            if (floorMat.HasProperty("_Smoothness"))
                floorMat.SetFloat("_Smoothness", 0.35f);

            // Wall: slightly deeper warm tone
            var wallMat = new Material(shader);
            wallMat.name = "BoardWall";
            SetBaseColor(wallMat, new Color(0.82f, 0.76f, 0.68f));
            if (wallMat.HasProperty("_Smoothness"))
                wallMat.SetFloat("_Smoothness", 0.30f);

            _boardMaterials = new[] { floorMat, wallMat };
            return _boardMaterials;
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
