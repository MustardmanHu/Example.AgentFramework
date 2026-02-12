namespace MyAgentTeam.Core.Models
{
    /// <summary>
    /// 代表 Agent 的回應模型。
    /// </summary>
    public class AgentResponse
    {
        /// <summary>
        /// 指示操作是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 操作的輸出結果。
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// 如果操作失敗，則包含錯誤訊息。
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}