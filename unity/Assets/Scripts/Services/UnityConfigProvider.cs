using System;
using System.IO;
using Match3.Core.Config;
using UnityEngine;

namespace Match3.Unity.Services
{
    /// <summary>
    /// Unity-specific configuration provider.
    /// In Editor: loads from project root config/ directory.
    /// In Build: loads from StreamingAssets/config/ directory.
    /// </summary>
    public static class UnityConfigProvider
    {
        private static FileConfigProvider _instance;
        private static bool _initialized;

        /// <summary>
        /// Get the configuration provider instance.
        /// </summary>
        public static IConfigProvider Instance
        {
            get
            {
                EnsureInitialized();
                return _instance;
            }
        }

        /// <summary>
        /// Initialize the config provider.
        /// Call this early in game startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            var configRoot = GetConfigRoot();
            Debug.Log($"[Config] Loading from: {configRoot}");

            _instance = new FileConfigProvider(
                configRoot,
                readFile: ReadFile,
                listFiles: ListFiles
            );

            _initialized = true;
        }

        /// <summary>
        /// Clear cached configurations and reload.
        /// </summary>
        public static void Reload()
        {
            _instance?.ClearCache();
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static string GetConfigRoot()
        {
#if UNITY_EDITOR
            // In Editor: use project root config/
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var configPath = Path.Combine(projectRoot, "config");
            if (Directory.Exists(configPath))
            {
                return configPath;
            }
            Debug.LogWarning($"[Config] Project config not found at {configPath}, falling back to StreamingAssets");
#endif
            // In Build or fallback: use StreamingAssets
            return Path.Combine(Application.streamingAssetsPath, "config");
        }

        private static string ReadFile(string path)
        {
            // Normalize path separators
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);

            if (!File.Exists(normalizedPath))
            {
                throw new FileNotFoundException($"Config file not found: {normalizedPath}");
            }

            return File.ReadAllText(normalizedPath);
        }

        private static string[] ListFiles(string path)
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);

            if (!Directory.Exists(normalizedPath))
            {
                return Array.Empty<string>();
            }

            var files = Directory.GetFiles(normalizedPath);
            var result = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                result[i] = Path.GetFileName(files[i]);
            }
            return result;
        }
    }
}
