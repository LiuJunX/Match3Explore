using Match3.Editor.ViewModels;

namespace Match3.Editor.Interfaces
{
    /// <summary>
    /// Service for managing level files in the file system.
    /// </summary>
    public interface ILevelService
    {
        /// <summary>
        /// Builds a tree structure of the level folder.
        /// </summary>
        ScenarioFolderNode BuildTree();

        /// <summary>
        /// Reads a level JSON file.
        /// </summary>
        string ReadLevelJson(string relativePath);

        /// <summary>
        /// Writes a level JSON file.
        /// </summary>
        void WriteLevelJson(string relativePath, string json);

        /// <summary>
        /// Creates a new level file.
        /// </summary>
        string CreateNewLevel(string folderRelativePath, string levelName, string json);

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        string CreateFolder(string parentFolderRelativePath, string folderName);

        /// <summary>
        /// Duplicates a level file.
        /// </summary>
        string DuplicateLevel(string sourceRelativePath, string newLevelName);

        /// <summary>
        /// Deletes a level file.
        /// </summary>
        void DeleteLevel(string relativePath);

        /// <summary>
        /// Deletes a folder.
        /// </summary>
        void DeleteFolder(string relativePath);

        /// <summary>
        /// Renames a level file.
        /// </summary>
        void RenameLevel(string relativePath, string newName);

        /// <summary>
        /// Renames a folder.
        /// </summary>
        void RenameFolder(string relativePath, string newName);
    }
}
