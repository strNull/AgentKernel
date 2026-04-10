namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示一条等待用户确认的 Shell 执行草案。
/// </summary>
public class PendingShellConfirmation
{
    public string UserGoal { get; set; } = string.Empty;

    public string PowerShellCommand { get; set; } = string.Empty;

    public string AssistantMessage { get; set; } = string.Empty;

    public string PlanningSource { get; set; } = string.Empty;

    public ShellIntentType IntentType { get; set; } = ShellIntentType.Unknown;

    public ShellOperationType OperationType { get; set; } = ShellOperationType.None;

    public ShellRiskLevel RiskLevel { get; set; } = ShellRiskLevel.Unknown;

    public ShellTargetLocation TargetLocation { get; set; } = ShellTargetLocation.Unknown;

    public ShellTargetKind TargetKind { get; set; } = ShellTargetKind.Unknown;

    public string TargetName { get; set; } = string.Empty;

    public string TargetPath { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
