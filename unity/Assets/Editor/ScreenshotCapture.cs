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

        // Get Game view window and capture it
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
            Debug.Log($"[Screenshot] Game view saved to: {path}");
        }
        else
        {
            Debug.LogError("[Screenshot] Could not find Game view window");
        }
    }
}
