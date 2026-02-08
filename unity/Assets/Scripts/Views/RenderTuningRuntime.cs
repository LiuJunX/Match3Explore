using UnityEngine;

namespace Match3.Unity.Views
{
    /// <summary>
    /// Boots a hidden RenderTuningController automatically and finds a RenderTuningSettings asset
    /// without requiring scene wiring (no manual drag/drop).
    /// </summary>
    public static class RenderTuningRuntime
    {
        private const string RuntimeGoName = "~Match3_RenderTuningRuntime";

        private static RenderTuningSettings _settings;
        private static RenderTuningController _controller;

        public static RenderTuningSettings Settings => _settings != null ? _settings : (_settings = FindSettings());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureController();
        }

        public static RenderTuningController EnsureController()
        {
            if (_controller != null) return _controller;

            var go = GameObject.Find(RuntimeGoName);
            if (go == null)
            {
                go = new GameObject(RuntimeGoName);
                go.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(go);
            }

            _controller = go.GetComponent<RenderTuningController>();
            if (_controller == null)
                _controller = go.AddComponent<RenderTuningController>();

            _controller.SetSettings(Settings);
            _controller.ForceRefresh();
            return _controller;
        }

        public static void ApplyNow()
        {
            EnsureController()?.ForceRefresh();
        }

        public static void ApplyNow(RenderTuningSettings settings)
        {
            _settings = settings;
            EnsureController()?.SetSettings(_settings);
            EnsureController()?.ForceRefresh();
        }

        private static RenderTuningSettings FindSettings()
        {
            // Build/runtime: only Resources is guaranteed.
            var s = Resources.Load<RenderTuningSettings>("Match3/RenderTuningSettings")
                 ?? Resources.Load<RenderTuningSettings>("RenderTuningSettings");
            if (s != null) return s;

            // If user moved the asset within any Resources folder (or renamed it),
            // LoadAll still works in player builds.
            var all = Resources.LoadAll<RenderTuningSettings>(string.Empty);
            if (all != null && all.Length > 0)
            {
                // Prefer the conventional name if present.
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == "RenderTuningSettings")
                        return all[i];
                }
                return all[0];
            }

#if UNITY_EDITOR
            // Editor: search the whole project (no need to be in Resources).
            // Prefer an asset named "RenderTuningSettings" if multiple exist.
            try
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:RenderTuningSettings");
                string bestGuid = null;
                foreach (var guid in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;

                    // Prefer the one in Resources so behavior matches player builds.
                    if (path.Contains("/Resources/") && path.EndsWith("/RenderTuningSettings.asset"))
                    {
                        bestGuid = guid;
                        break;
                    }
                    if (path.EndsWith("/RenderTuningSettings.asset"))
                        bestGuid ??= guid;
                    bestGuid ??= guid;
                }

                if (!string.IsNullOrEmpty(bestGuid))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(bestGuid);
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<RenderTuningSettings>(path);
                }
            }
            catch
            {
                // Ignore editor search errors.
            }
#endif

            return null;
        }
    }
}

