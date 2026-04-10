using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示任务执行引擎。
/// 负责按顺序调度 TaskDefinition 中的动作，并将结果写回运行时上下文。
/// </summary>
public class TaskExecutionEngine : ITaskExecutionEngine
{
    private readonly CapabilityRegistry _capabilityRegistry;

    /// <summary>
    /// 初始化任务执行引擎。
    /// </summary>
    /// <param name="capabilityRegistry">能力注册中心。</param>
    public TaskExecutionEngine(CapabilityRegistry capabilityRegistry)
    {
        _capabilityRegistry = capabilityRegistry ?? throw new ArgumentNullException(nameof(capabilityRegistry));
    }

    /// <summary>
    /// 执行当前任务上下文中的任务计划。
    /// </summary>
    /// <param name="context">任务执行上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task ExecuteAsync(
        TaskExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.TaskDefinition is null)
        {
            throw new InvalidOperationException("TaskExecutionContext.TaskDefinition 不能为空。");
        }

        List<TaskActionDefinition> actions = context.TaskDefinition.Actions
            .OrderBy(action => action.Order)
            .ToList();

        if (actions.Count == 0)
        {
            throw new InvalidOperationException("当前任务没有可执行动作。");
        }

        context.IsCompleted = false;
        context.IsCancelled = false;
        context.IsFailed = false;
        context.ErrorMessage = string.Empty;
        context.AddLog($"任务开始执行。TaskId={context.TaskId}");

        try
        {
            foreach (TaskActionDefinition action in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                context.CurrentStep = action.Name;
                context.AddLog($"开始执行动作：{action.Name}");

                ITaskCapability? capability = _capabilityRegistry.Find(action);
                if (capability is null)
                {
                    throw new InvalidOperationException($"未找到可处理动作 '{action.Name}' 的 Capability。");
                }

                await capability.ExecuteAsync(context, action, cancellationToken);
                context.AddLog($"动作执行完成：{action.Name}");
            }

            context.CurrentStep = "completed";
            context.IsCompleted = true;
            context.AddLog("任务执行完成。");
        }
        catch (OperationCanceledException)
        {
            context.CurrentStep = "cancelled";
            context.IsCancelled = true;
            context.AddLog("任务执行已取消。");
            throw;
        }
        catch (Exception ex)
        {
            context.CurrentStep = "failed";
            context.IsFailed = true;
            context.ErrorMessage = ex.Message;
            context.AddLog($"任务执行失败：{ex.Message}");
            throw;
        }
    }
}
