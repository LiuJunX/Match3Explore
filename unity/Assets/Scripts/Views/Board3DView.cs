using System.Collections.Generic;
using Match3.Core.Models.Grid;
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
        private ObjectPool<Projectile3DView> _projectilePool;
        private readonly Dictionary<int, Tile3DView> _activeTiles = new();
        private readonly Dictionary<int, Projectile3DView> _activeProjectiles = new();

        // Pre-allocated collections to avoid GC in hot path
        private readonly HashSet<int> _activeTileIds = new();
        private readonly HashSet<int> _activeProjectileIds = new();
        private readonly List<int> _tilesToRemove = new();
        private readonly List<int> _projectilesToRemove = new();

        private Match3Bridge _bridge;
        private Transform _tileContainer;
        private Transform _projectileContainer;
        private Light _keyLight;
        private Light _fillLight;
        private Light _rimLight;
        private GameObject _boardFloor;
        private bool _viewInitialized;
        private int _highlightedTileId = -1;

        public int ActiveTileCount => _activeTiles.Count;

        public void Initialize(Match3Bridge bridge)
        {
            _bridge = bridge;

            if (_viewInitialized) return;

            // Setup 3D lighting
            SetupLighting();

            _tileContainer = new GameObject("TileContainer3D").transform;
            _tileContainer.SetParent(transform, false);

            _projectileContainer = new GameObject("ProjectileContainer3D").transform;
            _projectileContainer.SetParent(transform, false);

            var (tileInitial, tileMax) = GetPoolSize("tiles", 64, 128);
            var (projInitial, projMax) = GetPoolSize("projectiles", 5, 20);

            _tilePool = new ObjectPool<Tile3DView>(
                factory: () => CreateTile3DView(_tileContainer),
                parent: _tileContainer,
                initialSize: tileInitial,
                maxSize: tileMax
            );

            _projectilePool = new ObjectPool<Projectile3DView>(
                factory: () => CreateProjectile3DView(_projectileContainer),
                parent: _projectileContainer,
                initialSize: projInitial,
                maxSize: projMax
            );

            // Build board floor mesh
            BuildBoardFloor();

            _viewInitialized = true;
        }

        private void BuildBoardFloor()
        {
            var width = _bridge.Width;
            var height = _bridge.Height;
            var cellSize = _bridge.CellSize;
            var origin = _bridge.BoardOrigin;

            var mesh = BoardMeshBuilder.BuildRectangular(width, height, cellSize, origin);

            _boardFloor = new GameObject("BoardFloor");
            _boardFloor.transform.SetParent(transform, false);
            // Push behind tiles so it doesn't z-fight
            _boardFloor.transform.localPosition = new Vector3(0f, 0f, 0.1f);

            _boardFloor.AddComponent<MeshFilter>().mesh = mesh;
            _boardFloor.AddComponent<MeshRenderer>().material = BoardMeshBuilder.CreateBoardMaterial();
        }

        private void SetupLighting()
        {
            // Key Light: neutral white, main illumination
            var keyGo = new GameObject("BoardLight_Key");
            keyGo.transform.SetParent(transform, false);
            keyGo.transform.localPosition = new Vector3(-0.03f, 1.02f, 0.41f);
            keyGo.transform.rotation = Quaternion.Euler(42.73f, 18.97f, 33.92f);
            _keyLight = keyGo.AddComponent<Light>();
            _keyLight.type = LightType.Directional;
            _keyLight.color = Color.white;
            _keyLight.intensity = 1.2f;
            _keyLight.shadows = LightShadows.None;

            // Fill Light: softer, opposite side
            var fillGo = new GameObject("BoardLight_Fill");
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.rotation = Quaternion.Euler(322.86f, 336.94f, 350.83f);
            _fillLight = fillGo.AddComponent<Light>();
            _fillLight.type = LightType.Directional;
            _fillLight.color = new Color(0.95f, 0.93f, 0.90f);
            _fillLight.intensity = 0.6f;
            _fillLight.shadows = LightShadows.None;

            // Rim Light: from behind, subtle edge highlight
            var rimGo = new GameObject("BoardLight_Rim");
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.rotation = Quaternion.Euler(340.89f, 209.44f, 4.03f);
            _rimLight = rimGo.AddComponent<Light>();
            _rimLight.type = LightType.Directional;
            _rimLight.color = new Color(1.0f, 0.95f, 0.88f);
            _rimLight.intensity = 0.35f;
            _rimLight.shadows = LightShadows.None;

            // Ambient Light: warm neutral (not cool gray)
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.43f, 0.40f);
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

            // Update selection highlight
            UpdateSelectionHighlight();

            // Render projectiles
            RenderProjectiles(state, cellSize, origin, height);
        }

        private void UpdateSelectionHighlight()
        {
            var selectedPos = _bridge.CurrentState.SelectedPosition;
            int selectedTileId = selectedPos != Position.Invalid
                ? _bridge.GetTileIdAt(selectedPos)
                : -1;

            if (selectedTileId == _highlightedTileId) return;

            if (_highlightedTileId >= 0 && _activeTiles.TryGetValue(_highlightedTileId, out var prev))
                prev.SetHighlighted(false);

            if (selectedTileId >= 0 && _activeTiles.TryGetValue(selectedTileId, out var next))
                next.SetHighlighted(true);

            _highlightedTileId = selectedTileId;
        }

        private void RenderProjectiles(VisualState state, float cellSize, Vector2 origin, int height)
        {
            _activeProjectileIds.Clear();
            _projectilesToRemove.Clear();

            foreach (var kvp in state.Projectiles)
            {
                var projectileId = kvp.Key;
                var visual = kvp.Value;

                if (!visual.IsVisible) continue;

                _activeProjectileIds.Add(projectileId);

                if (!_activeProjectiles.TryGetValue(projectileId, out var projView))
                {
                    projView = _projectilePool.Rent();
                    projView.Setup(projectileId);
                    _activeProjectiles[projectileId] = projView;
                }

                projView.UpdateFromVisual(visual, cellSize, origin, height);
            }

            foreach (var kvp in _activeProjectiles)
            {
                if (!_activeProjectileIds.Contains(kvp.Key))
                {
                    _projectilesToRemove.Add(kvp.Key);
                }
            }

            foreach (var projectileId in _projectilesToRemove)
            {
                if (_activeProjectiles.TryGetValue(projectileId, out var projView))
                {
                    _projectilePool.Return(projView);
                    _activeProjectiles.Remove(projectileId);
                }
            }
        }

        public void Clear()
        {
            _highlightedTileId = -1;

            foreach (var kvp in _activeTiles)
            {
                _tilePool.Return(kvp.Value);
            }
            _activeTiles.Clear();

            foreach (var kvp in _activeProjectiles)
            {
                _projectilePool.Return(kvp.Value);
            }
            _activeProjectiles.Clear();
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

        private static Projectile3DView CreateProjectile3DView(Transform parent)
        {
            var go = new GameObject("Projectile3D");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var projView = go.AddComponent<Projectile3DView>();
            return projView;
        }

        private void OnDestroy()
        {
            Clear();
            _tilePool?.Clear();
            _projectilePool?.Clear();

            if (_keyLight != null)
            {
                Destroy(_keyLight.gameObject);
                _keyLight = null;
            }
            if (_fillLight != null)
            {
                Destroy(_fillLight.gameObject);
                _fillLight = null;
            }
            if (_rimLight != null)
            {
                Destroy(_rimLight.gameObject);
                _rimLight = null;
            }
            if (_boardFloor != null)
            {
                Destroy(_boardFloor);
                _boardFloor = null;
            }
        }
    }
}
