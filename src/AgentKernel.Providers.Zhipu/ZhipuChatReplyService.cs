using System.Net.Http.Json;
using System.Text.Json;
using AgentKernel.Extensions.Chat;

namespace AgentKernel.Providers.Zhipu;

/// <summary>
/// 基于智谱兼容接口的真实聊天回复服务。
/// </summary>
public class ZhipuChatReplyService : IChatReplyService
{
    private readonly HttpClient _httpClient;
    private readonly ZhipuOptions _options;
    private readonly IChatReplyService? _fallbackService;

    public ZhipuChatReplyService(
        HttpClient httpClient,
        ZhipuOptions options,
        IChatReplyService? fallbackService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fallbackService = fallbackService;
    }

    public async Task<ChatReplyResult> GenerateReplyAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        string text = userMessage?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return await FallbackAsync(text, cancellationToken, "我在呢，你可以直接和我说话呀。");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await FallbackAsync(text, cancellationToken);
        }

        try
        {
            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(_options.ChatModelName) ? "glm-4-flash" : _options.ChatModelName.Trim(),
                stream = false,
                temperature = 0.8,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "你叫小智，是一个像朋友一样交流的 AI 好友。你的回复要自然、温和、简洁，稍微可爱一点，但不要太夸张。当前阶段你可以聊天，也可以帮用户执行一些受控系统任务，但当下这次请求已经被判定为普通聊天，所以不要主动输出命令、脚本或工具调用说明。"
                    },
                    new
                    {
                        role = "user",
                        content = text
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
            string reply = ExtractReplyText(rawJson);

            if (string.IsNullOrWhiteSpace(reply))
            {
                return await FallbackAsync(text, cancellationToken);
            }

            return new ChatReplyResult
            {
                ReplyText = reply.Trim(),
                Source = "zhipu"
            };
        }
        catch
        {
            return await FallbackAsync(text, cancellationToken);
        }
    }

    private async Task<ChatReplyResult> FallbackAsync(
        string userMessage,
        CancellationToken cancellationToken,
        string defaultReply = "我收到你的消息啦。")
    {
        if (_fallbackService is not null)
        {
            return await _fallbackService.GenerateReplyAsync(userMessage, cancellationToken);
        }

        return new ChatReplyResult
        {
            ReplyText = defaultReply,
            Source = "fallback"
        };
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
