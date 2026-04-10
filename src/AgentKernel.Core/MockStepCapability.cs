using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示一个最小测试能力。
/// 用于打通第三阶段第一版 Agent Kernel 的执行主线。
/// </summary>
public class MockStepCapability : ITaskCapability
{
    /// <summary>
    /// 当前能力名称。
    /// </summary>
    public string Name => "mock_step";

    /// <summary>
    /// 判断当前能力是否能够处理指定动作。
    /// </summary>
    /// <param name="action">待执行动作定义。</param>
    /// <returns>如果可以处理，则返回 true。</returns>
    public bool CanHandle(TaskActionDefinition action)
    {
        return action is not null &&
               string.Equals(action.Name, Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 执行最小测试动作。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="action">当前动作定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public Task ExecuteAsync(
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

        context.WorkingMemory["mock_result"] = "mock_step executed";
        context.Outputs["mock_output"] = "mock pipeline completed";
        context.AddLog("MockStepCapability 已执行。");

        return Task.CompletedTask;
    }
}
