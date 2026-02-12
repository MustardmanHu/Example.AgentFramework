using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAgentTeam.Infrastructure.Plugins;
using System;
using System.IO;

namespace MyAgentTeam.Infrastructure.Tests
{
    [TestClass]
    public class FileSystemPluginSecurityTests
    {
        private string _testDirectory = null!;
        private FileSystemPlugin _plugin = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "FileSystemPluginSecurityTests", Guid.NewGuid().ToString());
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
        public void WriteFile_ShouldFail_WhenPathTraversalAttempted_WithPartialMatch()
        {
            // Arrange
            // Exploit scenario: trying to write to a sibling directory that shares the same prefix.
            // Example: Root=/tmp/test, Target=/tmp/test_suffix/pwned.txt
            string rootDirName = Path.GetFileName(_testDirectory);
            string siblingDirName = rootDirName + "_suffix";
            string relativePath = $"../{siblingDirName}/pwned.txt";
            string content = "pwned";

            // Act
            string result = _plugin.WriteFile(relativePath, content);

            // Assert
            StringAssert.Contains(result, "Access Denied", "Partial path match should be denied.");
        }

        [TestMethod]
        public void WriteFile_ShouldFail_WhenAbsoluteRootPathProvided()
        {
            // Arrange
            // Exploit scenario: trying to write to an absolute path outside root.
            // We use a path that is clearly outside the _testDirectory.
            // Using a path relative to temp but outside _testDirectory.
            string absolutePath = Path.Combine(Path.GetTempPath(), "outside_root.txt");
            string content = "outside content";

            // Act
            // Although WriteFile expects relative path, providing absolute path might bypass combine logic or lead to unintended behavior.
            // But Path.Combine(root, absolute) returns absolute on many platforms.
            string result = _plugin.WriteFile(absolutePath, content);

            // Assert
            StringAssert.Contains(result, "Access Denied", "Absolute path outside root should be denied.");
        }

        [TestMethod]
        public void WriteFile_ShouldFail_WhenStandardPathTraversalAttempted()
        {
             // Arrange
            string fileName = "../standard_traversal.txt";
            string content = "Malicious Content";

            // Act
            string result = _plugin.WriteFile(fileName, content);

            // Assert
            StringAssert.Contains(result, "Access Denied", "Standard ../ traversal should be denied.");
        }
    }
}
