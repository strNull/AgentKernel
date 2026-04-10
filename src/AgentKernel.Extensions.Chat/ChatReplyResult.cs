namespace AgentKernel.Extensions.Chat;

/// <summary>
/// 表示聊天回复结果。
/// </summary>
public class ChatReplyResult
{
    /// <summary>
    /// 回复文本。
    /// </summary>
    public string ReplyText { get; set; } = string.Empty;

    /// <summary>
    /// 回复来源。
    /// 例如：zhipu、fallback。
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
