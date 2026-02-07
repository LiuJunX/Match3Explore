using System.Collections.Generic;
using Match3.Unity.Services;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// Builds a single combined board mesh from modular tile pieces.
    /// Supports irregular board shapes via a bool layout mask.
    /// Modules: Floor, Edge, CornerOuter, CornerInner — loaded from FBX.
    /// </summary>
    public static class BoardMeshBuilder
    {
        private const string TilePath = "Art/Board/Models/";

        // Cached module meshes
        private static Mesh _floorMesh;
        private static Mesh _edgeMesh;
        private static Mesh _cornerOuterMesh;
        private static Mesh _cornerInnerMesh;

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

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!layout[r, c]) continue;

                    // Cell center in world space (Y-flipped)
                    float worldX = origin.x + c * cellSize + cellSize * 0.5f;
                    float worldY = origin.y + (height - 1 - r) * cellSize + cellSize * 0.5f;
                    var center = new Vector3(worldX, worldY, 0f);

                    // Floor
                    AppendMesh(vertices, triangles, normals, uvs,
                        _floorMesh, center, 0f, cellSize);

                    // Check 4 cardinal neighbors for edges
                    // North (+Y in Unity = row-1 in grid)
                    if (!CellExists(layout, r - 1, c, rows, cols))
                        AppendMesh(vertices, triangles, normals, uvs,
                            _edgeMesh, center, 0f, cellSize);

                    // East (+X = col+1)
                    if (!CellExists(layout, r, c + 1, rows, cols))
                        AppendMesh(vertices, triangles, normals, uvs,
                            _edgeMesh, center, -90f, cellSize);

                    // South (-Y = row+1)
                    if (!CellExists(layout, r + 1, c, rows, cols))
                        AppendMesh(vertices, triangles, normals, uvs,
                            _edgeMesh, center, 180f, cellSize);

                    // West (-X = col-1)
                    if (!CellExists(layout, r, c - 1, rows, cols))
                        AppendMesh(vertices, triangles, normals, uvs,
                            _edgeMesh, center, 90f, cellSize);

                    // Check 4 diagonal corners for outer/inner corners
                    // NE corner (row-1, col+1)
                    CheckCorner(layout, r, c, -1, 1, rows, cols,
                        vertices, triangles, normals, uvs, center, -90f, cellSize);

                    // NW corner (row-1, col-1)
                    CheckCorner(layout, r, c, -1, -1, rows, cols,
                        vertices, triangles, normals, uvs, center, 0f, cellSize);

                    // SE corner (row+1, col+1)
                    CheckCorner(layout, r, c, 1, 1, rows, cols,
                        vertices, triangles, normals, uvs, center, 180f, cellSize);

                    // SW corner (row+1, col-1)
                    CheckCorner(layout, r, c, 1, -1, rows, cols,
                        vertices, triangles, normals, uvs, center, 90f, cellSize);
                }
            }

            var mesh = new Mesh();
            mesh.name = "BoardMesh";
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
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
            int rows, int cols, List<Vector3> vertices, List<int> triangles,
            List<Vector3> normals, List<Vector2> uvs, Vector3 center, float angle, float cellSize)
        {
            bool adjRow = CellExists(layout, r + dr, c, rows, cols);
            bool adjCol = CellExists(layout, r, c + dc, rows, cols);
            bool diag = CellExists(layout, r + dr, c + dc, rows, cols);

            if (!adjRow && !adjCol)
            {
                // Both adjacent edges exposed → outer corner
                AppendMesh(vertices, triangles, normals, uvs,
                    _cornerOuterMesh, center, angle, cellSize);
            }
            else if (adjRow && adjCol && !diag)
            {
                // Both adjacent cells exist but diagonal missing → inner corner
                AppendMesh(vertices, triangles, normals, uvs,
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

            Debug.Log($"[BoardMeshBuilder] Loaded {name}: {mf.sharedMesh.vertexCount} verts");
            return mf.sharedMesh;
        }

        public static void ClearCache()
        {
            _floorMesh = null;
            _edgeMesh = null;
            _cornerOuterMesh = null;
            _cornerInnerMesh = null;
        }

        /// <summary>
        /// Create a board material matching the ceramic style.
        /// </summary>
        public static Material CreateBoardMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "BoardFloor";

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0.92f, 0.88f, 0.82f));
            else
                mat.SetColor("_Color", new Color(0.92f, 0.88f, 0.82f));

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.35f);

            return mat;
        }
    }
}
