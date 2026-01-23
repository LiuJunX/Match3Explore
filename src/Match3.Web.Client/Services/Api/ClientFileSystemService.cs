using Match3.Editor.Interfaces;

namespace Match3.Web.Client.Services.Api;

/// <summary>
/// Client-side file system service that only provides path operations.
/// Actual file I/O is handled by API services.
/// </summary>
public class ClientFileSystemService : IFileSystemService
{
    public Task WriteTextAsync(string path, string content)
    {
        throw new NotSupportedException("Use API service for file operations");
    }

    public Task<string> ReadTextAsync(string path)
    {
        throw new NotSupportedException("Use API service for file operations");
    }

    public IEnumerable<string> GetFiles(string dir, string pattern)
    {
        return Enumerable.Empty<string>();
    }

    public IEnumerable<string> GetDirectories(string dir)
    {
        return Enumerable.Empty<string>();
    }

    public void CreateDirectory(string path) { }

    public void DeleteFile(string path) { }

    public void DeleteDirectory(string path) { }

    public bool FileExists(string path) => false;

    public bool DirectoryExists(string path) => false;

    public string GetStorageRoot() => "/";

    // Path operations work in client
    public string GetFileName(string path)
    {
        var idx = path.LastIndexOfAny(new[] { '/', '\\' });
        return idx >= 0 ? path.Substring(idx + 1) : path;
    }

    public string GetFileNameWithoutExtension(string path)
    {
        var fileName = GetFileName(path);
        var dotIdx = fileName.LastIndexOf('.');
        return dotIdx > 0 ? fileName.Substring(0, dotIdx) : fileName;
    }

    public string GetDirectoryName(string path)
    {
        var idx = path.LastIndexOfAny(new[] { '/', '\\' });
        return idx > 0 ? path.Substring(0, idx) : "";
    }

    public string CombinePath(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1)) return path2;
        if (string.IsNullOrEmpty(path2)) return path1;
        return path1.TrimEnd('/', '\\') + "/" + path2.TrimStart('/', '\\');
    }

    public string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
