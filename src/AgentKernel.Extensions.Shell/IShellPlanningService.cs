namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 表示 Shell 领域规划服务。
/// 用于根据用户输入生成执行前的 Shell 规划草案。
/// </summary>
public interface IShellPlanningService
{
    /// <summary>
    /// 根据用户输入生成 Shell 规划草案。
    /// </summary>
    /// <param name="userInstruction">用户输入。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Shell 规划草案。</returns>
    Task<ShellPlanDraft> PlanAsync(
        string userInstruction,
        CancellationToken cancellationToken = default);
}
