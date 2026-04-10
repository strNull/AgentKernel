using AgentKernel.Abstractions;

namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 超级终端的总规划器。
/// 负责识别确认/取消语义，并基于 Shell 草案决定直接执行、等待确认或转为普通聊天。
/// </summary>
public class ShellPlanningService : ITaskPlanningService
{
    private readonly IShellPlanningService _shellPlanningService;
    private readonly IPendingShellConfirmationStore _pendingConfirmationStore;

    public ShellPlanningService(
        IShellPlanningService shellPlanningService,
        IPendingShellConfirmationStore pendingConfirmationStore)
    {
        _shellPlanningService = shellPlanningService ?? throw new ArgumentNullException(nameof(shellPlanningService));
        _pendingConfirmationStore = pendingConfirmationStore ?? throw new ArgumentNullException(nameof(pendingConfirmationStore));
    }

    public async Task<TaskPlanningResult> PlanAsync(
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        string userGoal = context.TaskDefinition?.UserGoal?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userGoal))
        {
            return new TaskPlanningResult
            {
                Success = false,
                Message = "用户任务为空，无法规划。",
                RequiresUserConfirmation = true,
                Warnings = ["请先输入任务要求。"]
            };
        }

        if (IsConfirmationInput(userGoal))
        {
            return BuildConfirmationExecutionTask(context, userGoal);
        }

        if (IsCancellationInput(userGoal))
        {
            return BuildCancellationTask(context, userGoal);
        }

        ShellPlanDraft shellPlan = await _shellPlanningService.PlanAsync(userGoal, cancellationToken);

        if (string.IsNullOrWhiteSpace(shellPlan.PowerShellCommand) && shellPlan.ShouldExecute)
        {
            shellPlan.PowerShellCommand = ShellCommandBuilder.BuildCommand(shellPlan);
        }

        if (shellPlan.ShouldExecute && !string.IsNullOrWhiteSpace(shellPlan.PowerShellCommand))
        {
            if (shellPlan.RequiresConfirmation || shellPlan.RiskLevel == ShellRiskLevel.High)
            {
                return BuildConfirmationTask(context, userGoal, shellPlan);
            }

            return BuildShellTask(context, userGoal, shellPlan);
        }

        return BuildChatTask(context, userGoal, shellPlan);
    }

    private TaskPlanningResult BuildConfirmationTask(
        TaskExecutionContext context,
        string userGoal,
        ShellPlanDraft shellPlan)
    {
        _pendingConfirmationStore.Save(new PendingShellConfirmation
        {
            UserGoal = userGoal,
            PowerShellCommand = shellPlan.PowerShellCommand,
            AssistantMessage = shellPlan.AssistantMessage,
            PlanningSource = shellPlan.Source,
            IntentType = shellPlan.IntentType,
            OperationType = shellPlan.OperationType,
            RiskLevel = shellPlan.RiskLevel,
            TargetLocation = shellPlan.TargetLocation,
            TargetKind = shellPlan.TargetKind,
            TargetName = shellPlan.TargetName,
            TargetPath = shellPlan.TargetPath,
            Arguments = shellPlan.Arguments,
            Reason = shellPlan.Reason
        });

        string confirmationMessage =
            $"{shellPlan.AssistantMessage}\n\n" +
            "这条操作需要你先确认。\n" +
            $"意图类型：{shellPlan.IntentType}\n" +
            $"操作类型：{shellPlan.OperationType}\n" +
            $"风险等级：{shellPlan.RiskLevel}\n" +
            $"{BuildDirectoryScopeNote(shellPlan)}" +
            $"拟执行命令：{shellPlan.PowerShellCommand}\n\n" +
            "你可以点击下方按钮继续执行，或直接取消。";

        return BuildDirectReplyTask(
            context,
            userGoal,
            taskType: "confirmation_required",
            notes: shellPlan.Reason,
            reply: confirmationMessage,
            message: "该任务需要确认，当前已转为确认提示回复。",
            requiresUserConfirmation: true);
    }

    private TaskPlanningResult BuildConfirmationExecutionTask(
        TaskExecutionContext context,
        string userGoal)
    {
        if (!_pendingConfirmationStore.TryGet(out PendingShellConfirmation? confirmation) || confirmation is null)
        {
            return BuildDirectReplyTask(
                context,
                userGoal,
                taskType: "chat_reply",
                notes: "当前没有待确认终端操作。",
                reply: "当前没有等待确认的终端操作哦。你可以直接告诉我你想执行什么。",
                message: "已生成普通聊天回复任务。",
                requiresUserConfirmation: false);
        }

        var shellPlan = new ShellPlanDraft
        {
            Source = confirmation.PlanningSource,
            IntentType = confirmation.IntentType,
            OperationType = confirmation.OperationType,
            RiskLevel = confirmation.RiskLevel,
            TargetLocation = confirmation.TargetLocation,
            TargetKind = confirmation.TargetKind,
            TargetName = confirmation.TargetName,
            TargetPath = confirmation.TargetPath,
            Arguments = confirmation.Arguments,
            ShouldExecute = true,
            AssistantMessage = string.IsNullOrWhiteSpace(confirmation.AssistantMessage)
                ? "收到确认，我现在继续执行。"
                : $"{confirmation.AssistantMessage}\n\n收到确认，我现在继续执行。",
            PowerShellCommand = BuildPreferredCommand(confirmation),
            RequiresConfirmation = false,
            Reason = confirmation.Reason
        };

        return BuildShellTask(context, confirmation.UserGoal, shellPlan, confirmedExecution: true);
    }

    private TaskPlanningResult BuildCancellationTask(
        TaskExecutionContext context,
        string userGoal)
    {
        if (_pendingConfirmationStore.TryGet(out PendingShellConfirmation? confirmation) && confirmation is not null)
        {
            _pendingConfirmationStore.Clear();

            return BuildDirectReplyTask(
                context,
                userGoal,
                taskType: "chat_reply",
                notes: "待确认终端操作已取消。",
                reply: "好哦，我已经把刚才那条待确认操作取消掉了。",
                message: "已生成普通聊天回复任务。",
                requiresUserConfirmation: false);
        }

        return BuildDirectReplyTask(
            context,
            userGoal,
            taskType: "chat_reply",
            notes: "当前没有待取消终端操作。",
            reply: "当前没有待取消的终端操作。",
            message: "已生成普通聊天回复任务。",
            requiresUserConfirmation: false);
    }

    private static TaskPlanningResult BuildShellTask(
        TaskExecutionContext context,
        string userGoal,
        ShellPlanDraft shellPlan,
        bool confirmedExecution = false)
    {
        var taskDefinition = new TaskDefinition
        {
            TaskId = context.TaskId,
            Domain = "shell",
            TaskType = "execute_powershell_command",
            UserGoal = userGoal,
            Notes = shellPlan.Reason,
            Actions =
            [
                new TaskActionDefinition
                {
                    Name = "execute_powershell",
                    Order = 1,
                    Description = "执行规划好的 PowerShell 命令。",
                    Parameters = new Dictionary<string, string>
                    {
                        ["command"] = shellPlan.PowerShellCommand,
                        ["assistant_message"] = shellPlan.AssistantMessage,
                        ["planning_source"] = shellPlan.Source,
                        ["intent_type"] = shellPlan.IntentType.ToString(),
                        ["risk_level"] = shellPlan.RiskLevel.ToString(),
                        ["confirmed_execution"] = confirmedExecution ? "true" : "false"
                    }
                }
            ]
        };

        return new TaskPlanningResult
        {
            Success = true,
            TaskDefinition = taskDefinition,
            Message = "已生成 Shell 执行任务。",
            RequiresUserConfirmation = shellPlan.RequiresConfirmation
        };
    }

    private static string BuildPreferredCommand(PendingShellConfirmation confirmation)
    {
        var structuredPlan = new ShellPlanDraft
        {
            Source = confirmation.PlanningSource,
            IntentType = confirmation.IntentType,
            OperationType = confirmation.OperationType,
            RiskLevel = confirmation.RiskLevel,
            TargetLocation = confirmation.TargetLocation,
            TargetKind = confirmation.TargetKind,
            TargetName = confirmation.TargetName,
            TargetPath = confirmation.TargetPath,
            Arguments = confirmation.Arguments,
            ShouldExecute = true
        };

        if (ShouldPreferStructuredCommand(confirmation))
        {
            string rebuiltCommand = ShellCommandBuilder.BuildCommand(structuredPlan);
            if (!string.IsNullOrWhiteSpace(rebuiltCommand))
            {
                return rebuiltCommand;
            }
        }

        if (!string.IsNullOrWhiteSpace(confirmation.PowerShellCommand))
        {
            return confirmation.PowerShellCommand;
        }

        return ShellCommandBuilder.BuildCommand(structuredPlan);
    }

    private static bool ShouldPreferStructuredCommand(PendingShellConfirmation confirmation)
    {
        if (confirmation.OperationType == ShellOperationType.Query &&
            confirmation.TargetLocation != ShellTargetLocation.Unknown)
        {
            return true;
        }

        if (confirmation.OperationType == ShellOperationType.Delete &&
            (confirmation.TargetLocation != ShellTargetLocation.Unknown ||
             !string.IsNullOrWhiteSpace(confirmation.TargetPath) ||
             !string.IsNullOrWhiteSpace(confirmation.TargetName)))
        {
            return true;
        }

        if (confirmation.OperationType is ShellOperationType.Copy or ShellOperationType.Move or ShellOperationType.Rename &&
            (confirmation.TargetLocation != ShellTargetLocation.Unknown ||
             !string.IsNullOrWhiteSpace(confirmation.TargetPath) ||
             !string.IsNullOrWhiteSpace(confirmation.TargetName) ||
             !string.IsNullOrWhiteSpace(confirmation.Arguments)))
        {
            return true;
        }

        return false;
    }

    private static TaskPlanningResult BuildChatTask(
        TaskExecutionContext context,
        string userGoal,
        ShellPlanDraft shellPlan)
    {
        string reply = string.IsNullOrWhiteSpace(shellPlan.AssistantMessage)
            ? $"我收到了你说的“{userGoal}”。如果你愿意，也可以继续告诉我你想做什么。"
            : shellPlan.AssistantMessage;

        return BuildDirectReplyTask(
            context,
            userGoal,
            taskType: "chat_reply",
            notes: shellPlan.Reason,
            reply: reply,
            message: "已生成普通聊天回复任务。",
            requiresUserConfirmation: false);
    }

    private static TaskPlanningResult BuildDirectReplyTask(
        TaskExecutionContext context,
        string userGoal,
        string taskType,
        string notes,
        string reply,
        string message,
        bool requiresUserConfirmation)
    {
        var taskDefinition = new TaskDefinition
        {
            TaskId = context.TaskId,
            Domain = "chat",
            TaskType = taskType,
            UserGoal = userGoal,
            Notes = notes,
            Actions =
            [
                new TaskActionDefinition
                {
                    Name = "chat_reply",
                    Order = 1,
                    Description = "生成一条聊天回复。",
                    Parameters = new Dictionary<string, string>
                    {
                        ["reply"] = reply
                    }
                }
            ]
        };

        return new TaskPlanningResult
        {
            Success = true,
            TaskDefinition = taskDefinition,
            Message = message,
            RequiresUserConfirmation = requiresUserConfirmation
        };
    }

    private static bool IsConfirmationInput(string text)
    {
        return MatchesAny(text, "确认", "确认执行", "继续执行", "继续", "是");
    }

    private static bool IsCancellationInput(string text)
    {
        return MatchesAny(text, "取消", "不用了", "先别执行", "停止执行", "算了");
    }

    private static bool MatchesAny(string text, params string[] candidates)
    {
        string normalized = NormalizeConfirmationText(text);
        foreach (string candidate in candidates)
        {
            if (string.Equals(normalized, NormalizeConfirmationText(candidate), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeConfirmationText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text.Trim();
        char[] trailingChars =
        [
            '。',
            '.',
            '！',
            '!',
            '？',
            '?',
            '，',
            ',',
            '；',
            ';',
            '：',
            ':',
            '"',
            '\'',
            '”',
            '“',
            '’',
            '‘'
        ];

        return normalized.TrimEnd(trailingChars).Trim();
    }

    private static string BuildDirectoryScopeNote(ShellPlanDraft shellPlan)
    {
        if (shellPlan.TargetKind != ShellTargetKind.Directory)
        {
            return string.Empty;
        }

        return "注意：当前目标是文件夹，确认后会连同文件夹内部内容一起处理。\n";
    }
}
