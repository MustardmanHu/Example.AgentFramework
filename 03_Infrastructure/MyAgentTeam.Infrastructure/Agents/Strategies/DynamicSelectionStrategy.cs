namespace MyAgentTeam.Infrastructure.Agents.Strategies
{
    using System.Threading.Tasks;
    // Assuming usage of Microsoft.Agents.AI or similar abstractions
    
    /// <summary>
    /// 動態選擇策略，根據對話歷史動態決定下一位發言者。
    /// </summary>
    public class DynamicSelectionStrategy
    {
        /// <summary>
        /// 選擇下一位發言者。
        /// </summary>
        /// <param name="currentSpeaker">目前的發言者。</param>
        /// <param name="history">對話歷史紀錄。</param>
        /// <returns>下一位發言者的名稱。</returns>
        public Task<string> SelectNextSpeakerAsync(string currentSpeaker, string history)
        {
            // Simple logic: just rotate or return a specific agent based on history
            // In a real scenario, this would use an LLM to decide
            return Task.FromResult("Researcher"); 
        }
    }
}