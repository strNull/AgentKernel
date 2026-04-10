namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示从自然语言中提取出的已知目录目标请求。
/// </summary>
public sealed class KnownLocationTargetRequest
{
    public ShellTargetLocation Location { get; init; } = ShellTargetLocation.Unknown;

    public ShellTargetKind TargetKind { get; init; } = ShellTargetKind.Unknown;

    public string TargetName { get; init; } = string.Empty;

    public string DestinationName { get; init; } = string.Empty;

    public bool PreferExactMatch { get; init; }
}
