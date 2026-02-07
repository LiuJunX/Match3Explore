using UnityEditor;
using UnityEngine;

public static class ReimportGems
{
    [MenuItem("Tools/Reimport All Gems")]
    public static void Reimport()
    {
        string[] paths = {
            "Assets/Resources/Art/Gems/Models/Gem_Red.fbx",
            "Assets/Resources/Art/Gems/Models/Gem_Blue.fbx",
            "Assets/Resources/Art/Gems/Models/Gem_Green.fbx",
            "Assets/Resources/Art/Gems/Models/Gem_Yellow.fbx",
            "Assets/Resources/Art/Gems/Models/Gem_Purple.fbx",
            "Assets/Resources/Art/Gems/Models/Gem_Orange.fbx",
        };
        foreach (var p in paths)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ReimportGems] Reimported: {p}");
        }
        AssetDatabase.Refresh();
        Debug.Log("[ReimportGems] Done!");
    }
}
