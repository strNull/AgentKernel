namespace AgentKernel.Extensions.Chat;

/// <summary>
/// 表示聊天回复生成服务。
/// </summary>
public interface IChatReplyService
{
    /// <summary>
    /// 根据用户输入生成聊天回复结果。
    /// </summary>
    /// <param name="userMessage">用户输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>聊天回复结果。</returns>
    Task<ChatReplyResult> GenerateReplyAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}
