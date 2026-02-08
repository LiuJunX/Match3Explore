using System.Collections.Generic;
using Match3.Core.Models.Enums;
using Match3.Unity.Services;
using UnityEngine;

namespace Match3.Unity.Pools
{
    /// <summary>
    /// Runtime mesh and material generation for 3D tiles.
    /// Reuses SpriteFactory color definitions. Loads mesh via ResourceService.
    /// </summary>
    public static class MeshFactory
    {
        private static readonly Dictionary<TileType, Mesh> _tileMeshCache = new();
        private static Mesh _fallbackMesh;
        private static readonly Dictionary<TileType, Material> _materialCache = new();
        private static readonly Dictionary<BombType, Mesh> _bombMeshCache = new();
        private static Shader _litShader;

        /// <summary>
        /// Get the mesh for a specific tile type.
        /// Each color type has its own distinct shape.
        /// </summary>
        public static Mesh GetTileMesh(TileType type)
        {
            if (_tileMeshCache.TryGetValue(type, out var cached))
                return cached;

            var typeName = GetTileTypeName(type);
            var model = ResourceService.Loader.Load<GameObject>($"Art/Gems/Models/Gem_{typeName}");
            if (model != null)
            {
                var meshFilter = model.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    _tileMeshCache[type] = meshFilter.sharedMesh;
                    Debug.Log($"[MeshFactory] Loaded Gem_{typeName} mesh from Resources");
                    return meshFilter.sharedMesh;
                }
            }

            // Fallback: built-in sphere mesh
            Debug.LogWarning($"[MeshFactory] Gem_{typeName} not found, using fallback sphere");
            return GetFallbackMesh();
        }

        /// <summary>
        /// Get the shared fallback mesh (built-in sphere).
        /// </summary>
        public static Mesh GetFallbackMesh()
        {
            if (_fallbackMesh != null) return _fallbackMesh;

            var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _fallbackMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tempSphere);
            return _fallbackMesh;
        }

        /// <summary>
        /// Get the mesh for a bomb type.
        /// Loads from Resources, falls back to tile mesh.
        /// </summary>
        public static Mesh GetBombMesh(BombType type)
        {
            if (type == BombType.None) return GetFallbackMesh();

            if (_bombMeshCache.TryGetValue(type, out var cached))
                return cached;

            var typeName = type.ToString();
            var model = ResourceService.Loader.Load<GameObject>($"Art/Gems/Models/Bomb_{typeName}");
            if (model != null)
            {
                var meshFilter = model.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    _bombMeshCache[type] = meshFilter.sharedMesh;
                    Debug.Log($"[MeshFactory] Loaded Bomb_{typeName} mesh from Resources");
                    return meshFilter.sharedMesh;
                }
            }

            Debug.LogWarning($"[MeshFactory] Bomb_{typeName} mesh not found, using fallback sphere");
            var fallback = GetFallbackMesh();
            _bombMeshCache[type] = fallback;
            return fallback;
        }

        /// <summary>
        /// Get a cached ceramic material for the given tile type.
        /// Uses confirmed color palette with colored specular highlights.
        /// </summary>
        public static Material GetTileMaterial(TileType type)
        {
            if (_materialCache.TryGetValue(type, out var cached))
                return cached;

            var color = GetCeramicColor(type);
            var mat = CreateCeramicMaterial(color);
            mat.name = $"Tile3D_{GetTileTypeName(type)}";
            _materialCache[type] = mat;
            return mat;
        }

        /// <summary>
        /// Confirmed ceramic color palette (HSV-derived, boosted for 3D).
        /// Intentionally decoupled from SpriteFactory/VisualConfig — these are
        /// art-directed values with higher saturation to compensate for 3D lighting.
        /// Source of truth: Match3Art/docs/art-direction.md
        /// </summary>
        private static Color GetCeramicColor(TileType type)
        {
            if ((type & TileType.Red) != 0) return new Color(0.900f, 0.092f, 0.018f);
            if ((type & TileType.Blue) != 0) return new Color(0.070f, 0.367f, 0.880f);
            if ((type & TileType.Green) != 0) return new Color(0.041f, 0.820f, 0.301f);
            if ((type & TileType.Yellow) != 0) return new Color(0.950f, 0.724f, 0.048f);
            if ((type & TileType.Purple) != 0) return new Color(0.482f, 0.140f, 0.780f);
            if ((type & TileType.Orange) != 0) return new Color(0.950f, 0.464f, 0.038f);
            return Color.gray;
        }

        /// <summary>
        /// Get fallback material for unsupported types (Rainbow, bombs).
        /// </summary>
        public static Material GetFallbackMaterial()
        {
            return GetOrCreateFallbackMaterial();
        }

        /// <summary>
        /// Clear all cached materials and mesh reference.
        /// </summary>
        public static void ClearCache()
        {
            foreach (var mat in _materialCache.Values)
            {
                if (mat != null)
                    Object.Destroy(mat);
            }
            _materialCache.Clear();

            if (_fallbackMaterial != null)
            {
                Object.Destroy(_fallbackMaterial);
                _fallbackMaterial = null;
            }

            _bombMeshCache.Clear();
            _tileMeshCache.Clear();
            _fallbackMesh = null;
            _litShader = null;
            _complexLitShader = null;
        }

        private static Shader _complexLitShader;

        private static Shader GetLitShader()
        {
            if (_litShader != null) return _litShader;

            _litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_litShader == null)
                _litShader = Shader.Find("Standard");

            return _litShader;
        }

        private static Shader GetComplexLitShader()
        {
            if (_complexLitShader != null) return _complexLitShader;

            _complexLitShader = Shader.Find("Universal Render Pipeline/Complex Lit");
            return _complexLitShader;
        }

        /// <summary>
        /// Create ceramic material with glossy highlights and clear coat glaze.
        /// URP: Complex Lit (supports clear coat). Fallback: Standard.
        /// </summary>
        private static Material CreateCeramicMaterial(Color color)
        {
            // Prefer Complex Lit for clear coat, fallback to Lit, then Standard
            var shader = GetComplexLitShader() ?? GetLitShader();
            var mat = new Material(shader);

            // Base color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.SetColor("_Color", color);

            // Metallic workflow: low metallic = white specular highlights (candy/ceramic look)
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.05f);

            // Glossy ceramic: high enough for highlights, not so high that
            // flat-shading normal seams show harsh dark lines
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.72f);

            // Clear coat glaze layer (Complex Lit only)
            if (mat.HasProperty("_ClearCoatMask"))
            {
                mat.SetFloat("_ClearCoatMask", 0.6f);
                mat.EnableKeyword("_CLEARCOAT");
            }
            if (mat.HasProperty("_ClearCoatSmoothness"))
                mat.SetFloat("_ClearCoatSmoothness", 0.9f);

            // Enable emission keyword (default black = no glow)
            // Tile3DView sets emission color via PropertyBlock when selected
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);

            return mat;
        }

        private static Material _fallbackMaterial;

        private static Material GetOrCreateFallbackMaterial()
        {
            if (_fallbackMaterial != null) return _fallbackMaterial;

            _fallbackMaterial = CreateCeramicMaterial(Color.gray);
            _fallbackMaterial.name = "Tile3D_Fallback";
            return _fallbackMaterial;
        }

        private static string GetTileTypeName(TileType type)
        {
            if ((type & TileType.Red) != 0) return "Red";
            if ((type & TileType.Green) != 0) return "Green";
            if ((type & TileType.Blue) != 0) return "Blue";
            if ((type & TileType.Yellow) != 0) return "Yellow";
            if ((type & TileType.Purple) != 0) return "Purple";
            if ((type & TileType.Orange) != 0) return "Orange";
            return "Unknown";
        }
    }
}
