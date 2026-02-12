using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAgentTeam.Infrastructure.Plugins;
using System;
using System.IO;
using System.Linq;

namespace MyAgentTeam.Infrastructure.Tests
{
    [TestClass]
    public class FileSystemPluginTests
    {
        private string _testDirectory = null!;
        private FileSystemPlugin _plugin = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FileSystemPluginTests", Guid.NewGuid().ToString());
            // Ensure the directory exists before each test, although the plugin constructor also does it.
            // But we might want a clean slate.
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            _plugin = new FileSystemPlugin(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void Constructor_ShouldCreateDirectory_WhenItDoesNotExist()
        {
            // Arrange
            string newDir = Path.Combine(Path.GetTempPath(), "FileSystemPluginTests_Constructor", Guid.NewGuid().ToString());

            // Act
            new FileSystemPlugin(newDir);

            // Assert
            Assert.IsTrue(Directory.Exists(newDir));

            // Cleanup
            if (Directory.Exists(newDir))
            {
                Directory.Delete(newDir, true);
            }
        }

        [TestMethod]
        public void WriteFile_ShouldCreateFile_WithContent()
        {
            // Arrange
            string fileName = "test.txt";
            string content = "Hello World";

            // Act
            string result = _plugin.WriteFile(fileName, content);

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(_testDirectory, fileName)));
            Assert.AreEqual(content, File.ReadAllText(Path.Combine(_testDirectory, fileName)));
            StringAssert.Contains(result, "Success");
        }

        [TestMethod]
        public void WriteFile_ShouldOverwrite_WhenFileExists()
        {
            // Arrange
            string fileName = "test.txt";
            File.WriteAllText(Path.Combine(_testDirectory, fileName), "Old Content");
            string newContent = "New Content";

            // Act
            _plugin.WriteFile(fileName, newContent);

            // Assert
            Assert.AreEqual(newContent, File.ReadAllText(Path.Combine(_testDirectory, fileName)));
        }

        [TestMethod]
        public void WriteFile_ShouldCreateSubdirectories_WhenNeeded()
        {
            // Arrange
            string fileName = "sub/dir/test.txt";
            string content = "Deep Content";

            // Act
            _plugin.WriteFile(fileName, content);

            // Assert
            string fullPath = Path.Combine(_testDirectory, "sub", "dir", "test.txt");
            Assert.IsTrue(File.Exists(fullPath));
            Assert.AreEqual(content, File.ReadAllText(fullPath));
        }

        [TestMethod]
        public void WriteFile_ShouldFail_WhenPathTraversalAttempted()
        {
            // Arrange
            string fileName = "../outside.txt";
            string content = "Malicious Content";

            // Act
            string result = _plugin.WriteFile(fileName, content);

            // Assert
            // We can't easily verify the file wasn't created outside without knowing where outside is relative to temp,
            // but we can check the result message.
            StringAssert.Contains(result, "Access Denied");
        }

        [TestMethod]
        public void ReadFile_ShouldReturnContent_WhenFileExists()
        {
            // Arrange
            string fileName = "read.txt";
            string content = "Read Me";
            File.WriteAllText(Path.Combine(_testDirectory, fileName), content);

            // Act
            string result = _plugin.ReadFile(fileName);

            // Assert
            Assert.AreEqual(content, result);
        }

        [TestMethod]
        public void ReadFile_ShouldReturnError_WhenFileDoesNotExist()
        {
            // Arrange
            string fileName = "ghost.txt";

            // Act
            string result = _plugin.ReadFile(fileName);

            // Assert
            StringAssert.Contains(result, "File not found");
        }

        [TestMethod]
        public void ReadFile_ShouldFail_WhenPathTraversalAttempted()
        {
            // Arrange
            string fileName = "../secret.txt";

            // Act
            string result = _plugin.ReadFile(fileName);

            // Assert
            StringAssert.Contains(result, "Access Denied");
        }

        [TestMethod]
        public void ListFiles_ShouldReturnRelativePaths()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testDirectory, "root.txt"), "content");
            Directory.CreateDirectory(Path.Combine(_testDirectory, "sub"));
            File.WriteAllText(Path.Combine(_testDirectory, "sub", "child.txt"), "content");

            // Act
            string result = _plugin.ListFiles();
            var files = result.Split('\n').Select(f => f.Trim()).ToList();

            // Assert
            CollectionAssert.Contains(files, "root.txt");
            // Check for both separators just in case or use Path.Combine
            Assert.IsTrue(files.Any(f => f == Path.Combine("sub", "child.txt") || f == "sub/child.txt" || f == "sub\\child.txt"));
        }
    }
}
