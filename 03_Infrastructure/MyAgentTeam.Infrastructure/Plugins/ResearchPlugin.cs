using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace MyAgentTeam.Infrastructure.Plugins;

/// <summary>
/// Google 搜尋插件，用於搜尋最新的技術資訊或文件。
/// </summary>
public class ResearchPlugin
{
    private readonly string _apiKey;
    private readonly string _searchEngineId;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string> _searchHistory = new();

    /// <summary>
    /// 初始化 <see cref="ResearchPlugin"/> 類別的新實例。
    /// </summary>
    /// <param name="apiKey">Google Search API 金鑰。</param>
    /// <param name="searchEngineId">Google Search Engine ID。</param>
    /// <param name="httpClient">HttpClient 實例。</param>
    public ResearchPlugin(string apiKey, string searchEngineId, HttpClient httpClient)
    {
        _apiKey = apiKey;
        _searchEngineId = searchEngineId;
        _httpClient = httpClient;
    }

    /// <summary>
    /// 搜尋 Google 以取得最新的技術資訊或文件。支援分頁。
    /// </summary>
    /// <param name="query">搜尋關鍵字。</param>
    /// <param name="count">回傳筆數 (預設 10，最大 10)。</param>
    /// <param name="startIndex">開始索引 (分頁用，第一頁為 1，第二頁為 11...）。</param>
    /// <returns>搜尋結果的字串內容。</returns>
    [KernelFunction("Search"), Description("搜尋 Google 以取得最新的技術資訊或文件。支援分頁。")]
    public async Task<string> SearchAsync(
        [Description("搜尋關鍵字")] string query,
        [Description("回傳筆數 (1-10)")] int count = 10,
        [Description("開始索引 (分頁用，第一頁為 1，第二頁為 11...)")] int startIndex = 1)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_searchEngineId) || _apiKey.Contains("YOUR_"))
        {
            return "錯誤: Google Search API 未正確設定。請檢查 AppSettings.json 中的 GoogleSearch 區段。";
        }

        // 限制 count 範圍
        count = Math.Clamp(count, 1, 10);
        // 限制 startIndex 最小值
        startIndex = Math.Max(1, startIndex);

        // --- 避免死循環檢查 ---
        string searchKey = $"{query}|{count}|{startIndex}";
        string warningMsg = "";
        
        if (_searchHistory.Contains(searchKey))
        {
            warningMsg = "\n\n[System Warning]: 你已經執行過完全相同的搜尋 (Query/Count/Page)。為了避免無效的死循環，請更改你的搜尋關鍵字或參數！\n";
        }
        else
        {
            _searchHistory.Add(searchKey);
        }
        // -----------------------

        try
        {
            var url = $"https://www.googleapis.com/customsearch/v1?key={_apiKey}&cx={_searchEngineId}&q={Uri.EscapeDataString(query)}&num={count}&start={startIndex}";

            // 使用建構式注入的 HttpClient 發送請求
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(content);
            var items = jsonNode?["items"]?.AsArray();

            if (items == null || items.Count == 0)
            {
                return $"找不到關於 '{query}' 的相關結果 (Start: {startIndex})。{warningMsg}";
            }

            var resultBuilder = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(warningMsg))
            {
                resultBuilder.AppendLine(warningMsg);
            }
            resultBuilder.AppendLine($"Google 搜尋結果 ('{query}', Start: {startIndex}, Count: {items.Count}):");

            foreach (var item in items)
            {
                var title = item?["title"]?.ToString() ?? "無標題";
                var snippet = item?["snippet"]?.ToString() ?? "無摘要";
                var link = item?["link"]?.ToString() ?? "#";

                resultBuilder.AppendLine($"- **{title}**");
                resultBuilder.AppendLine($"  {snippet}");
                resultBuilder.AppendLine($"  連結: {link}");
                resultBuilder.AppendLine();
            }

            return resultBuilder.ToString();
        }
        catch (Exception ex)
        {
            return $"搜尋執行時發生錯誤: {ex.Message}";
        }
    }
}
