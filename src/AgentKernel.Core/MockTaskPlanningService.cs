using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示一个最小可运行的任务规划服务。
/// 当前版本不依赖大模型，只用于打通 Agent Kernel 主线。
/// </summary>
public class MockTaskPlanningService : ITaskPlanningService
{
    /// <summary>
    /// 根据当前任务上下文生成结构化任务定义。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务规划结果。</returns>
    public Task<TaskPlanningResult> PlanAsync(
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
            return Task.FromResult(new TaskPlanningResult
            {
                Success = false,
                Message = "用户任务为空，无法规划。",
                RequiresUserConfirmation = true,
                Warnings = ["请先输入任务要求。"]
            });
        }

        var taskDefinition = new TaskDefinition
        {
            TaskId = context.TaskId,
            Domain = "general",
            TaskType = "mock_pipeline",
            UserGoal = userGoal,
            Notes = "当前为第三阶段第一版 Mock Planner 生成的测试任务。",
            Actions =
            [
                new TaskActionDefinition
                {
                    Name = "mock_step",
                    Order = 1,
                    Description = "执行一个最小测试动作。"
                }
            ]
        };

        return Task.FromResult(new TaskPlanningResult
        {
            Success = true,
            TaskDefinition = taskDefinition,
            Message = "Mock Planner 已生成最小任务计划。",
            RequiresUserConfirmation = false
        });
    }
}
