namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务中的一个可执行动作。
/// ExecutionEngine 会按顺序调度这些动作。
/// </summary>
public class TaskActionDefinition
{
    /// <summary>
    /// 动作名称。
    /// 必须与 Capability 的 Name 对齐。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 动作执行顺序。
    /// 从 1 开始递增。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 动作说明。
    /// 用于帮助系统和人工理解这一步的作用。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 动作参数。
    /// 由 Planner 生成，Capability 在执行时读取。
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];
}
