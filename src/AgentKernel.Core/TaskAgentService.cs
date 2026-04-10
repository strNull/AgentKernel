using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示统一的 Agent 门面服务。
/// 负责组织任务输入、任务规划和任务执行流程。
/// </summary>
public class TaskAgentService : ITaskAgentService
{
    private readonly ITaskPlanningService _planningService;
    private readonly ITaskExecutionEngine _executionEngine;

    /// <summary>
    /// 初始化 Agent 门面服务。
    /// </summary>
    /// <param name="planningService">任务规划服务。</param>
    /// <param name="executionEngine">任务执行引擎。</param>
    public TaskAgentService(
        ITaskPlanningService planningService,
        ITaskExecutionEngine executionEngine)
    {
        _planningService = planningService ?? throw new ArgumentNullException(nameof(planningService));
        _executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
    }

    /// <summary>
    /// 运行一个任务，并返回最终任务上下文。
    /// </summary>
    /// <param name="userInstruction">用户输入的自然语言任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行完成后的任务上下文。</returns>
    public async Task<TaskExecutionContext> RunAsync(
        string userInstruction,
        CancellationToken cancellationToken = default)
    {
        var context = new TaskExecutionContext
        {
            TaskDefinition = new TaskDefinition
            {
                UserGoal = userInstruction
            }
        };

        context.TaskId = context.TaskDefinition.TaskId;
        context.AddLog("任务已创建，准备开始规划。");

        TaskPlanningResult planningResult = await _planningService.PlanAsync(context, cancellationToken);

        if (!planningResult.Success || planningResult.TaskDefinition is null)
        {
            context.IsFailed = true;
            context.CurrentStep = "planning_failed";
            context.ErrorMessage = string.IsNullOrWhiteSpace(planningResult.Message)
                ? "任务规划失败。"
                : planningResult.Message;

            context.AddLog($"任务规划失败：{context.ErrorMessage}");

            foreach (string warning in planningResult.Warnings)
            {
                context.AddLog($"规划警告：{warning}");
            }

            return context;
        }

        context.TaskDefinition = planningResult.TaskDefinition;
        context.TaskId = planningResult.TaskDefinition.TaskId;

        if (!string.IsNullOrWhiteSpace(planningResult.Message))
        {
            context.AddLog($"任务规划完成：{planningResult.Message}");
        }

        foreach (string warning in planningResult.Warnings)
        {
            context.AddLog($"规划警告：{warning}");
        }

        if (planningResult.RequiresUserConfirmation)
        {
            context.CurrentStep = "awaiting_confirmation";
            context.AddLog("任务规划需要人工确认，执行已暂停。");
            return context;
        }

        await _executionEngine.ExecuteAsync(context, cancellationToken);
        return context;
    }
}
