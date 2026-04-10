namespace AgentKernel.Abstractions;

/// <summary>
/// 表示统一的 Agent 门面服务。
/// 负责组织任务输入、任务规划和任务执行流程。
/// </summary>
public interface ITaskAgentService
{
    /// <summary>
    /// 运行一个任务，并返回最终任务上下文。
    /// </summary>
    /// <param name="userInstruction">用户输入的自然语言任务。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行完成后的任务上下文。</returns>
    Task<TaskExecutionContext> RunAsync(
        string userInstruction,
        CancellationToken cancellationToken = default);
}