using System.IO;
using System.Net.Http;
using System.Text.Json;
using AgentKernel.Abstractions;
using AgentKernel.Core;
using AgentKernel.Extensions.Chat;
using AgentKernel.Extensions.Shell;
using AgentKernel.Providers.Zhipu;

namespace AgentKernel.Host.Wpf;

/// <summary>
/// 表示 WPF 宿主的最小装配入口。
/// 当前阶段用于装配单一全功能 AI 好友主线。
/// </summary>
public static class AgentKernelBootstrapper
{
    private static readonly HttpClient SharedHttpClient = new();

    /// <summary>
    /// 构建一个最小可运行的 Agent 服务实例。
    /// </summary>
    public static ITaskAgentService BuildAgent()
    {
        var registry = new CapabilityRegistry();

        ChatAppSettings settings = LoadSettings();

        var zhipuOptions = new ZhipuOptions
        {
            ApiKey = settings.Zhipu.ApiKey,
            ChatModelName = settings.Zhipu.ChatModelName,
            ChatCompletionsUrl = settings.Zhipu.ChatCompletionsUrl
        };

        IChatReplyService fallbackChatReplyService = new FallbackChatReplyService();
        IChatReplyService chatReplyService = new ZhipuChatReplyService(
            SharedHttpClient,
            zhipuOptions,
            fallbackChatReplyService);

        IShellPlanningService fallbackShellPlanningService = new RuleBasedShellPlanningService();
        IShellPlanningService shellPlanningService = new ZhipuShellPlanningService(
            SharedHttpClient,
            zhipuOptions,
            fallbackShellPlanningService);

        IShellResultSummaryService fallbackShellResultSummaryService = new RuleBasedShellResultSummaryService();
        IShellResultSummaryService shellResultSummaryService = new ZhipuShellResultSummaryService(
            SharedHttpClient,
            zhipuOptions,
            fallbackShellResultSummaryService);

        IPendingShellConfirmationStore pendingShellConfirmationStore = new InMemoryPendingShellConfirmationStore();

        ChatModule.Register(registry, chatReplyService);
        ShellModule.Register(registry, shellResultSummaryService, pendingShellConfirmationStore);

        ITaskPlanningService planningService = new ShellPlanningService(
            shellPlanningService,
            pendingShellConfirmationStore);
        ITaskExecutionEngine executionEngine = new TaskExecutionEngine(registry);
        ITaskAgentService agentService = new TaskAgentService(planningService, executionEngine);

        return agentService;
    }

    private static ChatAppSettings LoadSettings()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
        if (!File.Exists(filePath))
        {
            return new ChatAppSettings();
        }

        string json = File.ReadAllText(filePath);
        ChatAppSettings? settings = JsonSerializer.Deserialize<ChatAppSettings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return settings ?? new ChatAppSettings();
    }
}
