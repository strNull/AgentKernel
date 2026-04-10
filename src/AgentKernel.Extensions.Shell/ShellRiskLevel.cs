namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 任务的风险等级。
/// 用于后续确认机制、策略限制和 UI 风险提示。
/// </summary>
public enum ShellRiskLevel
{
    /// <summary>
    /// 未知风险。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 纯查询、只读、无副作用。
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// 低风险，例如启动本地系统应用。
    /// </summary>
    Low = 2,

    /// <summary>
    /// 中风险，例如复制、移动、重命名等可能影响文件状态的操作。
    /// </summary>
    Medium = 3,

    /// <summary>
    /// 高风险，例如删除、格式化、安装、结束进程等。
    /// </summary>
    High = 4
}
