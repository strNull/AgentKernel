namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 定义 Shell 扩展中使用的 Runtime 键名。
/// 用于统一管理 WorkingMemory 和 Outputs 中的字段名称。
/// </summary>
public static class ShellMemoryKeys
{
    public const string PowershellCommand = "powershell_command";
    public const string PowershellStdout = "powershell_stdout";
    public const string PowershellStderr = "powershell_stderr";
    public const string PowershellExitCode = "powershell_exit_code";
    public const string PowershellSuccess = "powershell_success";
    public const string PowershellSummary = "powershell_summary";
    public const string PowershellAssistantMessage = "powershell_assistant_message";
    public const string PowershellPlanningSource = "powershell_planning_source";
    public const string PowershellSummarySource = "powershell_summary_source";
    public const string PowershellIntentType = "powershell_intent_type";
    public const string PowershellRiskLevel = "powershell_risk_level";
}
