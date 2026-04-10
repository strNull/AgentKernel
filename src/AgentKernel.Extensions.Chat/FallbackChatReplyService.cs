namespace AgentKernel.Extensions.Chat;

/// <summary>
/// 本地回退聊天回复服务。
/// 当真实模型不可用时，使用简单规则生成回复。
/// </summary>
public class FallbackChatReplyService : IChatReplyService
{
    public Task<ChatReplyResult> GenerateReplyAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        string text = userMessage?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new ChatReplyResult
            {
                ReplyText = "我在呢，你可以直接和我说话。",
                Source = "fallback"
            });
        }

        if (text.Contains("你好", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("嗨", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("hello", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ChatReplyResult
            {
                ReplyText = "你好呀，我在呢。你可以直接和我聊天，也可以让我帮你执行一些受控命令。",
                Source = "fallback"
            });
        }

        if (text.Contains("你是谁", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("你叫什么", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ChatReplyResult
            {
                ReplyText = "我是小智，是你的 AI 好友。我可以陪你聊天，也可以帮你执行一些受控任务。",
                Source = "fallback"
            });
        }

        return Task.FromResult(new ChatReplyResult
        {
            ReplyText = $"我收到了你说的“{text}”。如果你愿意，也可以继续告诉我你想做什么。",
            Source = "fallback"
        });
    }
}
