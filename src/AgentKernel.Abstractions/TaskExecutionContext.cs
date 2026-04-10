namespace AgentKernel.Abstractions;

/// <summary>
/// 表示一次任务执行过程中的运行时上下文。
/// 它负责承载任务定义、当前步骤、中间结果、输出结果、日志和错误状态。
/// </summary>
public class TaskExecutionContext
{
    /// <summary>
    /// 当前任务唯一标识。
    /// 默认与 TaskDefinition 的任务标识保持一致，或在运行时单独生成。
    /// </summary>
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 当前正在执行的任务定义。
    /// </summary>
    public TaskDefinition? TaskDefinition { get; set; }

    /// <summary>
    /// 当前执行步骤名称。
    /// 例如：scan_files、visual_verify、执行完成。
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// 任务执行过程中的中间结果仓库。
    /// 用于在各个 Capability 之间传递状态和产物。
    /// </summary>
    public Dictionary<string, object> WorkingMemory { get; set; } = [];

    /// <summary>
    /// 任务执行完成后的最终输出结果。
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = [];

    /// <summary>
    /// 执行日志集合。
    /// 用于记录任务运行过程中的关键事件。
    /// </summary>
    public List<string> ExecutionLogs { get; set; } = [];

    /// <summary>
    /// 当前任务是否执行完成。
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 当前任务是否已取消。
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// 当前任务是否执行失败。
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// 当前任务的错误信息。
    /// 如果执行失败，可在此记录原因。
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 向执行日志中追加一条记录。
    /// </summary>
    /// <param name="message">日志内容。</param>
    public void AddLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ExecutionLogs.Add(message);
    }
}
