using Match3.Core.Models.Enums;
using Match3.Presentation;
using Match3.Unity.Bridge;
using Match3.Unity.Pools;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// 3D visual representation of a single tile.
    /// Uses MeshFilter + MeshRenderer instead of SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class Tile3DView : MonoBehaviour, IPoolable
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;

        public int TileId { get; private set; }
        public TileType TileType { get; private set; }
        public BombType BombType { get; private set; }

        private Vector3 _baseScale = Vector3.one;
        private bool _isHighlighted;

        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropFallback = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Initialize the tile with ID, type, and bomb.
        /// </summary>
        public void Setup(int id, TileType type, BombType bomb)
        {
            TileId = id;
            TileType = type;
            BombType = bomb;

            _meshFilter.sharedMesh = MeshFactory.GetTileMesh();

            // 6 color types get their own material; others get fallback
            var isColorType = (type & (TileType.Red | TileType.Green | TileType.Blue |
                                       TileType.Yellow | TileType.Purple | TileType.Orange)) != 0;
            _meshRenderer.sharedMaterial = isColorType
                ? MeshFactory.GetTileMaterial(type)
                : MeshFactory.GetFallbackMaterial();
        }

        /// <summary>
        /// Update tile from visual state.
        /// </summary>
        public void UpdateFromVisual(TileVisual visual, float cellSize, Vector2 origin, int height)
        {
            // Position (with Y-flip)
            var worldPos = CoordinateConverter.GridToWorld(visual.Position, cellSize, origin, height);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Scale (uniform 3D, Z tracks min of X/Y for natural shrink)
            var scaleFactor = cellSize * 0.8f; // 80% of cell to leave gaps
            var zScale = Mathf.Min(visual.Scale.X, visual.Scale.Y);
            _baseScale = new Vector3(
                visual.Scale.X * scaleFactor,
                visual.Scale.Y * scaleFactor,
                zScale * scaleFactor);
            transform.localScale = _isHighlighted ? _baseScale * 1.1f : _baseScale;

            // Alpha via MaterialPropertyBlock
            if (visual.Alpha < 1f)
            {
                _meshRenderer.GetPropertyBlock(_propBlock);
                var mat = _meshRenderer.sharedMaterial;
                var color = mat.HasProperty(ColorProp)
                    ? mat.GetColor(ColorProp)
                    : mat.GetColor(ColorPropFallback);
                color.a = visual.Alpha;
                _propBlock.SetColor(ColorProp, color);
                _propBlock.SetColor(ColorPropFallback, color);
                _meshRenderer.SetPropertyBlock(_propBlock);
            }
            else
            {
                // Clear property block to use shared material directly
                _meshRenderer.SetPropertyBlock(null);
            }

            // Visibility
            gameObject.SetActive(visual.IsVisible);
        }

        /// <summary>
        /// Set highlight state for selection feedback.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted) return;
            _isHighlighted = highlighted;
            transform.localScale = highlighted ? _baseScale * 1.1f : _baseScale;
        }

        #region IPoolable

        public void OnSpawn()
        {
            TileId = -1;
            TileType = TileType.None;
            BombType = BombType.None;
            _baseScale = Vector3.one;
            _isHighlighted = false;
            transform.localScale = Vector3.one;
            _meshRenderer.SetPropertyBlock(null);
        }

        public void OnDespawn()
        {
            TileId = -1;
            gameObject.SetActive(false);
        }

        #endregion
    }
}
