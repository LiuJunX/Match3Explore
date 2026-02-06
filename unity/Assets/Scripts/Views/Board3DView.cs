using System.Collections.Generic;
using Match3.Presentation;
using Match3.Unity.Bridge;
using Match3.Unity.Pools;
using Match3.Unity.Services;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// 3D board view. Renders tiles as 3D meshes.
    /// Implements IBoardView for swappable 2D/3D rendering.
    /// </summary>
    public sealed class Board3DView : MonoBehaviour, IBoardView
    {
        private ObjectPool<Tile3DView> _tilePool;
        private readonly Dictionary<long, Tile3DView> _activeTiles = new();

        // Pre-allocated collections to avoid GC in hot path
        private readonly HashSet<long> _activeTileIds = new();
        private readonly List<long> _tilesToRemove = new();

        private Match3Bridge _bridge;
        private Transform _tileContainer;

        public int ActiveTileCount => _activeTiles.Count;

        public void Initialize(Match3Bridge bridge)
        {
            _bridge = bridge;

            _tileContainer = new GameObject("TileContainer3D").transform;
            _tileContainer.SetParent(transform, false);

            var (initial, max) = GetPoolSize("tiles", 64, 128);

            _tilePool = new ObjectPool<Tile3DView>(
                factory: () => CreateTile3DView(_tileContainer),
                parent: _tileContainer,
                initialSize: initial,
                maxSize: max
            );
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
        }
    }
}
