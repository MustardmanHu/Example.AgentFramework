using System.ComponentModel;
using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace MyAgentTeam.Infrastructure.Plugins;

/// <summary>
/// 提供 Agent 執行 Shell 指令的能力 (限制在特定沙盒目錄內)。
/// </summary>
public class ShellPlugin
{
    private readonly string _workingDirectory;

    /// <summary>
    /// 初始化 <see cref="ShellPlugin"/> 類別的新實例。
    /// </summary>
    /// <param name="workingDirectory">工作目錄路徑。</param>
    public ShellPlugin(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// 執行 Shell 指令 (如 dotnet build)。
    /// </summary>
    /// <param name="command">要執行的指令。</param>
    /// <returns>指令執行結果 (包含 stdout, stderr 和 Exit Code)。</returns>
    [KernelFunction, Description("執行 Shell 指令 (如 dotnet build)")]
    public string RunShellCommand(
        [Description("要執行的指令")] string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash", // Linux 環境通常使用 bash
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory
            };

            // 強制設定 dotnet 輸出為英文，避免亂碼
            processInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
            // 強制 UTF-8 編碼 (針對某些 Console 環境)
            processInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            processInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            // Windows 環境相容性調整
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                processInfo.FileName = "cmd.exe";
                // 使用 chcp 65001 強制 UTF-8 頁碼，但這需要 cmd /c 串接
                // 為了簡化，依賴 EnvironmentVariables 控制 dotnet 輸出英文通常足夠
                processInfo.Arguments = $"/C {command}";
            }

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            // 避免 Deadlock: 先讀取串流，再等待結束
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            bool exited = process.WaitForExit(60000); // 延長至 60 秒，編譯可能較久

            if (!exited)
            {
                process.Kill();
                return $"Error: Process timed out (60s).\nOutput so far:\n{output}";
            }

            // 組合最終結果，強制附上 Exit Code
            string result = $"\n=== COMMAND OUTPUT ===\n{output}\n{error}\n\n[Exit Code]: {process.ExitCode}";
            return result;
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}