namespace MyAgentTeam.Infrastructure.Configuration;

/// <summary>
/// 專門處理 429 Too Many Requests 的重試處理器。
/// 採用指數退避 (Exponential Backoff) 策略。
/// </summary>
public class RateLimitRetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 5; // 最多嘗試 5 次 (1次正常 + 4次重試)
    private readonly TimeSpan FixedDelay = TimeSpan.FromSeconds(5); // 固定等待 5 秒

    /// <summary>
    /// 初始化 <see cref="RateLimitRetryHandler"/> 類別的新實例。
    /// </summary>
    public RateLimitRetryHandler()
    {
    }

    /// <summary>
    /// 發送 HTTP 請求並處理 429 重試邏輯。
    /// </summary>
    /// <param name="request">HTTP 請求訊息。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>HTTP 回應訊息。</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int i = 1; i <= MaxAttempts; i++)
        {
            // 執行請求
            var response = await base.SendAsync(request, cancellationToken);

            // 如果不是 429，直接回傳結果
            if ((int)response.StatusCode != 429)
            {
                return response;
            }

            // 如果是最後一次嘗試，就不等待了，直接回傳(或讓上層處理失敗)
            if (i == MaxAttempts)
            {
                return response; 
                // 或者依照舊邏輯 throw exception，但通常回傳 429 Response 讓呼叫端知道失敗比較好，
                // 不過原本邏輯是 throw。為了保持行為一致，我們可以 throw。
                // 原本邏輯: throw new HttpRequestException...
            }

            // --- 遇到 429，準備重試 ---

            // 優先讀取 Retry-After，如果沒有則使用固定 5 秒
            var retryAfter = response.Headers.RetryAfter?.Delta;
            var delay = retryAfter ?? FixedDelay;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[System Warning]: Hit Rate Limit (429). Waiting {delay.TotalSeconds}s before retry ({i}/{MaxAttempts - 1})...");
            Console.ResetColor();

            // 等待
            await Task.Delay(delay, cancellationToken);

            // 銷毀舊的 Response
            response.Dispose();
        }

        // 理論上不會執行到這裡，因為最後一次嘗試在迴圈內處理了
        throw new HttpRequestException("Rate limit exceeded after multiple retries.", null, System.Net.HttpStatusCode.TooManyRequests);
    }
}