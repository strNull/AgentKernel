namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 任务的意图类型。
/// 用于把自然语言请求归类为更稳定的终端动作类型。
/// </summary>
public enum ShellIntentType
{
    /// <summary>
    /// 未识别或不属于 Shell 工具任务。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 查询系统或环境信息。
    /// </summary>
    QuerySystemInfo = 1,

    /// <summary>
    /// 查询文件、目录或文件属性。
    /// </summary>
    QueryFiles = 2,

    /// <summary>
    /// 启动应用、程序或打开目标。
    /// </summary>
    LaunchApplication = 3,

    /// <summary>
    /// 文件操作，例如复制、移动、重命名等。
    /// </summary>
    FileOperation = 4,

    /// <summary>
    /// 进程控制，例如结束进程、启动或停止服务。
    /// </summary>
    ProcessControl = 5
}
