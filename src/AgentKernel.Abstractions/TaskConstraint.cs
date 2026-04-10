namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务约束条件。
/// 用于描述任务执行时必须满足的筛选条件、目标条件或限制条件。
/// </summary>
public class TaskConstraint
{
    /// <summary>
    /// 约束名称。
    /// 例如：capture_time、location、file_type。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 约束操作符。
    /// 例如：equals、contains、in_month、greater_than。
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// 约束值。
    /// 例如：去年3月、长城、.dwg。
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 约束说明。
    /// 用于帮助系统或人工理解该约束的含义。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
