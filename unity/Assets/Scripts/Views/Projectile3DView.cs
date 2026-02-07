using Match3.Presentation;
using Match3.Unity.Bridge;
using Match3.Unity.Pools;
using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// 3D visual representation of a projectile (UFO bomb missile).
    /// Uses MeshFilter + MeshRenderer with trail effect.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class Projectile3DView : MonoBehaviour, IPoolable
    {
        private static Material _sharedTrailMaterial;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private TrailRenderer _trail;

        public int ProjectileId { get; private set; }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            // Use UFO bomb mesh for projectile appearance
            _meshFilter.sharedMesh = MeshFactory.GetBombMesh(Core.Models.Enums.BombType.Ufo);
            _meshRenderer.sharedMaterial = MeshFactory.GetFallbackMaterial();

            CreateTrail();
        }

        private void CreateTrail()
        {
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(transform, false);
            trailGo.transform.localPosition = Vector3.zero;

            _trail = trailGo.AddComponent<TrailRenderer>();
            _trail.startWidth = 0.25f;
            _trail.endWidth = 0f;
            _trail.time = 0.2f;
            _trail.minVertexDistance = 0.05f;

            if (_sharedTrailMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                _sharedTrailMaterial = new Material(shader);
            }
            _trail.sharedMaterial = _sharedTrailMaterial;

            _trail.startColor = new Color(0.5f, 1f, 0.5f, 0.8f);
            _trail.endColor = new Color(0.5f, 1f, 0.5f, 0f);
        }

        public void Setup(int id)
        {
            ProjectileId = id;
        }

        public void UpdateFromVisual(ProjectileVisual visual, float cellSize, Vector2 origin, int height)
        {
            var worldPos = CoordinateConverter.GridToWorld(visual.Position, cellSize, origin, height);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Apply rotation around Z axis
            transform.rotation = Quaternion.Euler(0, 0, visual.Rotation);

            gameObject.SetActive(visual.IsVisible);
        }

        #region IPoolable

        public void OnSpawn()
        {
            ProjectileId = -1;
            transform.localScale = Vector3.one * 0.4f;
            _trail.Clear();
        }

        public void OnDespawn()
        {
            ProjectileId = -1;
            gameObject.SetActive(false);
            _trail.Clear();
        }

        #endregion
    }
}
