using UnityEngine;
using UnityEditor;

public static class ScreenshotCapture
{
    [MenuItem("Tools/Capture Game View")]
    public static void CaptureGameView()
    {
        string dir = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(dir, "capture.png"));

        if (Application.isPlaying)
        {
            // Runtime: use ScreenCapture which grabs the actual rendered frame
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Screenshot] Runtime capture saved to: {path}");
        }
        else
        {
            // Editor: read Game view pixels
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            var gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView != null)
            {
                gameView.Focus();
                int w = (int)gameView.position.width;
                int h = (int)gameView.position.height;
                var colors = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                    gameView.position.position, w, h);
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.SetPixels(colors);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Debug.Log($"[Screenshot] Editor capture saved to: {path}");
            }
        }
    }
}
