namespace MyAgentTeam.Core.Interfaces;

/// <summary>
/// Agent 協調器介面，負責管理 Agent 團隊的執行流程。
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// 執行 Agent 協作流程，根據使用者的目標進行。
    /// </summary>
    /// <param name="userGoal">使用者的目標描述。</param>
    /// <param name="isNewProject">是否為新專案 (true: 從頭建立, false: 維護既有專案)。</param>
    /// <returns>一個非同步的可列舉序列，包含 Agent 的名稱與回應內容。</returns>
    IAsyncEnumerable<(string AgentName, string Content)> ExecuteAsync(string userGoal, bool isNewProject);
}