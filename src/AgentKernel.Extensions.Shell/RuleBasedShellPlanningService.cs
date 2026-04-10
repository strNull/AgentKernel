namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 基于规则的 Shell 规划服务。
/// 当前仅保留少量稳定样例，作为超级终端主链的最小兜底。
/// </summary>
public class RuleBasedShellPlanningService : IShellPlanningService
{
    public Task<ShellPlanDraft> PlanAsync(
        string userInstruction,
        CancellationToken cancellationToken = default)
    {
        string text = userInstruction?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new ShellPlanDraft
            {
                Source = "fallback",
                IntentType = ShellIntentType.Unknown,
                OperationType = ShellOperationType.None,
                RiskLevel = ShellRiskLevel.Unknown,
                TargetLocation = ShellTargetLocation.Unknown,
                TargetKind = ShellTargetKind.Unknown,
                ShouldExecute = false,
                AssistantMessage = "你还没有告诉我具体要做什么哦。",
                Reason = "用户输入为空。"
            });
        }

        if (ContainsAny(text, "powershell版本", "power shell版本", "ps版本"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好呀，我帮你看看当前环境的 PowerShell 版本。",
                intentType: ShellIntentType.QuerySystemInfo,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.Unknown,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为 PowerShell 版本查询任务。",
                command: "$PSVersionTable.PSVersion"));
        }

        if (ContainsAny(text, "当前目录文件", "列出当前目录"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "收到，我这就帮你把当前目录里的内容列出来。",
                intentType: ShellIntentType.QueryFiles,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.CurrentDirectory,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为当前目录查询任务。"));
        }

        if (ContainsAny(text, "桌面文件", "查看桌面文件", "桌面文件列表"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好的，我现在去查看你的桌面文件。",
                intentType: ShellIntentType.QueryFiles,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.Desktop,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为桌面文件查询任务。"));
        }

        if (ContainsAny(text, "文档文件", "查看文档文件", "文档列表"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好的，我帮你查看文档文件夹里的内容。",
                intentType: ShellIntentType.QueryFiles,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.Documents,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为文档文件查询任务。"));
        }

        if (ContainsAny(text, "下载文件", "查看下载文件", "下载列表"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "收到，我帮你看看下载文件夹里有什么。",
                intentType: ShellIntentType.QueryFiles,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.Downloads,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为下载文件查询任务。"));
        }

        if (ContainsAny(text, "图片文件", "查看图片文件", "图片列表"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好呀，我来帮你看看图片文件夹里的内容。",
                intentType: ShellIntentType.QueryFiles,
                operationType: ShellOperationType.Query,
                riskLevel: ShellRiskLevel.ReadOnly,
                targetLocation: ShellTargetLocation.Pictures,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                reason: "识别为图片文件查询任务。"));
        }

        if (ContainsAny(text, "打开计算器", "启动计算器", "打开 calc", "calc"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好呀，我这就帮你把计算器打开。",
                intentType: ShellIntentType.LaunchApplication,
                operationType: ShellOperationType.Launch,
                riskLevel: ShellRiskLevel.Low,
                targetLocation: ShellTargetLocation.Unknown,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                targetName: "calc",
                reason: "识别为低风险应用启动任务。"));
        }

        if (ContainsAny(text, "打开记事本", "启动记事本", "打开 notepad", "notepad"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "收到，我这就帮你把记事本打开。",
                intentType: ShellIntentType.LaunchApplication,
                operationType: ShellOperationType.Launch,
                riskLevel: ShellRiskLevel.Low,
                targetLocation: ShellTargetLocation.Unknown,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                targetName: "notepad",
                reason: "识别为低风险应用启动任务。"));
        }

        if (ContainsAny(text, "打开资源管理器", "启动资源管理器", "打开 explorer", "explorer"))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "好呀，我来帮你把资源管理器打开。",
                intentType: ShellIntentType.LaunchApplication,
                operationType: ShellOperationType.Launch,
                riskLevel: ShellRiskLevel.Low,
                targetLocation: ShellTargetLocation.Unknown,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: true,
                targetName: "explorer",
                reason: "识别为低风险应用启动任务。"));
        }

        if (KnownLocationFileOperationPlanner.TryPlanDelete(text, "fallback", out ShellPlanDraft deletePlan))
        {
            return Task.FromResult(deletePlan);
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(text, "fallback", ShellOperationType.Copy, out ShellPlanDraft copyPlan))
        {
            return Task.FromResult(copyPlan);
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(text, "fallback", ShellOperationType.Move, out ShellPlanDraft movePlan))
        {
            return Task.FromResult(movePlan);
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(text, "fallback", ShellOperationType.Rename, out ShellPlanDraft renamePlan))
        {
            return Task.FromResult(renamePlan);
        }

        if (CustomPathFileOperationPlanner.TryPlanRename(text, "fallback", out ShellPlanDraft customRenamePlan))
        {
            return Task.FromResult(customRenamePlan);
        }

        if (KnownLocationTargetParser.LooksLikeDeleteRequest(text) ||
            KnownLocationTargetParser.LooksLikeCopyRequest(text) ||
            KnownLocationTargetParser.LooksLikeMoveRequest(text) ||
            KnownLocationTargetParser.LooksLikeRenameRequest(text))
        {
            return Task.FromResult(BuildPlan(
                assistantMessage: "这类文件操作有一定风险，我需要先确认目标位置和名称后再继续。",
                intentType: ShellIntentType.FileOperation,
                operationType: ShellOperationType.Delete,
                riskLevel: ShellRiskLevel.High,
                targetLocation: ShellTargetLocation.Unknown,
                targetKind: ShellTargetKind.Unknown,
                shouldExecute: false,
                requiresConfirmation: false,
                reason: "识别为高风险删除请求，但当前无法稳定解析目标。"));
        }

        return Task.FromResult(BuildPlan(
            assistantMessage: "这条消息暂时不像我当前稳定支持的终端任务，我先陪你正常聊天吧。",
            intentType: ShellIntentType.Unknown,
            operationType: ShellOperationType.None,
            riskLevel: ShellRiskLevel.Unknown,
            targetLocation: ShellTargetLocation.Unknown,
            targetKind: ShellTargetKind.Unknown,
            shouldExecute: false,
            reason: "规则版 Shell Planner 未识别出稳定支持的终端意图。"));
    }

    private static ShellPlanDraft BuildPlan(
        string assistantMessage,
        ShellIntentType intentType,
        ShellOperationType operationType,
        ShellRiskLevel riskLevel,
        ShellTargetLocation targetLocation,
        ShellTargetKind targetKind,
        bool shouldExecute,
        string reason,
        bool requiresConfirmation = false,
        string targetName = "",
        string targetPath = "",
        string arguments = "",
        string command = "")
    {
        var plan = new ShellPlanDraft
        {
            Source = "fallback",
            AssistantMessage = assistantMessage,
            IntentType = intentType,
            OperationType = operationType,
            RiskLevel = riskLevel,
            TargetLocation = targetLocation,
            TargetKind = targetKind,
            ShouldExecute = shouldExecute,
            RequiresConfirmation = requiresConfirmation,
            TargetName = targetName,
            TargetPath = targetPath,
            Arguments = arguments,
            Reason = reason,
            PowerShellCommand = command
        };

        if (string.IsNullOrWhiteSpace(plan.PowerShellCommand) && plan.ShouldExecute)
        {
            plan.PowerShellCommand = ShellCommandBuilder.BuildCommand(plan);
        }

        return plan;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
