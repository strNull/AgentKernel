using AgentKernel.Abstractions;
using AgentKernel.Core;

namespace AgentKernel.Extensions.Chat;

/// <summary>
/// 表示 Chat 扩展模块注册入口。
/// </summary>
public static class ChatModule
{
    /// <summary>
    /// 向指定的能力注册中心注册 Chat 扩展能力。
    /// </summary>
    /// <param name="registry">能力注册中心。</param>
    /// <param name="chatReplyService">聊天回复服务。</param>
    public static void Register(
        CapabilityRegistry registry,
        IChatReplyService chatReplyService)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (chatReplyService is null)
        {
            throw new ArgumentNullException(nameof(chatReplyService));
        }

        registry.Register(
            new ChatReplyCapability(chatReplyService),
            new CapabilityDescriptor
            {
                Name = "chat_reply",
                DisplayName = "普通聊天回复",
                Description = "处理普通聊天消息，并生成自然语言回复。",
                Category = "chat",
                Domain = "chat",
                Consumes = [],
                Produces = [ChatMemoryKeys.ChatReplyText],
                RequiresModel = false,
                SupportsReview = false
            });
    }
}
