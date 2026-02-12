using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAgentTeam.Infrastructure.Services;
using System;
using System.IO;

namespace MyAgentTeam.Infrastructure.Tests
{
    [TestClass]
    public class PathValidatorTests
    {
        [TestMethod]
        public void IsSafePath_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(PathValidator.IsSafePath(null!));
            Assert.IsFalse(PathValidator.IsSafePath(""));
            Assert.IsFalse(PathValidator.IsSafePath("   "));
        }

        [TestMethod]
        public void IsSafePath_RootDirectory_ReturnsFalse()
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.IsFalse(PathValidator.IsSafePath("C:\\"));
                Assert.IsFalse(PathValidator.IsSafePath("D:\\"));
            }
            else
            {
                Assert.IsFalse(PathValidator.IsSafePath("/"));
            }
        }

        [TestMethod]
        public void IsSafePath_SystemDirectories_ReturnsFalse()
        {
            if (OperatingSystem.IsWindows())
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                Assert.IsFalse(PathValidator.IsSafePath(winDir));
                Assert.IsFalse(PathValidator.IsSafePath(Path.Combine(winDir, "System32")));

                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                Assert.IsFalse(PathValidator.IsSafePath(progFiles));
            }
            else
            {
                Assert.IsFalse(PathValidator.IsSafePath("/bin"));
                Assert.IsFalse(PathValidator.IsSafePath("/usr"));
                Assert.IsFalse(PathValidator.IsSafePath("/etc/passwd"));
            }
        }

        [TestMethod]
        public void IsSafePath_ValidPath_ReturnsTrue()
        {
            string safePath;
            if (OperatingSystem.IsWindows())
            {
                safePath = @"C:\Users\Public\Documents\MyProject";
            }
            else
            {
                safePath = "/home/user/projects/myproject";
            }

            Assert.IsTrue(PathValidator.IsSafePath(safePath));
        }
    }
}
