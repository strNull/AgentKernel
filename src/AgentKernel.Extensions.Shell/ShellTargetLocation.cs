namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 任务的目标位置。
/// </summary>
public enum ShellTargetLocation
{
    Unknown = 0,
    CurrentDirectory = 1,
    Desktop = 2,
    Documents = 3,
    Downloads = 4,
    Pictures = 5,
    CustomPath = 6
}
