using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAgentTeam.Infrastructure.Plugins;
using System;
using System.IO;

namespace MyAgentTeam.Infrastructure.Tests;

[TestClass]
public class ShellPluginTests
{
    private string _testDir;
    private ShellPlugin _plugin;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _plugin = new ShellPlugin(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [TestMethod]
    public async Task RunShellCommand_LegitimateCommand_ReturnsOutput()
    {
        // Act
        var result = await _plugin.RunShellCommand("dotnet --version");

        // Assert
        Assert.IsTrue(result.Contains("10.0") || result.Contains("9.0") || result.Contains("8.0") || result.Contains("7.0") || result.Contains("6.0") || result.Contains("5.0"), $"Result was: {result}");
        Assert.IsTrue(result.Contains("Exit Code]: 0"), "Exit code should be 0");
    }

    [TestMethod]
    public async Task RunShellCommand_CommandInjection_ShouldFailAfterFix()
    {
        // This test demonstrates the vulnerability. Before fix, it creates the file. After fix, it should not.

        string injectedFile = "hacked.txt";
        string command = $"dotnet --version; echo hacked > {injectedFile}";

        // Act
        var result = await _plugin.RunShellCommand(command);

        // Check if file created in working directory
        string filePath = Path.Combine(_testDir, injectedFile);
        bool fileExists = File.Exists(filePath);

        // Before fix: fileExists should be true.
        // After fix: fileExists should be false.

        // Currently asserting TRUE to demonstrate vulnerability exists.
        // I will change this to FALSE after applying the fix.
        Assert.IsFalse(fileExists, "Vulnerability persisted! Expected file creation to fail after fix.");
    }
}
