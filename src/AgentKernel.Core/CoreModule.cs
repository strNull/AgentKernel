using AgentKernel.Abstractions;

namespace AgentKernel.Core;

/// <summary>
/// 表示 Core 层的最小模块注册入口。
/// 用于集中注册当前阶段的基础能力。
/// </summary>
public static class CoreModule
{
    /// <summary>
    /// 向指定的能力注册中心注册 Core 层当前可用能力。
    /// </summary>
    /// <param name="registry">能力注册中心。</param>
    public static void Register(CapabilityRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.Register(
            new MockStepCapability(),
            new CapabilityDescriptor
            {
                Name = "mock_step",
                DisplayName = "最小测试动作",
                Description = "用于打通第三阶段第一版 Agent Kernel 执行主线的测试能力。",
                Category = "mock",
                Domain = "general",
                Consumes = [],
                Produces = ["mock_result", "mock_output"],
                RequiresModel = false,
                SupportsReview = false
            });
    }
}
