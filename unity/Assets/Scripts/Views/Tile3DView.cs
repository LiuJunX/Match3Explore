using Match3.Core.Models.Enums;
using Match3.Presentation;
using Match3.Unity.Bridge;
using Match3.Unity.Pools;
using UnityEngine;
using UnityEngine.Rendering;

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
        private MeshRenderer _shadowRenderer;
        private MaterialPropertyBlock _shadowPropBlock;

        public int TileId { get; private set; }
        public TileType TileType { get; private set; }
        public BombType BombType { get; private set; }

        private Vector3 _baseScale = Vector3.one;
        private bool _isHighlighted;
        private bool _wasAnimated;
        private float _bounceTime = -1f;
        private float _highlightTime;
        private float _idleTime;

        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropFallback = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int ShadowColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ShadowColorPropFallback = Shader.PropertyToID("_Color");

        private const float BounceEndTime = 0.15f;

        // Scale multiplier: makes tiles fill more of the cell (~85% instead of ~70%)
        private const float TileScaleMultiplier = 1.05f;

        // Blob shadow constants
        private const float BlobShadowZ = 0.08f; // Between tile (Z=0) and board (Z=0.1)
        private const float BlobShadowSize = 0.72f; // Shadow disc size relative to cellSize
        private const float BlobShadowBaseAlpha = 0.22f; // Final alpha is per-tile via PropertyBlock
        private const float BlobShadowOffset = 0.06f; // In cellSize units, along sun direction

        private Transform _shadowTransform;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
            CreateBlobShadow();
        }

        /// <summary>
        /// Create a blob shadow child: flat semi-transparent circle under the tile.
        /// Gives the illusion of contact shadow on the board surface.
        /// </summary>
        private void CreateBlobShadow()
        {
            var shadowGo = new GameObject("BlobShadow");
            _shadowTransform = shadowGo.transform;
            _shadowTransform.SetParent(transform, false);

            shadowGo.AddComponent<MeshFilter>().sharedMesh = MeshFactory.GetBlobShadowMesh();
            _shadowRenderer = shadowGo.AddComponent<MeshRenderer>();
            _shadowRenderer.sharedMaterial = MeshFactory.GetBlobShadowMaterial();
            _shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _shadowRenderer.receiveShadows = false;
            _shadowPropBlock = new MaterialPropertyBlock();
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

            // 棋子在棋盘上投射并接收阴影
            _meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            _meshRenderer.receiveShadows = true;
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
            var pos = new Vector3(worldPos.x, worldPos.y, 0f);

            // Idle breathing: gentle Z-axis float when not animated (matches 2D TileView behavior)
            if (!isAnimated && !_isHighlighted)
            {
                _idleTime = (_idleTime + Time.deltaTime) % 628f;
                pos.z += Mathf.Sin(_idleTime * 2f + TileId * 0.5f) * 0.015f * cellSize;
            }
            else if (isAnimated)
            {
                _idleTime = 0f;
            }

            if (_isHighlighted)
            {
                _highlightTime = (_highlightTime + Time.deltaTime) % 628f; // wrap to avoid float precision loss
                // 选中呼吸：相对棋盘的上下（沿棋盘法线 Z 轴轻微浮动）
                var floatZ = Mathf.Sin(_highlightTime * 5f) * 0.04f * cellSize;
                pos.z += floatZ;
            }
            transform.position = pos;

            // Scale (uniform 3D, Z tracks min of X/Y for natural shrink)
            var scaleFactor = cellSize * TileScaleMultiplier;
            var zScale = Mathf.Min(visual.Scale.X, visual.Scale.Y);
            _baseScale = new Vector3(
                visual.Scale.X * scaleFactor,
                visual.Scale.Y * scaleFactor,
                zScale * scaleFactor);

            var finalScale = _baseScale;

            // Idle breathing scale pulse (matches 2D TileView)
            if (!isAnimated && !_isHighlighted)
            {
                var breathe = 1f + Mathf.Sin(_idleTime * 1.5f + TileId) * 0.012f;
                finalScale *= breathe;
            }

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

            // Selection pulse (scale + rotation; _highlightTime already updated above)
            if (_isHighlighted)
            {
                var pulse = 1f + Mathf.Sin(_highlightTime * 8f) * 0.08f;
                finalScale *= pulse;

                // 3D mode: Y-axis rotation
                var rot = transform.localEulerAngles;
                rot.y += 30f * Time.deltaTime;
                transform.localEulerAngles = rot;
            }

            transform.localScale = finalScale;

            // Update blob shadow: pinned to board surface, constant size
            UpdateBlobShadow(worldPos, cellSize, finalScale, pos.z);

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

        /// <summary>
        /// Update blob shadow transform: fixed on board surface, constant world size,
        /// unaffected by tile's rotation or scale animation.
        /// </summary>
        private void UpdateBlobShadow(Vector3 worldPos, float cellSize, Vector3 tileScale, float tileZ)
        {
            if (_shadowTransform == null) return;

            // Directional offset (stylized): shift shadow slightly along sun direction.
            // This gives a more "lit" and less "stamped" look.
            Vector2 dir2 = new Vector2(-0.35f, -0.70f).normalized;
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                var d = sun.transform.forward; // direction of light rays in world space
                var v = new Vector2(d.x, d.y);
                if (v.sqrMagnitude > 0.0001f)
                    dir2 = v.normalized;
            }

            // Lift factor: when tile floats a bit (selection pulse), shadow softens.
            float lift01 = Mathf.Clamp01(Mathf.Abs(tileZ) / Mathf.Max(cellSize * 0.06f, 0.0001f));
            float alpha = BlobShadowBaseAlpha * (1f - lift01 * 0.55f);
            float sizeMul = 1f + lift01 * 0.28f;

            // Pin shadow to board surface (between tile Z=0 and board Z=0.1)
            float off = BlobShadowOffset * cellSize;
            _shadowTransform.position = new Vector3(
                worldPos.x + dir2.x * off,
                worldPos.y + dir2.y * off,
                BlobShadowZ);
            _shadowTransform.rotation = Quaternion.identity;

            // Compensate parent scale so shadow has constant world size
            float shadowWorldSize = cellSize * BlobShadowSize * sizeMul;
            _shadowTransform.localScale = new Vector3(
                shadowWorldSize / Mathf.Max(Mathf.Abs(tileScale.x), 0.001f),
                shadowWorldSize / Mathf.Max(Mathf.Abs(tileScale.y), 0.001f),
                1f / Mathf.Max(Mathf.Abs(tileScale.z), 0.001f));

            // Per-tile alpha via PropertyBlock (keeps shared material)
            if (_shadowRenderer != null && _shadowPropBlock != null)
            {
                var c = new Color(1f, 1f, 1f, alpha);
                _shadowRenderer.GetPropertyBlock(_shadowPropBlock);
                _shadowPropBlock.SetColor(ShadowColorProp, c);
                _shadowPropBlock.SetColor(ShadowColorPropFallback, c);
                _shadowRenderer.SetPropertyBlock(_shadowPropBlock);
            }
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
            _idleTime = 0f;
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
