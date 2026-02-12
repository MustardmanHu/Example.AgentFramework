
using AgentFrameworkwithGemini.Agents;
using GenerativeAI;
using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;

internal class Program
{
    private static readonly string apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ??
        throw new InvalidOperationException("API Key not found. Please set the GOOGLE_API_KEY environment variable.");

    private static void Main(string[] args)
    {
        var client = new GenerativeAIChatClient(apiKey: apiKey, modelName: GoogleAIModels.GeminiProLatest);

        var agentCreator = new AgentCreator(client);

        var supervisorAgent = agentCreator.CreateNewAgent(
            name: "Supervisor",
            instructions: "你是軟體開發主管。職責：指揮 System_Designer, DBA, Programmer 工作。審查產出，只有當所有程式碼和設計都符合需求時，回復 'TERMINATE'。"
        );

        var systemDesignerAgent = agentCreator.CreateNewAgent(
            name: "System_Designer",
            instructions: "你是資深系統架構師。職責：根據需求設計系統架構、Mermaid 圖表。"
        );

        var dbaAgent = agentCreator.CreateNewAgent(
            name: "DBA",
            instructions: "你是資深資料庫管理員。職責：根據需求設計資料庫結構和 SQL 查詢語句，設計 SQL Schema，專注於調整效能與正規化。"
        );

        var programmerAgent = agentCreator.CreateNewAgent(
            name: "Programmer",
            instructions: "你是資深軟體工程師。職責：根據需求和設計文件編寫高品質程式碼，確保程式碼符合最佳實踐和效能要求。"
        );

        var researcherAgent = agentCreator.CreateNewAgent(
            name: "Researcher",
            instructions: "你是資深研究員。職責：根據需求進行技術調研，提供最新的技術解決方案和建議。"
        );

        var groupChat = new GroupChat()
        {
            members = [
                supervisorAgent,
                systemDesignerAgent,
                dbaAgent,
                programmerAgent,
                researcherAgent
            ],
            Topic = args
        };
    }
}