using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using MyAgentTeam.Core.Interfaces;
using MyAgentTeam.Infrastructure.Plugins;
using MyAgentTeam.Infrastructure.Services;

namespace MyAgentTeam.Infrastructure.Configuration;

/// <summary>
/// 擴充方法，用於註冊 Agent 基礎設施相關的服務。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 Agent 基礎設施服務，包含 Kernel、Plugins 與 Orchestrator。
    /// </summary>
    /// <param name="services">IServiceCollection 實例。</param>
    /// <param name="geminiApiKey">Gemini API 金鑰。</param>
    /// <param name="modelId">Gemini 模型 ID。</param>
    /// <param name="googleSearchApiKey">Google Search API 金鑰。</param>
    /// <param name="googleSearchEngineId">Google Search Engine ID。</param>
    /// <param name="projectPath">專案生成的根目錄路徑。</param>
    /// <returns>更新後的 IServiceCollection。</returns>
    public static IServiceCollection AddAgentInfrastructure(
        this IServiceCollection services,
        string geminiApiKey,
        string modelId,
        string googleSearchApiKey,
        string googleSearchEngineId,
        string projectPath)
    {
        // 1. 確保專案目錄存在或建立專案目錄，若無法建立則回退到本地目錄
        if (!Directory.Exists(projectPath))
        {
            try
            {
                Directory.CreateDirectory(projectPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to create project directory at {projectPath}. Fallback to local _GeneratedCode. Error: {ex.Message}");
                projectPath = Path.Combine(Directory.GetCurrentDirectory(), "_GeneratedCode");
                if (!Directory.Exists(projectPath)) Directory.CreateDirectory(projectPath);
            }
        }

        // 2. 設定 HttpClient (包含重試機制)
        // ---------------------------------------------------------

        // Handler
        HttpMessageHandler pipeline = new HttpClientHandler();

        // Add Retry Handler (RateLimitRetryHandler)
        var retryHandler = new RateLimitRetryHandler
        {
            InnerHandler = pipeline
        };

        // HttpClient
        HttpClient robustClient = new HttpClient(retryHandler)
        {
            Timeout = TimeSpan.FromMinutes(5) // Timeout
        };
        // ---------------------------------------------------------

        // 5. 建立Kernel
        services.AddTransient<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            // 3. 建立 Gemini Chat Completion Service
#pragma warning disable SKEXP0070 
            builder.AddGoogleAIGeminiChatCompletion(
                modelId: modelId,
                apiKey: geminiApiKey,
                httpClient: robustClient
            );
#pragma warning restore SKEXP0070 

            // 4. Add Plugins
            // Add ResearchPlugin
            var researchPlugin = new ResearchPlugin(googleSearchApiKey, googleSearchEngineId, robustClient);
            builder.Plugins.AddFromObject(researchPlugin, "research");

            // Add FileSystemPlugin
            builder.Plugins.AddFromObject(new FileSystemPlugin(projectPath), "file_system");

            // Add ShellPlugin
            builder.Plugins.AddFromObject(new ShellPlugin(projectPath), "shell");

            return builder.Build();
        });

        // 6. Add Orchestrator
        services.AddTransient<IAgentOrchestrator, AgentOrchestrator>();

        return services;
    }
}