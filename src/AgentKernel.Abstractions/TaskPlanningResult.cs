namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务规划结果。
/// 用于描述 Planner 是否成功生成了可执行的 TaskDefinition，
/// 以及是否存在警告或需要人工确认的信息。
/// </summary>
public class TaskPlanningResult
{
    /// <summary>
    /// 当前任务规划是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 规划生成的任务定义。
    /// 如果规划失败，该值可以为空。
    /// </summary>
    public TaskDefinition? TaskDefinition { get; set; }

    /// <summary>
    /// 规划结果说明消息。
    /// 例如成功说明、失败原因或回退提示。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 当前规划结果是否建议人工确认后再继续执行。
    /// </summary>
    public bool RequiresUserConfirmation { get; set; }

    /// <summary>
    /// 规划阶段产生的警告信息集合。
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
