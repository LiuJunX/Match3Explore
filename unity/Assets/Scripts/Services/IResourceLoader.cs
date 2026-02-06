namespace Match3.Unity.Services
{
    /// <summary>
    /// Unified resource loading interface.
    /// Swap implementation to switch between Resources, Addressables, AssetBundle, etc.
    /// </summary>
    public interface IResourceLoader
    {
        T Load<T>(string path) where T : UnityEngine.Object;
    }
}
