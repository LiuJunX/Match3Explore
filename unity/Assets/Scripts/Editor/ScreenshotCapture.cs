using System.IO;
using UnityEditor;
using UnityEngine;

namespace Match3.Unity.Editor
{
    /// <summary>
    /// Captures the Game view screenshot for AI-assisted debugging.
    /// Menu: Match3 > Capture Screenshot
    /// </summary>
    public static class ScreenshotCapture
    {
        private const string OutputPath = "Screenshots/capture.png";

        [MenuItem("Match3/Capture Screenshot")]
        public static void Capture()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Screenshot] No main camera found");
                return;
            }

            // Ensure directory exists
            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Render camera to texture
            var width = Screen.width > 0 ? Screen.width : 800;
            var height = Screen.height > 0 ? Screen.height : 600;

            // Clamp to reasonable size
            width = Mathf.Clamp(width, 320, 1920);
            height = Mathf.Clamp(height, 240, 1080);

            var rt = new RenderTexture(width, height, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Restore
            cam.targetTexture = prev;
            RenderTexture.active = null;

            // Save PNG
            var bytes = tex.EncodeToPNG();
            var fullPath = Path.GetFullPath(OutputPath);
            File.WriteAllBytes(fullPath, bytes);

            // Cleanup
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"[Screenshot] Saved to {fullPath} ({width}x{height})");
        }
    }
}
