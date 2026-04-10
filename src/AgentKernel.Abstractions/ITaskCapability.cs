namespace AgentKernel.Abstractions;

/// <summary>
/// 表示一个可被 ExecutionEngine 调度的能力模块。
/// 每个 Capability 负责完成一类单一动作。
/// </summary>
public interface ITaskCapability
{
    /// <summary>
    /// 当前能力名称。
    /// 应与 TaskActionDefinition.Name 对齐。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 判断当前能力是否能够处理指定动作。
    /// </summary>
    /// <param name="action">待执行动作定义。</param>
    /// <returns>如果可以处理，则返回 true。</returns>
    bool CanHandle(TaskActionDefinition action);

    /// <summary>
    /// 执行当前能力对应的动作。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="action">当前动作定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task ExecuteAsync(
        TaskExecutionContext context,
        TaskActionDefinition action,
        CancellationToken cancellationToken = default);
}
