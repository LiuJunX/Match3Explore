using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Match3.Unity.Editor
{
    /// <summary>
    /// Copies config/ to StreamingAssets before build.
    /// </summary>
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CopyConfigToStreamingAssets();
        }

        [MenuItem("Match3/Sync Config to StreamingAssets")]
        public static void CopyConfigToStreamingAssets()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            var sourceConfig = Path.Combine(projectRoot, "config");
            var targetConfig = Path.Combine(Application.streamingAssetsPath, "config");

            if (!Directory.Exists(sourceConfig))
            {
                Debug.LogError($"[BuildPreprocessor] Config source not found: {sourceConfig}");
                return;
            }

            // Ensure StreamingAssets exists
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            // Remove old config
            if (Directory.Exists(targetConfig))
            {
                Directory.Delete(targetConfig, recursive: true);
            }

            // Copy recursively
            CopyDirectory(sourceConfig, targetConfig);

            AssetDatabase.Refresh();
            Debug.Log($"[BuildPreprocessor] Config copied to StreamingAssets ({CountFiles(targetConfig)} files)");
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(target, fileName), overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var dirName = Path.GetFileName(dir);
                CopyDirectory(dir, Path.Combine(target, dirName));
            }
        }

        private static int CountFiles(string path)
        {
            if (!Directory.Exists(path)) return 0;
            int count = Directory.GetFiles(path).Length;
            foreach (var dir in Directory.GetDirectories(path))
            {
                count += CountFiles(dir);
            }
            return count;
        }
    }
}
