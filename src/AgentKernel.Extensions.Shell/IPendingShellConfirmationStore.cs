namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示待确认 Shell 执行草案的存储服务。
/// </summary>
public interface IPendingShellConfirmationStore
{
    /// <summary>
    /// 保存当前待确认草案。
    /// </summary>
    /// <param name="confirmation">待确认草案。</param>
    void Save(PendingShellConfirmation confirmation);

    /// <summary>
    /// 尝试获取当前待确认草案。
    /// </summary>
    /// <param name="confirmation">输出待确认草案。</param>
    /// <returns>如果存在则返回 true。</returns>
    bool TryGet(out PendingShellConfirmation? confirmation);

    /// <summary>
    /// 清除当前待确认草案。
    /// </summary>
    void Clear();
}
