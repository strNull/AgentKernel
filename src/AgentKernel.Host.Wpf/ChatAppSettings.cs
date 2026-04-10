namespace AgentKernel.Host.Wpf;

/// <summary>
/// 表示 Host.Wpf 的本地聊天配置。
/// </summary>
public class ChatAppSettings
{
    /// <summary>
    /// 智谱配置。
    /// </summary>
    public ZhipuChatSettings Zhipu { get; set; } = new();
}

/// <summary>
/// 表示智谱聊天配置。
/// </summary>
public class ZhipuChatSettings
{
    /// <summary>
    /// API Key。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 聊天模型名称。
    /// </summary>
    public string ChatModelName { get; set; } = "glm-4-flash";

    /// <summary>
    /// 聊天接口地址。
    /// </summary>
    public string ChatCompletionsUrl { get; set; } = "https://api.z.ai/api/paas/v4/chat/completions";
}
