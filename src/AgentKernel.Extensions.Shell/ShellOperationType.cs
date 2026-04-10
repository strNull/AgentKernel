namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 任务的具体操作类型。
/// </summary>
public enum ShellOperationType
{
    None = 0,
    Query = 1,
    Launch = 2,
    Delete = 3,
    Copy = 4,
    Move = 5,
    Rename = 6
}
