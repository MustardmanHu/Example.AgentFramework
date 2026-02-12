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
            var (fileName, arguments) = ParseCommand(command);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "Error: Empty command.";
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
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

    /// <summary>
    /// 解析指令字串，分離執行檔與參數。
    /// 支援雙引號 " 與單引號 ' 包裹執行檔路徑。
    /// </summary>
    private static (string FileName, string Arguments) ParseCommand(string command)
    {
        command = command.Trim();
        if (string.IsNullOrEmpty(command))
        {
            return (string.Empty, string.Empty);
        }

        // 處理引號包裹的執行檔路徑 (e.g. "C:\Program Files\App.exe")
        if (command.StartsWith('"'))
        {
            int endQuote = command.IndexOf('"', 1);
            if (endQuote != -1)
            {
                string fileName = command.Substring(1, endQuote - 1);
                string args = command.Substring(endQuote + 1).Trim();
                return (fileName, args);
            }
        }

        // 處理單引號包裹 (Linux 風格)
        if (command.StartsWith('\''))
        {
            int endQuote = command.IndexOf('\'', 1);
            if (endQuote != -1)
            {
                string fileName = command.Substring(1, endQuote - 1);
                string args = command.Substring(endQuote + 1).Trim();
                return (fileName, args);
            }
        }

        // 預設以空白分隔
        int firstSpace = command.IndexOf(' ');
        if (firstSpace == -1)
        {
            return (command, string.Empty);
        }

        return (command.Substring(0, firstSpace), command.Substring(firstSpace + 1).Trim());
    }
}