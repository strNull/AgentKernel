using AgentKernel.Abstractions;
using AgentKernel.Core;

namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 扩展模块注册入口。
/// 负责向能力注册中心注册 Shell 相关能力。
/// </summary>
public static class ShellModule
{
    /// <summary>
    /// 向指定的能力注册中心注册 Shell 扩展能力。
    /// </summary>
    /// <param name="registry">能力注册中心。</param>
    /// <param name="shellResultSummaryService">Shell 结果总结服务。</param>
    /// <param name="pendingConfirmationStore">待确认草案存储服务。</param>
    public static void Register(
        CapabilityRegistry registry,
        IShellResultSummaryService shellResultSummaryService,
        IPendingShellConfirmationStore pendingConfirmationStore)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (shellResultSummaryService is null)
        {
            throw new ArgumentNullException(nameof(shellResultSummaryService));
        }

        if (pendingConfirmationStore is null)
        {
            throw new ArgumentNullException(nameof(pendingConfirmationStore));
        }

        registry.Register(
            new ExecutePowershellCapability(shellResultSummaryService, pendingConfirmationStore),
            new CapabilityDescriptor
            {
                Name = "execute_powershell",
                DisplayName = "执行 PowerShell",
                Description = "执行受控的 PowerShell 命令，并将执行结果写入运行时上下文。",
                Category = "shell",
                Domain = "shell",
                Consumes = [],
                Produces =
                [
                    ShellMemoryKeys.PowershellCommand,
                    ShellMemoryKeys.PowershellAssistantMessage,
                    ShellMemoryKeys.PowershellPlanningSource,
                    ShellMemoryKeys.PowershellIntentType,
                    ShellMemoryKeys.PowershellRiskLevel,
                    ShellMemoryKeys.PowershellStdout,
                    ShellMemoryKeys.PowershellStderr,
                    ShellMemoryKeys.PowershellExitCode,
                    ShellMemoryKeys.PowershellSuccess,
                    ShellMemoryKeys.PowershellSummary,
                    ShellMemoryKeys.PowershellSummarySource
                ],
                RequiresModel = false,
                SupportsReview = false
            });
    }
}
