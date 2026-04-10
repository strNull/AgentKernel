namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示基于内存的待确认 Shell 草案存储服务。
/// 当前版本先按单好友单会话保留一条待确认任务。
/// </summary>
public class InMemoryPendingShellConfirmationStore : IPendingShellConfirmationStore
{
    private PendingShellConfirmation? _current;

    /// <summary>
    /// 保存当前待确认草案。
    /// </summary>
    /// <param name="confirmation">待确认草案。</param>
    public void Save(PendingShellConfirmation confirmation)
    {
        _current = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
    }

    /// <summary>
    /// 尝试获取当前待确认草案。
    /// </summary>
    /// <param name="confirmation">输出待确认草案。</param>
    /// <returns>如果存在则返回 true。</returns>
    public bool TryGet(out PendingShellConfirmation? confirmation)
    {
        confirmation = _current;
        return confirmation is not null;
    }

    /// <summary>
    /// 清除当前待确认草案。
    /// </summary>
    public void Clear()
    {
        _current = null;
    }
}
