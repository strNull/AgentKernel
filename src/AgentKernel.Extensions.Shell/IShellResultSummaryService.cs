namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 执行结果总结服务。
/// </summary>
public interface IShellResultSummaryService
{
    /// <summary>
    /// 根据执行结果生成面向用户的自然语言总结。
    /// </summary>
    Task<ShellResultSummaryResult> SummarizeAsync(
        string userInstruction,
        string command,
        string stdout,
        string stderr,
        bool success,
        CancellationToken cancellationToken = default);
}
