namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务规划服务。
/// 负责把自然语言或任务输入转换成结构化 TaskDefinition。
/// </summary>
public interface ITaskPlanningService
{
    /// <summary>
    /// 根据当前任务上下文生成结构化任务定义。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务规划结果。</returns>
    Task<TaskPlanningResult> PlanAsync(
        TaskExecutionContext context,
        CancellationToken cancellationToken = default);
}
