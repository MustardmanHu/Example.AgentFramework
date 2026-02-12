using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyAgentTeam.ConsoleHost;
using MyAgentTeam.Core.Interfaces;
using MyAgentTeam.Infrastructure.Configuration;

// 1. .NET Host Builder
// appsettings.json
var builder = Host.CreateApplicationBuilder(args);

// 2. Configuration 
// (Binding) 
string googleApiKey = builder.Configuration["Google:ApiKey"]
        ?? throw new InvalidOperationException(": Google:ApiKey");
string googleModelId = builder.Configuration["Google:ModelId"] ?? "gemini-2.5-pro";

string googleSearchApiKey = builder.Configuration["GoogleSearch:ApiKey"] ?? "";
string googleCx = builder.Configuration["GoogleSearch:SearchEngineId"] ?? "";

// ---Introduction---
Console.WriteLine("--- .NET AI Agent Team (Multi-Layer Architecture) ---");

// 使用 ConsoleWorkflow 初始化專案 (選擇模式、路徑)
var projectConfig = ConsoleWorkflow.InitializeProject();
string projectPath = projectConfig.ProjectPath;
bool isNewProject = projectConfig.IsNewProject;

// 3. DI ( Project Path)
builder.Services.AddAgentInfrastructure(googleApiKey, googleModelId, googleSearchApiKey, googleCx, projectPath);

var host = builder.Build();

// 取得使用者需求 (手動輸入 or 讀取 Spec)
string userGoal = ConsoleWorkflow.DetermineUserGoal(isNewProject);

// DI  Orchestrator
using (var scope = host.Services.CreateScope())
{
    var orchestrator = scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();

    Console.WriteLine("\n--- Team Working ---");
    Console.WriteLine($"Working Directory: {projectPath}");
    Console.WriteLine($"Mode: {(isNewProject ? "New Project" : "Maintenance / Existing Project")}");

    try
    {
        // 傳入 isNewProject 參數，以切換 Agent 的 Prompt 模式
        await foreach (var response in orchestrator.ExecuteAsync(userGoal, isNewProject))
        {
            PrintColoredMessage(response.AgentName, response.Content);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    // 若為測試模式 (內容包含特定關鍵字)，驗證檔案是否建立
    if (userGoal.Contains("test_write.txt"))
    {
        string testFilePath = Path.Combine(projectPath, "test_write.txt");
        if (File.Exists(testFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[Test Passed]: File '{testFilePath}' was successfully created.");
            Console.WriteLine($"Content: {File.ReadAllText(testFilePath)}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Test Failed]: File '{testFilePath}' was NOT found.");
            Console.ResetColor();
        }
    }
}
Console.WriteLine("\n--- Process Completed ---");


// UI 
/// <summary>
/// 輸出帶有顏色的訊息到控制台。
/// </summary>
/// <param name="name">Agent 名稱。</param>
/// <param name="content">訊息內容。</param>
static void PrintColoredMessage(string name, string content)
{
    var color = name switch
    {
        "Supervisor" => ConsoleColor.Red,
        "System_Designer" => ConsoleColor.Magenta,
        "DBA" => ConsoleColor.Yellow,
        "Programmer" => ConsoleColor.Cyan,
        "Researcher" => ConsoleColor.Green,
        "QA" => ConsoleColor.DarkYellow,
        "System_Interceptor" => ConsoleColor.Blue,
        _ => ConsoleColor.Gray
    };

    Console.ForegroundColor = color;
    Console.WriteLine($"\n[{name}]:");
    Console.ResetColor();

    // 工具呼叫與 THOUGHT 區塊的 Regex 模式
    // Group 1: Tool Calls
    // Group 2: THOUGHT blocks (capture [THOUGHT] ... until next tag or end, but usually just coloring the tag line or the block if possible)
    // Simplified: Split by Tool Call OR [THOUGHT]...[/THOUGHT] if structured, but here it's likely just text.
    // Let's match [THOUGHT] content generally. 
    // New Pattern: (ToolCall) | (ThoughtBlock)
    string tokenPattern = @"(file_system\.(?:WriteFile|ReadFile|ListFiles)\s*\(.*?\)|shell\.RunShellCommand\s*\(.*?\))|(\[THOUGHT\].*?)(?=\[|$|file_system|shell)";

    // The previous regex split might be too simple for overlapping/nested. 
    // Let's try a linear parsing approach with a robust Regex.
    // Matches:
    // 1. Tool Call
    // 2. [THOUGHT] ... (Non-greedy until end or next keyword, hard to do perfectly without specific end tag)
    // Let's assume [THOUGHT] is followed by text. We can color lines starting with [THOUGHT] or the whole block if explicit.
    // Given the prompt instruction: "output `[THOUGHT]` block".

    // Better strategy: Split by Tool Pattern first. Then inside non-tool parts, check for [THOUGHT].

    var parts = System.Text.RegularExpressions.Regex.Split(content, @"(file_system\.(?:WriteFile|ReadFile|ListFiles)\s*\(.*?\)|shell\.RunShellCommand\s*\(.*?\))", System.Text.RegularExpressions.RegexOptions.Singleline);

    foreach (var part in parts)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^file_system\.|^shell\.", System.Text.RegularExpressions.RegexOptions.Singleline))
        {
            // 高亮顯示工具指令 (白字深藍底)
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(part);
            Console.ResetColor();
        }
                else
                {
                    // 在一般文字中處理 [THOUGHT] 到 [END OF THOUGHT] 或段落結尾
                    string currentPart = part;
                    
                    while (!string.IsNullOrEmpty(currentPart))
                    {
                        int thoughtStart = currentPart.IndexOf("[THOUGHT]");
                        if (thoughtStart >= 0)
                        {
                            // 1. 輸出 THOUGHT 之前的文字
                            Console.ForegroundColor = (name == "System_Interceptor") ? ConsoleColor.Blue : ConsoleColor.Gray;
                            Console.Write(currentPart.Substring(0, thoughtStart));
                            
                                                // 2. 尋找結束標籤 (支援 [END OF THOUGHT] 或 [/THOUGHT])
                                                string remaining = currentPart.Substring(thoughtStart);
                                                string[] endTags = { "[END OF THOUGHT]", "[/THOUGHT]" };
                                                int thoughtEnd = -1;
                                                string matchedTag = "";
                            
                                                foreach (var tag in endTags)
                                                {
                                                    int index = remaining.IndexOf(tag);
                                                    if (index >= 0 && (thoughtEnd == -1 || index < thoughtEnd))
                                                    {
                                                        thoughtEnd = index;
                                                        matchedTag = tag;
                                                    }
                                                }
                                                
                                                if (thoughtEnd >= 0)
                                                {
                                                    // 有結束標籤：高亮中間部分
                                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                                    int fullEndIndex = thoughtEnd + matchedTag.Length;
                                                    Console.Write(remaining.Substring(0, fullEndIndex));
                                                    
                                                    // 準備處理剩下的內容
                                                    currentPart = remaining.Substring(fullEndIndex);
                                                }                            else
                            {
                                // 沒有結束標籤：高亮剩下所有內容
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Write(remaining);
                                currentPart = "";
                            }
                        }
                        else
                        {
                            // 沒有 THOUGHT 標籤：正常輸出
                            Console.ForegroundColor = (name == "System_Interceptor") ? ConsoleColor.Blue : ConsoleColor.Gray;
                            Console.Write(currentPart);
                            currentPart = "";
                        }
                    }
                }    }

    Console.WriteLine();
    Console.ResetColor();
    Console.WriteLine(new string('-', 30));
}