namespace AgentKernel.Providers.Zhipu;

/// <summary>
/// 表示智谱 Provider 的配置选项。
/// </summary>
public class ZhipuOptions
{
    /// <summary>
    /// 智谱兼容接口 API Key。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 聊天模型名称。
    /// </summary>
    public string ChatModelName { get; set; } = "glm-4-flash";

    /// <summary>
    /// 聊天请求接口地址。
    /// </summary>
    public string ChatCompletionsUrl { get; set; } = "https://api.z.ai/api/paas/v4/chat/completions";
}
