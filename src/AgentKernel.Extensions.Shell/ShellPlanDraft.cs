namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 领域的执行前规划草案。
/// </summary>
public class ShellPlanDraft
{
    /// <summary>
    /// 草案来源，例如 zhipu / fallback。
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 意图类型。
    /// </summary>
    public ShellIntentType IntentType { get; set; } = ShellIntentType.Unknown;

    /// <summary>
    /// 具体操作类型。
    /// </summary>
    public ShellOperationType OperationType { get; set; } = ShellOperationType.None;

    /// <summary>
    /// 风险等级。
    /// </summary>
    public ShellRiskLevel RiskLevel { get; set; } = ShellRiskLevel.Unknown;

    /// <summary>
    /// 目标位置。
    /// </summary>
    public ShellTargetLocation TargetLocation { get; set; } = ShellTargetLocation.Unknown;

    /// <summary>
    /// 目标类型。
    /// </summary>
    public ShellTargetKind TargetKind { get; set; } = ShellTargetKind.Unknown;

    /// <summary>
    /// 是否应该执行 Shell 命令。
    /// </summary>
    public bool ShouldExecute { get; set; }

    /// <summary>
    /// 执行前返回给用户的自然语言消息。
    /// </summary>
    public string AssistantMessage { get; set; } = string.Empty;

    /// <summary>
    /// PowerShell 命令；如系统可根据结构化字段重建，可留空。
    /// </summary>
    public string PowerShellCommand { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要用户确认。
    /// </summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// 目标名称，例如文件名、文件夹名、程序名。
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// 目标路径。
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// 附加参数。
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// 规划原因或说明。
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
