using AgentKernel.Abstractions;

namespace AgentKernel.Extensions.Chat;

/// <summary>
/// 普通聊天回复能力。
/// 通过注入的聊天服务生成自然语言回复。
/// </summary>
public class ChatReplyCapability : ITaskCapability
{
    private readonly IChatReplyService _chatReplyService;

    public ChatReplyCapability(IChatReplyService chatReplyService)
    {
        _chatReplyService = chatReplyService ?? throw new ArgumentNullException(nameof(chatReplyService));
    }

    public string Name => "chat_reply";

    public bool CanHandle(TaskActionDefinition action)
    {
        return action is not null &&
               string.Equals(action.Name, Name, StringComparison.OrdinalIgnoreCase);
    }

    public async Task ExecuteAsync(
        TaskExecutionContext context,
        TaskActionDefinition action,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        ChatReplyResult result;

        if (action.Parameters.TryGetValue("reply", out string? presetReply) &&
            !string.IsNullOrWhiteSpace(presetReply))
        {
            result = new ChatReplyResult
            {
                ReplyText = presetReply.Trim(),
                Source = "preset"
            };
        }
        else
        {
            string userGoal = context.TaskDefinition?.UserGoal?.Trim() ?? string.Empty;
            result = await _chatReplyService.GenerateReplyAsync(userGoal, cancellationToken);
        }

        context.WorkingMemory[ChatMemoryKeys.ChatReplyText] = result.ReplyText;
        context.WorkingMemory[ChatMemoryKeys.ChatReplySource] = result.Source;
        context.Outputs[ChatMemoryKeys.ChatReplyText] = result.ReplyText;
        context.Outputs[ChatMemoryKeys.ChatReplySource] = result.Source;
        context.AddLog($"ChatReplyCapability 已生成普通聊天回复。Source={result.Source}");
    }
}
