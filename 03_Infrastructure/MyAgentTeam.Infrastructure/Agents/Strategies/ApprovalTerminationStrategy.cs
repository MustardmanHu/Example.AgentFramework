namespace MyAgentTeam.Infrastructure.Agents.Strategies
{
    using System.Threading.Tasks;

    /// <summary>
    /// 核准終止策略，當訊息中包含特定關鍵字時終止對話。
    /// </summary>
    public class ApprovalTerminationStrategy
    {
        /// <summary>
        /// 判斷是否應該終止對話。
        /// </summary>
        /// <param name="lastMessage">最後一則訊息的內容。</param>
        /// <returns>若應終止則回傳 true，否則回傳 false。</returns>
        public Task<bool> ShouldTerminateAsync(string lastMessage)
        {
            // Terminate if "TERMINATE" is found
            if (lastMessage.Contains("TERMINATE"))
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}