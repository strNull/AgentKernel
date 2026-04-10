namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 执行结果总结。
/// </summary>
public class ShellResultSummaryResult
{
    /// <summary>
    /// 面向用户的自然语言总结文本。
    /// </summary>
    public string SummaryText { get; set; } = string.Empty;

    /// <summary>
    /// 当前总结的生成来源。
    /// 例如 zhipu / fallback。
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
