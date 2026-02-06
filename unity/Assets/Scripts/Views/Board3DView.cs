using System.Collections.Generic;
using Match3.Presentation;
using Match3.Unity.Bridge;
using Match3.Unity.Pools;
using Match3.Unity.Services;
using UnityEngine;
using UnityEngine.Rendering;

namespace Match3.Unity.Views
{
    /// <summary>
    /// 3D board view. Renders tiles as 3D meshes.
    /// Implements IBoardView for swappable 2D/3D rendering.
    /// </summary>
    public sealed class Board3DView : MonoBehaviour, IBoardView
    {
        private ObjectPool<Tile3DView> _tilePool;
        private readonly Dictionary<int, Tile3DView> _activeTiles = new();

        // Pre-allocated collections to avoid GC in hot path
        private readonly HashSet<int> _activeTileIds = new();
        private readonly List<int> _tilesToRemove = new();

        private Match3Bridge _bridge;
        private Transform _tileContainer;
        private Light _directionalLight;
        private bool _viewInitialized;

        public int ActiveTileCount => _activeTiles.Count;

        public void Initialize(Match3Bridge bridge)
        {
            _bridge = bridge;

            if (_viewInitialized) return;

            // Setup 3D lighting
            SetupLighting();

            _tileContainer = new GameObject("TileContainer3D").transform;
            _tileContainer.SetParent(transform, false);

            var (initial, max) = GetPoolSize("tiles", 64, 128);

            _tilePool = new ObjectPool<Tile3DView>(
                factory: () => CreateTile3DView(_tileContainer),
                parent: _tileContainer,
                initialSize: initial,
                maxSize: max
            );

            _viewInitialized = true;
        }

        private void SetupLighting()
        {
            // Directional Light: warm white, angled for depth
            var lightGo = new GameObject("BoardLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            _directionalLight = lightGo.AddComponent<Light>();
            _directionalLight.type = LightType.Directional;
            _directionalLight.color = new Color(1f, 0.98f, 0.94f); // warm white
            _directionalLight.intensity = 1.0f;
            _directionalLight.shadows = LightShadows.None;

            // Ambient Light: cool gray
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.31f, 0.33f, 0.39f); // cool gray
        }

        public void Render(VisualState state)
        {
            if (state == null) return;

            var cellSize = _bridge.CellSize;
            var origin = _bridge.BoardOrigin;
            var height = _bridge.Height;

            _activeTileIds.Clear();
            _tilesToRemove.Clear();

            // Update existing tiles and create new ones
            foreach (var kvp in state.Tiles)
            {
                var tileId = kvp.Key;
                var visual = kvp.Value;

                if (!visual.IsVisible) continue;

                _activeTileIds.Add(tileId);

                if (!_activeTiles.TryGetValue(tileId, out var tileView))
                {
                    tileView = _tilePool.Rent();
                    tileView.Setup(tileId, visual.TileType, visual.BombType);
                    _activeTiles[tileId] = tileView;
                }

                tileView.UpdateFromVisual(visual, cellSize, origin, height);
            }

            // Remove tiles that are no longer active
            foreach (var kvp in _activeTiles)
            {
                if (!_activeTileIds.Contains(kvp.Key))
                {
                    _tilesToRemove.Add(kvp.Key);
                }
            }

            foreach (var tileId in _tilesToRemove)
            {
                if (_activeTiles.TryGetValue(tileId, out var tileView))
                {
                    _tilePool.Return(tileView);
                    _activeTiles.Remove(tileId);
                }
            }
        }

        public void Clear()
        {
            foreach (var kvp in _activeTiles)
            {
                _tilePool.Return(kvp.Value);
            }
            _activeTiles.Clear();
        }

        private static Tile3DView CreateTile3DView(Transform parent)
        {
            var go = new GameObject("Tile3D");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var tileView = go.AddComponent<Tile3DView>();
            return tileView;
        }

        private static (int initial, int max) GetPoolSize(string poolName, int defaultInitial, int defaultMax)
        {
            try
            {
                var config = UnityConfigProvider.Instance.GetGameConfig();
                if (config.PoolSizes != null && config.PoolSizes.TryGetValue(poolName, out var poolConfig))
                {
                    return (poolConfig.Initial, poolConfig.Max);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Board3DView] Failed to load pool config for '{poolName}': {ex.Message}");
            }
            return (defaultInitial, defaultMax);
        }

        private void OnDestroy()
        {
            Clear();
            _tilePool?.Clear();

            if (_directionalLight != null)
            {
                Destroy(_directionalLight.gameObject);
                _directionalLight = null;
            }
        }
    }
}
