namespace MyAgentTeam.Core.Interfaces
{
    using System.Threading.Tasks;

    /// <summary>
    /// 研究工具介面，定義搜尋功能。
    /// </summary>
    public interface IResearchTool
    {
        /// <summary>
        /// 執行非同步搜尋。
        /// </summary>
        /// <param name="query">搜尋關鍵字。</param>
        /// <returns>搜尋結果的字串內容。</returns>
        Task<string> SearchAsync(string query);
    }
}