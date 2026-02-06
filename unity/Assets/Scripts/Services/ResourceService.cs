namespace Match3.Unity.Services
{
    /// <summary>
    /// Static access point for resource loading.
    /// Set Loader at startup to switch loading strategy.
    /// </summary>
    public static class ResourceService
    {
        private static IResourceLoader _loader;

        public static IResourceLoader Loader
        {
            get => _loader ??= new ResourcesLoader();
            set => _loader = value;
        }
    }
}
