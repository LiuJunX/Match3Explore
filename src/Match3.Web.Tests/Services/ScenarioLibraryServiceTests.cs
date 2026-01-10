using System;
using System.IO;
using System.Linq;
using Match3.Web.Services;
using Xunit;

namespace Match3.Web.Tests.Services
{
    public class ScenarioLibraryServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ScenarioLibraryService _service;

        public ScenarioLibraryServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "Match3Tests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _service = new ScenarioLibraryService(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void CreateNewScenario_CreatesFile()
        {
            var path = _service.CreateNewScenario("", "Test Level", "{}");
            Assert.True(File.Exists(Path.Combine(_tempDir, "TestLevel.json")));
            Assert.Equal("TestLevel.json", path);
        }

        [Fact]
        public void CreateFolder_CreatesDirectory()
        {
            var path = _service.CreateFolder("", "My Folder");
            Assert.True(Directory.Exists(Path.Combine(_tempDir, "MyFolder")));
            Assert.Equal("MyFolder", path);
        }

        [Fact]
        public void GetFolderContents_ReturnsCorrectItems()
        {
            _service.CreateFolder("", "FolderA");
            _service.CreateNewScenario("", "FileA", "{}");
            _service.CreateNewScenario("FolderA", "FileB", "{}");

            // ScenarioLibraryService no longer has GetFolderContents method.
            // It uses BuildTree() to return a ScenarioFolderNode structure.
            var rootNode = _service.BuildTree();
            
            Assert.Single(rootNode.Folders);
            Assert.Equal("FolderA", rootNode.Folders[0].Name);
            Assert.Single(rootNode.Files);
            Assert.Equal("FileA", rootNode.Files[0].Name);

            var subFolder = rootNode.Folders[0];
            Assert.Empty(subFolder.Folders);
            Assert.Single(subFolder.Files);
            Assert.Equal("FileB", subFolder.Files[0].Name);
        }
    }
}
