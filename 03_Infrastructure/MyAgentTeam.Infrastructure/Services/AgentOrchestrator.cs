using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using MyAgentTeam.Core.Interfaces;
using MyAgentTeam.Infrastructure.Agents;
using System.Text.RegularExpressions;

namespace MyAgentTeam.Infrastructure.Services;

public partial class AgentOrchestrator(Kernel kernel) : IAgentOrchestrator
{
    public async IAsyncEnumerable<(string AgentName, string Content)> ExecuteAsync(string userGoal, bool isNewProject)
    {
        // 0. 載入指令
        string sharedInstructions = await LoadInstructionsAsync();

        // 1. 建立 Agent
        var supervisor = AgentDefinitions.CreateSupervisor(kernel, sharedInstructions);
        var designer = AgentDefinitions.CreateDesigner(kernel, sharedInstructions);
        var dba = AgentDefinitions.CreateDBA(kernel, sharedInstructions);
        var programmer = AgentDefinitions.CreateProgrammer(kernel, sharedInstructions);
        var secondProgrammer = AgentDefinitions.CreateProgrammerSecond(kernel, sharedInstructions);
        var researcher = AgentDefinitions.CreateResearcher(kernel, sharedInstructions);
        var tester = AgentDefinitions.CreateTester(kernel, sharedInstructions);
        var qa = AgentDefinitions.CreateQA(kernel, sharedInstructions);

        // 2. 準備 Agent 列表以便存取
        var agents = new Agent[] { supervisor, designer, dba, researcher, programmer, secondProgrammer, tester, qa };

        // 3. 設定策略
        string selectionPrompt = isNewProject ? GetNewProjectSelectionPrompt : GetExistingProjectSelectionPrompt;
        KernelFunction selectionFunc = kernel.CreateFunctionFromPrompt(selectionPrompt);

        // [效能優化] 在迴圈外建立策略與聊天室實例，避免重複建立開銷
        var selectionStrategy = new DynamicSelectionStrategy(kernel, selectionFunc, historyWindowSize: 3);
        var terminationStrategy = new ToolAwareTerminationStrategy();

#pragma warning disable SKEXP0110
        AgentGroupChat chat = new(agents)
        {
            ExecutionSettings = new()
            {
                SelectionStrategy = selectionStrategy,
                TerminationStrategy = terminationStrategy,
            }
        };
#pragma warning restore SKEXP0110

        // 加入初始使用者目標
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userGoal));

        bool isProjectComplete = false;
        const int MaxHistoryCount = 20; // 觸發壓縮的閾值
        const int KeepRecentCount = 10;  // 保留的最近訊息數

        // 5. 主要執行迴圈
        while (!isProjectComplete)
        {
            // 執行聊天
            await foreach (var msg in chat.InvokeAsync())
            {
                if (msg == null || string.IsNullOrWhiteSpace(msg.Content)) continue;
                yield return (msg.AuthorName ?? "Unknown", msg.Content ?? "");
            }

            // [效能優化] 直接從聊天室實例取得歷史紀錄
            var history = await chat.GetChatMessagesAsync().Reverse().ToListAsync();
            var lastMsg = history.LastOrDefault();

            if (lastMsg == null)
            {
                break;
            }

            // [效能優化] 檢查訊息數量是否過多，若過多則進行壓縮並重建聊天室
            if (history.Count > MaxHistoryCount)
            {
                // [效能優化] 壓縮策略：保留初始目標 + 最近對話，避免任務失真

                // 1. 保留初始目標 (通常是第一則訊息)
                var firstMessage = history.FirstOrDefault();

                // 2. 保留最近的 N 則訊息
                var recentMessages = history.TakeLast(KeepRecentCount).ToList();

                // 3. 建立總結與過渡訊息 (使用 Assistant 角色避免 SK 限制)
                var summaryMessage = new ChatMessageContent(AuthorRole.Assistant,
                    $"[系統通知]: 為了釋放記憶體，中間的對話歷史已被封存。初始目標已保留，請根據最近的對話繼續執行任務。")
                {
                    AuthorName = "System_Memory"
                };

                // 重建列表
                var newHistory = new List<ChatMessageContent>();
                if (firstMessage != null) newHistory.Add(firstMessage);
                newHistory.Add(summaryMessage);
                foreach (var m in recentMessages)
                {
                    if (m != firstMessage) newHistory.Add(m);
                }
                history = newHistory;
            }

            string content = lastMsg.Content ?? "";
            var toolMessages = new List<ChatMessageContent>();

            // 檢查終止原因
            if (content.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("PROJECT_FAILED", StringComparison.OrdinalIgnoreCase))
            {
                isProjectComplete = true;
                break;
            }
            else if (HasToolCall(content))
            {
                // 執行工具
                var results = await InterceptAndExecuteToolAsync(kernel, content, lastMsg.AuthorName ?? "Unknown");

                // 將結果投影為訊息物件 (使用 Assistant 角色，因 System 角色不支援動態加入)
                toolMessages = results.Select(res => new ChatMessageContent(AuthorRole.Assistant, res ?? "") { AuthorName = "System_Interceptor" }).ToList();

                foreach (var toolMsg in toolMessages)
                {
                    yield return ("System_Interceptor", toolMsg.Content ?? "");
                }
            }

            selectionStrategy = new DynamicSelectionStrategy(kernel, selectionFunc, historyWindowSize: 3);
            terminationStrategy = new ToolAwareTerminationStrategy();

#pragma warning disable SKEXP0110
            chat = new AgentGroupChat(agents)
            {
                ExecutionSettings = new()
                {
                    SelectionStrategy = selectionStrategy,
                    TerminationStrategy = terminationStrategy,
                }
            };
#pragma warning restore SKEXP0110

            // Restore History with CLONED messages to prevent SK internal state issues
            foreach (var msg in history)
            {
                if (string.IsNullOrWhiteSpace(msg.Content)) continue;

                var newMsg = new ChatMessageContent(msg.Role, msg.Content ?? "")
                {
                    AuthorName = msg.AuthorName
                };

                chat.AddChatMessage(newMsg);
            }
            foreach (var tm in toolMessages)
            {
                chat.AddChatMessage(new ChatMessageContent(tm.Role, tm.Content ?? "") { AuthorName = tm.AuthorName });
            }
        }
    }

    /// <summary>
    /// 取得Instruction.md內容
    /// </summary>
    /// <returns></returns>
    private static async Task<string> LoadInstructionsAsync()
    {
        // 優先讀取使用者指定的路徑
        var paths = new[]
        {
            @"/d/Project/Instructions/Instruction.md",
            @"D:\Project\Instructions\Instruction.md",
            Path.Combine(AppContext.BaseDirectory, "Instruction.md")
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
        }

        return "";
    }

    /// <summary>
    /// 是否有呼叫Plugin工具
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private bool HasToolCall(string content)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(content, @"file_system\.(WriteFile|ReadFile|ListFiles)|shell\.RunShellCommand");
    }

    /// <summary>
    /// 攔截執行工具指令並執行
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="content"></param>
    /// <param name="agentName"></param>
    /// <returns></returns>
    private static async Task<List<string>> InterceptAndExecuteToolAsync(Kernel kernel, string content, string agentName)
    {
        var results = new List<string>();
        try
        {
            var filePlugin = kernel.Plugins["file_system"];

            // WriteFile
            var writeMatches = WriteFileRegex().Matches(content);
            foreach (Match m in writeMatches)
            {
                string path = m.Groups[1].Value;
                if (agentName == "QA" || agentName == "Supervisor")
                {
                    results.Add($"[Error]: Agent '{agentName}' is not authorized to write files.");
                    continue;
                }

                // Special restriction for System_Designer: Only .md or .txt
                if (agentName == "System_Designer")
                {
                    string ext = System.IO.Path.GetExtension(path).ToLower();
                    if (ext != ".md" && ext != ".txt")
                    {
                        results.Add($"[Error]: Agent '{agentName}' is ONLY authorized to write documentation files (.md, .txt). Access to '{path}' is denied.");
                        continue;
                    }
                }

                FunctionResult? res;

                if (!string.IsNullOrEmpty(m.Groups[3].Value))
                {
                    res = await filePlugin["WriteFile"].InvokeAsync(kernel,
                    new KernelArguments
                    {
                        ["relativePath"] = m.Groups[1].Value,
                        ["content"] =
                            !string.IsNullOrEmpty(m.Groups[2].Value) ?
                            m.Groups[2].Value :
                            m.Groups[3].Value
                    });
                }
                else
                {
                    res = await filePlugin["WriteFile"].InvokeAsync(kernel,
                    new KernelArguments
                    {
                        ["relativePath"] = m.Groups[1].Value,
                        ["content"] =
                            !string.IsNullOrEmpty(m.Groups[2].Value) ?
                            m.Groups[2].Value :
                            m.Groups[4].Value
                    });
                }

                results.Add($"[Tool Result (WriteFile - {m.Groups[1].Value})]: {res}");
            }

            // ReadFile
            var readMatches = ReadFileRegex().Matches(content);

            foreach (Match m in readMatches)
            {
                var res = await filePlugin["ReadFile"].InvokeAsync(kernel, new KernelArguments { ["relativePath"] = m.Groups[1].Value });
                results.Add($"[Tool Result (ReadFile - {m.Groups[1].Value})]: {res}");
            }

            // ListFiles
            if (content.Contains("file_system.ListFiles()"))
            {
                var res = await filePlugin["ListFiles"].InvokeAsync(kernel, []);
                results.Add($"[Tool Result (ListFiles)]: {res}");
            }

            // Shell
            var shellMatches = ShellCommandRegex().Matches(content);

            foreach (Match m in shellMatches)
            {
                string rawCommand = !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value :
                                    !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value :
                                    m.Groups[3].Value;

                var subCommands = rawCommand.Split(["\r\n", "\n", "&&", ";"], StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmd in subCommands)
                {
                    string cleanCmd = cmd.Trim();

                    // Skip empty lines or comments
                    if (string.IsNullOrWhiteSpace(cleanCmd) || cleanCmd.StartsWith("#"))
                    {
                        continue;
                    }

                    if (cleanCmd.StartsWith("git"))
                    {
                        results.Add("[Error]: git commands are not allowed.");
                        continue;
                    }

                    try
                    {
                        // Execute atomic command
                        var res = await kernel.Plugins["shell"]["RunShellCommand"].InvokeAsync(kernel, new KernelArguments { ["command"] = cleanCmd });
                        results.Add($"[Tool Result (Shell - {cleanCmd})]: {res}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"[Tool Error (Shell - {cleanCmd})]: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            results.Add($"[System Interceptor Error]: {ex.Message}");
        }
        return results;
    }

    private static string GetNewProjectSelectionPrompt => @"
            You are the GroupChatManager. Your single responsibility is to select the next speaker.
            Agents: {{$agents}}
            History:
            {{$history}}
            
            **Rules for Agent Selection:**
            1. **HANDOFF PRIORITY**: If the last message contains `[HANDOFF TO {AgentName}]`, you **MUST** select `{AgentName}`.
            2. **DEFAULT FLOW**:
                - **Supervisor/User** -> **System_Designer**.
                - **System_Designer** -> **DBA** or **Programmer**.
                - **DBA** -> **Programmer**.
                - **Programmer** -> **Tester**.
                - **Tester** -> **QA**.
                - **QA** (PASSED) -> **Supervisor**.
            
            **Your output MUST be ONLY the agent name wrapped in <agent> tags.**
            Example: <agent>Supervisor</agent>";

    private static string GetExistingProjectSelectionPrompt => @"
            You are the GroupChatManager. Your single responsibility is to select the next speaker.
            Agents: {{$agents}}
            History:
            {{$history}}
            
            **Rules for Agent Selection:**
            1. **HANDOFF PRIORITY**: If the last message contains `[HANDOFF TO {AgentName}]`, you **MUST** select `{AgentName}`.
            2. **INITIALIZATION**: If the history only contains a single user message, you **MUST** select `Supervisor`.
            3. **DEFAULT FLOW**:
                - **Supervisor** -> **System_Designer**, **DBA**, or **Programmer**.
                - **Programmer** -> **Tester**.
                - **Tester** -> **QA**.
                - **QA** (if passed) -> **Supervisor**.
                - **QA** (if failed) -> **Programmer**.

            **Your output MUST be ONLY the agent name wrapped in <agent> tags.**
            Example: <agent>Programmer</agent>";

    private class DynamicSelectionStrategy(Kernel kernel, KernelFunction selectionFunction, int historyWindowSize) : SelectionStrategy
    {
        protected override async Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
        {
            // 0. 優先檢查最近的訊息是否有明確的交接指令 [HANDOFF TO X]
            // 必須跳過 "System_Interceptor" 的工具回報訊息，往前追溯真正發言的 Agent
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var msg = history[i];
                // 只追溯最近 5 則，避免翻舊帳
                if (history.Count - i > 5) break;

                // 跳過工具回報訊息
                if (msg.AuthorName == "System_Interceptor") continue;

                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    var handoffMatch = System.Text.RegularExpressions.Regex.Match(msg.Content, @"\[HANDOFF TO\s+(.*?)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (handoffMatch.Success)
                    {
                        string targetAgentName = handoffMatch.Groups[1].Value.Trim().TrimEnd('.', '!', '?');
                        var targetAgent = agents.FirstOrDefault(a => a.Name.Equals(targetAgentName, StringComparison.OrdinalIgnoreCase));

                        if (targetAgent != null)
                        {
                            // 找到了交接指令，直接指派
                            return targetAgent;
                        }
                    }
                    // 如果遇到了一則非 System 的訊息但沒有 Handoff，通常表示這則訊息是主要的對話內容
                    // 我們應該在這裡停止搜尋，除非我們允許多次發言後才交接。
                    // 但為了安全起見，如果最近的一個真人 Agent 沒說交接，那就是沒交接。
                    break;
                }
            }

            // 1. [強制規則] 確保流程絕對由 Supervisor 啟動
            // 當歷史訊息只有 1 則 (使用者的初始需求) 時，不經過 LLM 判斷，直接指定 Supervisor。
            if (history.Count <= 1)
            {
                return agents.First(a => a.Name == "Supervisor");
            }

            // 2. [黏著性規則 (Agent Stickiness)]
            // 如果上一位發言者不是 "Supervisor" 且不是 "System_Interceptor"，
            // 並且沒有發出 [HANDOFF] 指令，通常意味著他還在思考或執行步驟中。
            // 我們應該讓他繼續發言，而不是切回 Supervisor。

            var lastRealMsg = history.LastOrDefault(m => m.AuthorName != "System_Interceptor");
            if (lastRealMsg != null && lastRealMsg.AuthorName != "Supervisor" && lastRealMsg.AuthorName != "User")
            {
                // 檢查是否含有 Handoff
                bool hasHandoff = System.Text.RegularExpressions.Regex.IsMatch(lastRealMsg.Content ?? "", @"\[HANDOFF TO\s+.*?\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 檢查是否已結束
                bool isDone = (lastRealMsg.Content ?? "").Contains("APPROVED", StringComparison.OrdinalIgnoreCase);

                if (!hasHandoff && !isDone)
                {
                    var currentAgent = agents.FirstOrDefault(a => a.Name == lastRealMsg.AuthorName);
                    if (currentAgent != null)
                    {
                        return currentAgent;
                    }
                }
            }

            var arguments = new KernelArguments(); arguments["agents"] = string.Join(", ", agents.Select(a => a.Name));

            // Simple history serialization
            var relevantHistory = history.TakeLast(historyWindowSize);
            var historyText = string.Join("\n", relevantHistory.Select(m => $"{m.AuthorName}: {m.Content}"));
            arguments["history"] = historyText;

            var result = await selectionFunction.InvokeAsync(kernel, arguments, cancellationToken);
            string resultString = result.GetValue<string>() ?? "";

            // Parse <agent>Name</agent>
            var match = System.Text.RegularExpressions.Regex.Match(resultString, @"<agent>(.*?)</agent>");
            string agentName = match.Success ? match.Groups[1].Value : resultString;
            agentName = agentName.Trim();

            return agents.FirstOrDefault(a => a.Name.Equals(agentName, StringComparison.OrdinalIgnoreCase)) ?? agents.First();
        }
    }

    public class ToolAwareTerminationStrategy : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        {
            string content = history.Last().Content ?? "";

            if (content.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("PROJECT_FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(true);
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"file_system\.(WriteFile|ReadFile|ListFiles)|shell\.RunShellCommand"))
            {
                return Task.FromResult(true);
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"\[HANDOFF TO\s+.*?\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"file_system\.WriteFile\s*\(\s*relativePath\s*=\s*['""](.*?)['""]\s*,\s*content\s*=\s*(?:'''(.*?)'''|""""""(.*?)""""""|['""](.*?)['""])\s*\)", System.Text.RegularExpressions.RegexOptions.Singleline)]
    private static partial System.Text.RegularExpressions.Regex WriteFileRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"shell\.RunShellCommand\s*\(\s*command\s*=\s*(?:'''(.*?)'''|""""""(.*?)""""""|['""](.*?)['""])\s*\)", System.Text.RegularExpressions.RegexOptions.Singleline)]
    private static partial System.Text.RegularExpressions.Regex ShellCommandRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"file_system\.ReadFile\s*\(\s*relativePath\s*=\s*['""](.*?)['""]\s*\)", System.Text.RegularExpressions.RegexOptions.Singleline)]
    private static partial System.Text.RegularExpressions.Regex ReadFileRegex();
}