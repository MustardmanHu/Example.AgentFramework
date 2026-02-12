using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.RegularExpressions;

namespace MyAgentTeam.Infrastructure.Agents.Strategies;

/// <summary>
/// 摘要式選擇策略，負責在對話歷史過長時進行壓縮，並選擇下一位發言者。
/// </summary>
public partial class SummarizingSelectionStrategy : SelectionStrategy
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _selectionFunction;
    private readonly int _historyWindowSize;
    private string _currentSummary = "";
    private int _lastSummarizedCount = 0;

    /// <summary>
    /// 設定預設的 Prompt 執行參數。
    /// </summary>
    private readonly OpenAIPromptExecutionSettings _defaultSettings = new()
    {
        // 溫度: 0 表示最保守的回答，避免無用贅字
        Temperature = 0
    };

    /// <summary>
    /// 初始化 <see cref="SummarizingSelectionStrategy"/> 類別的新實例。
    /// </summary>
    /// <param name="kernel">Kernel 實例。</param>
    /// <param name="selectionFunction">用於選擇下一位 Agent 的 Kernel Function。</param>
    /// <param name="historyWindowSize">保留原始對話的視窗大小 (每 N 筆對話壓縮一次)。</param>
    public SummarizingSelectionStrategy(Kernel kernel, KernelFunction selectionFunction, int historyWindowSize = 3)
    {
        _kernel = kernel;
        _selectionFunction = selectionFunction;
        _historyWindowSize = historyWindowSize;
    }

    /// <summary>
    /// 根據對話歷史選擇下一位 Agent。
    /// </summary>
    /// <param name="agents">可用的 Agent 列表。</param>
    /// <param name="history">對話歷史紀錄。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>選定的 Agent。</returns>
    protected override async Task<Agent> SelectAgentAsync(
        IReadOnlyList<Agent> agents,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default
        )
    {
        // 準備壓縮後的歷史紀錄字串
        string compressedHistory = await GetCompressedHistoryAsync(history);

        // 設定參數
        var arguments = new KernelArguments(_defaultSettings)
        {
            ["history"] = compressedHistory,
            ["agents"] = string.Join(", ", agents.Select(a => a.Name))
        };

        // 呼叫選擇人選函式
        var result = await _selectionFunction.InvokeAsync(_kernel, arguments, cancellationToken);
        string output = result.GetValue<string>() ?? "";

        // 解析 Agent 名稱 (更加強壯的解析邏輯)
        string? selectedName = null;

        // 嘗試 Regex 抓取 <agent> 標籤
        var match = AgentRegex().Match(output);
        if (match.Success)
        {
            selectedName = match.Groups[1].Value.Trim();
        }

        // 如果失敗，嘗試在整個字串中尋找已知的 Agent 名稱
        if (string.IsNullOrEmpty(selectedName))
        {
            foreach (var agent in agents)
            {
                if (output.Contains(agent.Name!, StringComparison.OrdinalIgnoreCase))
                {
                    selectedName = agent.Name;
                    break;
                }
            }
        }

        // 根據名稱回傳對應的 Agent 物件
        var selectedAgent = agents.FirstOrDefault(a => a.Name!.Equals(selectedName, StringComparison.OrdinalIgnoreCase));

        if (selectedAgent == null)
        {
            return agents.First(a => a.Name == "Supervisor");
        }

        return selectedAgent;
    }

    /// <summary>
    /// 取得壓縮後的歷史紀錄。
    /// </summary>
    /// <param name="history">原始對話歷史。</param>
    /// <returns>包含摘要與近期對話的字串。</returns>
    private async Task<string> GetCompressedHistoryAsync(IReadOnlyList<ChatMessageContent> history)
    {
        // 如果歷史紀錄很少，直接回傳原始內容
        if (history.Count <= _historyWindowSize)
        {
            return FormatHistory(history);
        }

        // 計算需要摘要的部分：除了最後 N 筆之外的所有訊息
        int keepIndex = Math.Max(0, history.Count - _historyWindowSize);
        var messagesToSummarize = history.Take(keepIndex).ToList();
        var recentMessages = history.Skip(keepIndex).ToList();

        // 優化：每累積 3 筆舊訊息才重新摘要一次，確保頻率符合要求
        if (messagesToSummarize.Count >= _lastSummarizedCount + 3)
        {
            // 執行摘要
            _currentSummary = await SummarizeMessagesAsync(messagesToSummarize);
            _lastSummarizedCount = messagesToSummarize.Count;
        }

        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(_currentSummary))
        {
            sb.AppendLine("=== 先前對話重點紀錄 (Compressed Context) ===");
            sb.AppendLine(_currentSummary);
            sb.AppendLine("============================================");
        }

        sb.AppendLine(FormatHistory(recentMessages));

        return sb.ToString();
    }

    /// <summary>
    /// 將對話訊息格式化為字串 (Name: Content)。
    /// </summary>
    /// <param name="messages">訊息集合。</param>
    /// <returns>格式化後的對話字串。</returns>
    private string FormatHistory(IEnumerable<ChatMessageContent> messages)
    {
        StringBuilder sb = new();
        foreach (var msg in messages)
        {
            var name = msg.AuthorName ?? msg.Role.ToString();
            var content = msg.Content ?? "";
            sb.AppendLine($"{name}: {content}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 使用 LLM 執行對話摘要。
    /// </summary>
    /// <param name="messages">要摘要的訊息列表。</param>
    /// <returns>摘要文字。</returns>
    private async Task<string> SummarizeMessagesAsync(List<ChatMessageContent> messages)
    {
        if (messages.Count == 0) return "";

        string textToSummarize = FormatHistory(messages);

        var summarizePrompt = @"
            請將以下對話紀錄進行【無損壓縮 (Lossless Compression)】整理。
            
            **步驟 1: 分析 (Think)**
            先在腦中分析：哪些是廢話？哪些是關鍵技術決策？哪些是檔案變更紀錄？

            **步驟 2: 摘要 (Summarize)**
            請輸出一段精簡的重點整理。

            **絕對要求 (CRITICAL REQUIREMENTS):**
            1. **禁止失真**：必須保留所有 **檔案路徑**、**函式名稱**、**錯誤代碼**、**SQL指令**。
            2. **保留狀態**：明確記錄每個 Agent 的最後動作 (例如：Programmer 修改了 `Program.cs`，QA 回報 `Test1` 失敗)。
            3. **禁止過度概括**：
               - ❌ 錯誤：Programmer 修改了程式碼。
               - ✅ 正確：Programmer 在 `Services/UserService.cs` 新增了 `ValidateUser` 方法。
            4. **保留決策關鍵字**：**QA_PASSED**, **APPROVED**, **REJECTED**, **HANDOFF** 必須完整保留。

            不需要對話格式，請以條列式重點呈現，確保下一個閱讀的人能完全接軌狀況。

            對話紀錄:
            {{$input}}
        ";

        var summarizeFunc = _kernel.CreateFunctionFromPrompt(summarizePrompt);
        var result = await summarizeFunc.InvokeAsync(_kernel, new KernelArguments(_defaultSettings) { ["input"] = textToSummarize });

        return result.GetValue<string>() ?? "";
    }

    [GeneratedRegexAttribute(@"<agent>(.*?)</agent>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "zh-TW")]
    private partial Regex AgentRegex();
}