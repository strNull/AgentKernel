namespace AgentKernel.Abstractions;

/// <summary>
/// 表示任务的输出定义。
/// 用于描述任务执行后需要交付的结果形态和目标位置。
/// </summary>
public class TaskOutputDefinition
{
    /// <summary>
    /// 输出类型。
    /// 例如：directory、text_file、json_report。
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 输出目标位置。
    /// 例如目录路径、文件路径、目标资源标识。
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// 输出说明。
    /// 用于说明该输出的业务含义。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 输出参数。
    /// 用于附加输出配置。
    /// 例如编码、格式、覆盖策略等。
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];
}
