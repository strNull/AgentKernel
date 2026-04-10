using System.Net.Http.Json;
using System.Text.Json;
using AgentKernel.Extensions.Shell;

namespace AgentKernel.Providers.Zhipu;

/// <summary>
/// 基于智谱兼容接口的 Shell 结果总结服务。
/// </summary>
public class ZhipuShellResultSummaryService : IShellResultSummaryService
{
    private readonly HttpClient _httpClient;
    private readonly ZhipuOptions _options;
    private readonly IShellResultSummaryService _fallbackService;

    public ZhipuShellResultSummaryService(
        HttpClient httpClient,
        ZhipuOptions options,
        IShellResultSummaryService? fallbackService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fallbackService = fallbackService ?? new RuleBasedShellResultSummaryService();
    }

    public async Task<ShellResultSummaryResult> SummarizeAsync(
        string userInstruction,
        string command,
        string stdout,
        string stderr,
        bool success,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await _fallbackService.SummarizeAsync(userInstruction, command, stdout, stderr, success, cancellationToken);
        }

        try
        {
            string prompt = BuildPrompt(userInstruction, command, stdout, stderr, success);

            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(_options.ChatModelName) ? "glm-4-flash" : _options.ChatModelName.Trim(),
                stream = false,
                temperature = 0.3,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ChatCompletionsUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            string rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            string summary = ExtractReplyText(rawJson).Trim();

            if (string.IsNullOrWhiteSpace(summary))
            {
                return await _fallbackService.SummarizeAsync(userInstruction, command, stdout, stderr, success, cancellationToken);
            }

            return new ShellResultSummaryResult
            {
                SummaryText = summary,
                Source = "zhipu"
            };
        }
        catch
        {
            return await _fallbackService.SummarizeAsync(userInstruction, command, stdout, stderr, success, cancellationToken);
        }
    }

    private static string BuildPrompt(
        string userInstruction,
        string command,
        string stdout,
        string stderr,
        bool success)
    {
        return
            "你叫小智，是一个像朋友一样交流的 AI 好友。\n" +
            "你的任务是把一次 Shell 执行结果总结成一小段自然语言回复。\n" +
            "要求：语气自然、稍微可爱一点、简洁，不要输出 Markdown，不要复述整段原始输出。\n\n" +
            $"用户原始请求：{userInstruction}\n" +
            $"执行命令：{command}\n" +
            $"是否成功：{success}\n" +
            $"标准输出：{stdout}\n" +
            $"标准错误：{stderr}\n";
    }

    private static string ExtractReplyText(string rawJson)
    {
        using JsonDocument doc = JsonDocument.Parse(rawJson);

        JsonElement contentElement = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content");

        return contentElement.ValueKind switch
        {
            JsonValueKind.String => contentElement.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Concat(contentElement.EnumerateArray()
                .Select(item => item.TryGetProperty("text", out JsonElement textProp)
                    ? textProp.GetString()
                    : string.Empty)),
            _ => string.Empty
        };
    }
}
