using System.Diagnostics;
using AgentKernel.Abstractions;

namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示执行 PowerShell 命令的能力。
/// </summary>
public class ExecutePowershellCapability : ITaskCapability
{
    private readonly IShellResultSummaryService _shellResultSummaryService;
    private readonly IPendingShellConfirmationStore _pendingConfirmationStore;

    /// <summary>
    /// 初始化 PowerShell 执行能力。
    /// </summary>
    /// <param name="shellResultSummaryService">Shell 结果总结服务。</param>
    /// <param name="pendingConfirmationStore">待确认草案存储服务。</param>
    public ExecutePowershellCapability(
        IShellResultSummaryService shellResultSummaryService,
        IPendingShellConfirmationStore pendingConfirmationStore)
    {
        _shellResultSummaryService = shellResultSummaryService ?? throw new ArgumentNullException(nameof(shellResultSummaryService));
        _pendingConfirmationStore = pendingConfirmationStore ?? throw new ArgumentNullException(nameof(pendingConfirmationStore));
    }

    /// <summary>
    /// 当前能力名称。
    /// </summary>
    public string Name => "execute_powershell";

    /// <summary>
    /// 判断当前能力是否能够处理指定动作。
    /// </summary>
    public bool CanHandle(TaskActionDefinition action)
    {
        return action is not null &&
               string.Equals(action.Name, Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行 PowerShell 命令，并将结果写入运行时上下文。
    /// </summary>
    public async Task ExecuteAsync(
        TaskExecutionContext context,
        TaskActionDefinition action,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!action.Parameters.TryGetValue("command", out string? command) ||
            string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException("execute_powershell 动作缺少 command 参数。");
        }

        action.Parameters.TryGetValue("assistant_message", out string? assistantMessage);
        action.Parameters.TryGetValue("planning_source", out string? planningSource);
        action.Parameters.TryGetValue("intent_type", out string? intentType);
        action.Parameters.TryGetValue("risk_level", out string? riskLevel);
        action.Parameters.TryGetValue("confirmed_execution", out string? confirmedExecutionText);

        bool confirmedExecution = string.Equals(
            confirmedExecutionText,
            "true",
            StringComparison.OrdinalIgnoreCase);

        context.AddLog($"准备执行 PowerShell 命令：{command}");
        context.WorkingMemory[ShellMemoryKeys.PowershellCommand] = command;

        if (!string.IsNullOrWhiteSpace(assistantMessage))
        {
            context.WorkingMemory[ShellMemoryKeys.PowershellAssistantMessage] = assistantMessage;
            context.Outputs[ShellMemoryKeys.PowershellAssistantMessage] = assistantMessage;
        }

        if (!string.IsNullOrWhiteSpace(planningSource))
        {
            context.WorkingMemory[ShellMemoryKeys.PowershellPlanningSource] = planningSource;
            context.Outputs[ShellMemoryKeys.PowershellPlanningSource] = planningSource;
            context.AddLog($"Shell 规划来源：{planningSource}");
        }

        if (!string.IsNullOrWhiteSpace(intentType))
        {
            context.WorkingMemory[ShellMemoryKeys.PowershellIntentType] = intentType;
            context.Outputs[ShellMemoryKeys.PowershellIntentType] = intentType;
            context.AddLog($"Shell 意图类型：{intentType}");
        }

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            context.WorkingMemory[ShellMemoryKeys.PowershellRiskLevel] = riskLevel;
            context.Outputs[ShellMemoryKeys.PowershellRiskLevel] = riskLevel;
            context.AddLog($"Shell 风险等级：{riskLevel}");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        int exitCode = process.ExitCode;
        bool success = exitCode == 0;

        context.WorkingMemory[ShellMemoryKeys.PowershellStdout] = stdout;
        context.WorkingMemory[ShellMemoryKeys.PowershellStderr] = stderr;
        context.WorkingMemory[ShellMemoryKeys.PowershellExitCode] = exitCode;
        context.WorkingMemory[ShellMemoryKeys.PowershellSuccess] = success;

        string userInstruction = context.TaskDefinition?.UserGoal?.Trim() ?? string.Empty;
        ShellResultSummaryResult summaryResult = await _shellResultSummaryService.SummarizeAsync(
            userInstruction,
            command,
            stdout,
            stderr,
            success,
            cancellationToken);

        context.Outputs[ShellMemoryKeys.PowershellSummary] = summaryResult.SummaryText;
        context.Outputs[ShellMemoryKeys.PowershellSummarySource] = summaryResult.Source;

        if (!string.IsNullOrWhiteSpace(summaryResult.Source))
        {
            context.AddLog($"Shell 总结来源：{summaryResult.Source}");
        }

        if (confirmedExecution)
        {
            _pendingConfirmationStore.Clear();
            context.AddLog("待确认的 Shell 草案已在执行后清理。");
        }

        context.AddLog($"PowerShell 执行完成。ExitCode={exitCode}");
    }
}
