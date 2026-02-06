using UnityEngine;

namespace Match3.Unity.Services
{
    /// <summary>
    /// Default resource loader using Unity's Resources.Load.
    /// Assets must be placed under a Resources/ folder.
    /// </summary>
    public sealed class ResourcesLoader : IResourceLoader
    {
        public T Load<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }
    }
}
