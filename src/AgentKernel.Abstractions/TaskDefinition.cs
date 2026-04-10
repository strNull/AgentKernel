namespace AgentKernel.Abstractions;

/// <summary>
/// 表示一条结构化任务定义。
/// 它是 Planner 输出给执行层的正式任务计划。
/// </summary>
public class TaskDefinition
{
    /// <summary>
    /// 任务唯一标识。
    /// </summary>
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 任务所属业务域。
    /// 例如：photo、ocr、shell、cad。
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 任务类型。
    /// 例如：filter_and_export_images、extract_text_from_images。
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// 用户原始目标描述。
    /// 用于保留任务的原始语义意图。
    /// </summary>
    public string UserGoal { get; set; } = string.Empty;

    /// <summary>
    /// 任务约束集合。
    /// 例如时间、地点、格式、目标对象等。
    /// </summary>
    public List<TaskConstraint> Constraints { get; set; } = [];

    /// <summary>
    /// 任务动作集合。
    /// ExecutionEngine 会按顺序调度这些动作。
    /// </summary>
    public List<TaskActionDefinition> Actions { get; set; } = [];

    /// <summary>
    /// 任务输出定义集合。
    /// 表示任务执行后需要交付的结果。
    /// </summary>
    public List<TaskOutputDefinition> Outputs { get; set; } = [];

    /// <summary>
    /// 任务补充说明。
    /// 可用于 Planner 备注、策略说明或人工提示。
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
