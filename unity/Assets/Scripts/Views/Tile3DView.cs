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
        private bool _wasAnimated;
        private float _bounceTime = -1f;
        private float _highlightTime;

        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropFallback = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

        private const float BounceEndTime = 0.15f;

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

            // Use bomb mesh when applicable, otherwise per-type tile mesh
            _meshFilter.sharedMesh = bomb != BombType.None
                ? MeshFactory.GetBombMesh(bomb)
                : MeshFactory.GetTileMesh(type);

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
            var isAnimated = visual.IsBeingAnimated;

            // Detect landing: was animated, now idle
            if (_wasAnimated && !isAnimated)
            {
                _bounceTime = 0f;
            }
            _wasAnimated = isAnimated;

            // Position (with Y-flip)
            var worldPos = CoordinateConverter.GridToWorld(visual.Position, cellSize, origin, height);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Scale (uniform 3D, Z tracks min of X/Y for natural shrink)
            var scaleFactor = cellSize * 0.8f;
            var zScale = Mathf.Min(visual.Scale.X, visual.Scale.Y);
            _baseScale = new Vector3(
                visual.Scale.X * scaleFactor,
                visual.Scale.Y * scaleFactor,
                zScale * scaleFactor);

            var finalScale = _baseScale;

            // Landing bounce
            if (_bounceTime >= 0f && _bounceTime < BounceEndTime)
            {
                _bounceTime += Time.deltaTime;
                var t = _bounceTime / BounceEndTime;
                var squash = Mathf.Sin(t * Mathf.PI) * 0.1f;
                finalScale.x *= 1f + squash;
                finalScale.z *= 1f + squash;
                finalScale.y *= 1f - squash;
            }
            else if (_bounceTime >= BounceEndTime)
            {
                _bounceTime = -1f;
            }

            // Selection pulse
            if (_isHighlighted)
            {
                _highlightTime += Time.deltaTime;
                var pulse = 1f + Mathf.Sin(_highlightTime * 8f) * 0.08f;
                finalScale *= pulse;

                // 3D mode: Y-axis rotation
                var rot = transform.localEulerAngles;
                rot.y += 30f * Time.deltaTime;
                transform.localEulerAngles = rot;
            }

            transform.localScale = finalScale;

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
                _meshRenderer.SetPropertyBlock(null);
            }

            // Visibility
            gameObject.SetActive(visual.IsVisible);

            // Update bomb mesh if type changed (e.g., UpdateTileBombCommand)
            if (BombType != visual.BombType)
            {
                BombType = visual.BombType;
                _meshFilter.sharedMesh = BombType != BombType.None
                    ? MeshFactory.GetBombMesh(BombType)
                    : MeshFactory.GetTileMesh(TileType);
            }
        }

        /// <summary>
        /// Set highlight state for selection feedback.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted) return;
            _isHighlighted = highlighted;

            // Emission glow: on when selected, off when deselected
            _meshRenderer.GetPropertyBlock(_propBlock);
            if (highlighted)
            {
                var mat = _meshRenderer.sharedMaterial;
                var baseColor = mat.HasProperty(ColorProp)
                    ? mat.GetColor(ColorProp)
                    : mat.GetColor(ColorPropFallback);
                _propBlock.SetColor(EmissionColorProp, baseColor * 0.3f);
            }
            else
            {
                _propBlock.SetColor(EmissionColorProp, Color.black);
                _highlightTime = 0f;
                transform.localEulerAngles = Vector3.zero;
                transform.localScale = _baseScale;
            }
            _meshRenderer.SetPropertyBlock(_propBlock);
        }

        #region IPoolable

        public void OnSpawn()
        {
            TileId = -1;
            TileType = TileType.None;
            BombType = BombType.None;
            _baseScale = Vector3.one;
            _isHighlighted = false;
            _wasAnimated = false;
            _bounceTime = -1f;
            _highlightTime = 0f;
            transform.localScale = Vector3.one;
            transform.localEulerAngles = Vector3.zero;
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
