using System.Collections.Generic;
using System.Threading.Tasks;

namespace Match3.Editor.Interfaces
{
    /// <summary>
    /// Abstraction for File System operations (IO)
    /// </summary>
    public interface IFileSystemService
    {
        Task WriteTextAsync(string path, string content);
        Task<string> ReadTextAsync(string path);
        IEnumerable<string> GetFiles(string dir, string pattern);
        IEnumerable<string> GetDirectories(string dir);
        void CreateDirectory(string path);
        void DeleteFile(string path);
        void DeleteDirectory(string path);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string GetStorageRoot(); // Root directory for data
    }
}
