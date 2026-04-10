namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务执行引擎。
/// 负责根据 TaskDefinition.Actions 调度 Capability 执行。
/// </summary>
public interface ITaskExecutionEngine
{
    /// <summary>
    /// 执行当前任务上下文中的任务计划。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task ExecuteAsync(
        TaskExecutionContext context,
        CancellationToken cancellationToken = default);
}
