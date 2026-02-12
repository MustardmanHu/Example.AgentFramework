using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace MyAgentTeam.Infrastructure.Plugins;

/// <summary>
/// 提供 Agent 讀寫檔案的能力 (限制在特定沙盒目錄內)。
/// </summary>
public class FileSystemPlugin
{
    private readonly string _rootPath;

    /// <summary>
    /// 初始化 <see cref="FileSystemPlugin"/> 類別的新實例。
    /// </summary>
    /// <param name="workingDirectory">工作目錄路徑。</param>
    public FileSystemPlugin(string workingDirectory)
    {
        // 確保路徑是絕對路徑，並作為沙盒根目錄
        _rootPath = Path.GetFullPath(workingDirectory);

        // 如果目錄不存在，自動建立
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <summary>
    /// 將內容寫入指定檔案。如果檔案存在則覆寫，如果目錄缺失則建立。
    /// </summary>
    /// <param name="relativePath">檔案相對路徑 (包含副檔名)，例如 'Models/User.cs' 或 'index.html'。</param>
    /// <param name="content">要寫入檔案的完整內容。</param>
    /// <returns>操作結果訊息。</returns>
    [KernelFunction, Description("Writes content to the specified file. Overwrites if exists, creates directory if missing.")]
    public string WriteFile(
        [Description("The relative path to the file (including extension), e.g., 'Models/User.cs' or 'index.html'")] string relativePath,
        [Description("The full content to write to the file")] string content)
    {
        try
        {
            // --- 安全性檢查 (Security Sandbox) ---
            // 組合完整路徑
            string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

            // 防止 Path Traversal 攻擊 (例如: ../../Windows/System32/...)
            if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Access Denied. You can only write files within {_rootPath}.";
            }

            // --- 執行寫入 ---
            // 確保目標資料夾存在
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);

            // 在 Console 顯示 Log，讓開發者知道發生了什麼事
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[System IO]: File saved to {relativePath}");
            Console.ResetColor();

            return $"Success: File created at {relativePath}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    /// <summary>
    /// 讀取現有檔案的內容。
    /// </summary>
    /// <param name="relativePath">檔案的相對路徑。</param>
    /// <returns>檔案內容或錯誤訊息。</returns>
    [KernelFunction, Description("Reads the content of an existing file.")]
    public string ReadFile(
        [Description("The relative path to the file.")] string relativePath)
    {
        try
        {
            string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

            // 安全性檢查
            if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Error: Access Denied.";
            }

            if (!File.Exists(fullPath))
            {
                return "Error: File not found.";
            }

            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    /// <summary>
    /// 列出目前目錄及子目錄下的所有檔案。
    /// </summary>
    /// <returns>檔案列表字串，每行一個檔案路徑。</returns>
    [KernelFunction, Description("Lists all files in the current directory and subdirectories.")]
    public string ListFiles()
    {
        try
        {
            var files = Directory.GetFiles(_rootPath, "*.*", SearchOption.AllDirectories);
            // 轉為相對路徑回傳，方便 Agent 閱讀
            var relativeFiles = files.Select(f => Path.GetRelativePath(_rootPath, f));
            return string.Join("\n", relativeFiles);
        }
        catch (Exception ex)
        {
            return $"Error listing files: {ex.Message}";
        }
    }
}