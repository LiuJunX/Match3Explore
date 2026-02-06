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
        private static Mesh _tileMesh;
        private static bool _meshLoaded;
        private static readonly Dictionary<TileType, Material> _materialCache = new();
        private static Shader _unlitShader;

        /// <summary>
        /// Get the shared tile mesh.
        /// Tries to load from Resources, falls back to built-in sphere.
        /// </summary>
        public static Mesh GetTileMesh()
        {
            if (_meshLoaded) return _tileMesh;

            // Try loading from Resources via ResourceService
            var model = ResourceService.Loader.Load<GameObject>("Art/Gems/Models/Gem");
            if (model != null)
            {
                var meshFilter = model.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    _tileMesh = meshFilter.sharedMesh;
                    _meshLoaded = true;
                    Debug.Log("[MeshFactory] Loaded Gem mesh from Resources");
                    return _tileMesh;
                }
            }

            // Fallback: built-in sphere mesh
            var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _tileMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tempSphere);
            _meshLoaded = true;
            Debug.Log("[MeshFactory] Using built-in sphere mesh (fallback)");
            return _tileMesh;
        }

        /// <summary>
        /// Get a cached Unlit material for the given tile type.
        /// </summary>
        public static Material GetTileMaterial(TileType type)
        {
            if (_materialCache.TryGetValue(type, out var cached))
                return cached;

            var color = SpriteFactory.GetTileColor(type);
            var mat = CreateUnlitMaterial(color);
            mat.name = $"Tile3D_{GetTileTypeName(type)}";
            _materialCache[type] = mat;
            return mat;
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

            _tileMesh = null;
            _meshLoaded = false;
            _unlitShader = null;
        }

        private static Shader GetUnlitShader()
        {
            if (_unlitShader != null) return _unlitShader;

            // Try URP Unlit first, then built-in
            _unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_unlitShader == null)
                _unlitShader = Shader.Find("Unlit/Color");

            return _unlitShader;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            var shader = GetUnlitShader();
            var mat = new Material(shader);

            // URP Unlit uses _BaseColor, built-in Unlit/Color uses _Color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.SetColor("_Color", color);

            return mat;
        }

        private static Material _fallbackMaterial;

        private static Material GetOrCreateFallbackMaterial()
        {
            if (_fallbackMaterial != null) return _fallbackMaterial;

            _fallbackMaterial = CreateUnlitMaterial(Color.gray);
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
