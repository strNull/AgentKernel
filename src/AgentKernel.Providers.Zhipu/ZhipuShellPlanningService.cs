using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AgentKernel.Extensions.Shell;

namespace AgentKernel.Providers.Zhipu;

/// <summary>
/// 基于智谱兼容接口的 Shell 规划服务。
/// </summary>
public class ZhipuShellPlanningService : IShellPlanningService
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly HttpClient _httpClient;
    private readonly ZhipuOptions _options;
    private readonly IShellPlanningService _fallbackService;

    public ZhipuShellPlanningService(
        HttpClient httpClient,
        ZhipuOptions options,
        IShellPlanningService? fallbackService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fallbackService = fallbackService ?? new RuleBasedShellPlanningService();
    }

    public async Task<ShellPlanDraft> PlanAsync(
        string userInstruction,
        CancellationToken cancellationToken = default)
    {
        string text = userInstruction?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return await _fallbackService.PlanAsync(text, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return await _fallbackService.PlanAsync(text, cancellationToken);
        }

        try
        {
            string prompt = BuildPrompt(text);

            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(_options.ChatModelName)
                    ? "glm-4-flash"
                    : _options.ChatModelName.Trim(),
                stream = false,
                temperature = 0.2,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ChatCompletionsUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            string rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            string modelText = ExtractReplyText(rawJson);

            ShellPlanDraft? plan = JsonSerializer.Deserialize<ShellPlanDraft>(modelText, DeserializeOptions);
            if (plan is null)
            {
                return await _fallbackService.PlanAsync(text, cancellationToken);
            }

            NormalizePlan(plan);
            ApplyKnownLocationHints(text, plan);
            ApplyFileOperationConstraints(text, plan);

            if (ShouldRebuildCommandFromStructure(plan))
            {
                plan.PowerShellCommand = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(plan.PowerShellCommand) && plan.ShouldExecute)
            {
                plan.PowerShellCommand = ShellCommandBuilder.BuildCommand(plan);
            }

            plan.PowerShellCommand = NormalizePowerShellCommand(plan.PowerShellCommand);
            return plan;
        }
        catch
        {
            return await _fallbackService.PlanAsync(text, cancellationToken);
        }
    }

    private static string BuildPrompt(string userInstruction)
    {
        return
            "你是小智的 Shell 任务规划器。\n" +
            "你的职责是把用户输入转换成一个严格 JSON 对象，不要输出任何额外说明，不要输出 Markdown，也不要输出代码块。\n" +
            "assistantMessage 要自然、温和、简洁，稍微可爱一点，但不要太长。\n\n" +
            "你必须返回如下 JSON 结构：\n" +
            "{\n" +
            "  \"intentType\": \"Unknown | QuerySystemInfo | QueryFiles | LaunchApplication | FileOperation | ProcessControl\",\n" +
            "  \"operationType\": \"None | Query | Launch | Delete | Copy | Move | Rename\",\n" +
            "  \"targetLocation\": \"Unknown | CurrentDirectory | Desktop | Documents | Downloads | Pictures | CustomPath\",\n" +
            "  \"targetKind\": \"Unknown | File | Directory\",\n" +
            "  \"riskLevel\": \"Unknown | ReadOnly | Low | Medium | High\",\n" +
            "  \"shouldExecute\": true 或 false,\n" +
            "  \"assistantMessage\": \"给用户的自然语言前置回复\",\n" +
            "  \"powerShellCommand\": \"真正要执行的 PowerShell 命令；如果系统已能根据结构化字段安全构建命令，可以留空\",\n" +
            "  \"targetName\": \"文件名、文件夹名、程序名或目标名；没有则为空字符串\",\n" +
            "  \"targetPath\": \"绝对路径或受控路径；没有则为空字符串\",\n" +
            "  \"arguments\": \"附加参数；没有则为空字符串\",\n" +
            "  \"requiresConfirmation\": true 或 false,\n" +
            "  \"reason\": \"简短说明为什么这样判断\"\n" +
            "}\n\n" +
            "规则：\n" +
            "1. 如果这是普通聊天，不要执行命令，intentType 返回 Unknown，operationType 返回 None。\n" +
            "2. 如果是系统信息查询，intentType 返回 QuerySystemInfo，operationType 返回 Query。\n" +
            "3. 如果是文件或目录查询，intentType 返回 QueryFiles，operationType 返回 Query。\n" +
            "4. 如果是启动应用，intentType 返回 LaunchApplication，operationType 返回 Launch。\n" +
            "5. 如果是文件操作，intentType 返回 FileOperation，并设置对应的 operationType。\n" +
            "6. 删除、格式化、安装、卸载、结束关键进程等高风险任务，riskLevel 返回 High，并优先 requiresConfirmation=true。\n" +
            "7. 对于桌面、文档、下载、图片这类常见目录，优先使用 targetLocation；如果用户明确给出盘符或路径，使用 targetLocation=CustomPath 并把路径写入 targetPath，不要硬编码 C:\\Users\\YourUsername 或 C:\\Users\\%USERNAME%。\n" +
            "8. 对于文件操作，提取用户口语中最核心的目标名称填入 targetName。即使用户没有加双引号、没有说后缀名（比如“把桌面的1删了”），你也应该提取 targetName=\"1\"。\n" +
            "9. 如果用户明确说了类型（比如“1那个文件夹”或“文本文件”），在 targetKind 中标记 Directory 或 File；如果不确定，标记为 Unknown。\n" +
            "10. shouldExecute：只要解析出了意图和目标名称，就设为 true。因为底层系统会自动去模糊搜索并与用户确认。\n" +
            "11. 如果目标明确是文件夹，assistantMessage 与 reason 必须提示：该操作会连同文件夹内部内容一起处理。\n" +
            "12. 只能输出 JSON。\n\n" +
            "用户输入：\n" +
            userInstruction;
    }

    private static void NormalizePlan(ShellPlanDraft plan)
    {
        plan.Source = "zhipu";
        plan.AssistantMessage = plan.AssistantMessage?.Trim() ?? string.Empty;
        plan.PowerShellCommand = plan.PowerShellCommand?.Trim() ?? string.Empty;
        plan.TargetName = plan.TargetName?.Trim() ?? string.Empty;
        plan.TargetPath = plan.TargetPath?.Trim() ?? string.Empty;
        plan.Arguments = plan.Arguments?.Trim() ?? string.Empty;
        plan.Reason = plan.Reason?.Trim() ?? string.Empty;

        if (!plan.ShouldExecute)
        {
            plan.PowerShellCommand = string.Empty;
        }
    }

    private static void ApplyKnownLocationHints(string userInstruction, ShellPlanDraft plan)
    {
        if (!KnownLocationTargetParser.TryExtractKnownLocation(userInstruction, out ShellTargetLocation location))
        {
            return;
        }

        if (plan.TargetLocation == ShellTargetLocation.Unknown)
        {
            plan.TargetLocation = location;
        }

        if (plan.IntentType == ShellIntentType.QueryFiles &&
            plan.OperationType == ShellOperationType.None)
        {
            plan.OperationType = ShellOperationType.Query;
        }
    }

    private static void ApplyFileOperationConstraints(string userInstruction, ShellPlanDraft plan)
    {
        if (KnownLocationFileOperationPlanner.TryPlanFromModel(plan, "zhipu", out ShellPlanDraft modelPlan))
        {
            ApplyPlanOverride(plan, modelPlan);
            return;
        }

        if (KnownLocationFileOperationPlanner.TryPlanDelete(userInstruction, "zhipu", out ShellPlanDraft deletePlan))
        {
            plan.Source = deletePlan.Source;
            plan.IntentType = deletePlan.IntentType;
            plan.OperationType = deletePlan.OperationType;
            plan.RiskLevel = deletePlan.RiskLevel;
            plan.TargetLocation = deletePlan.TargetLocation;
            plan.TargetKind = deletePlan.TargetKind;
            plan.ShouldExecute = deletePlan.ShouldExecute;
            plan.AssistantMessage = deletePlan.AssistantMessage;
            plan.PowerShellCommand = deletePlan.PowerShellCommand;
            plan.RequiresConfirmation = deletePlan.RequiresConfirmation;
            plan.TargetName = deletePlan.TargetName;
            plan.TargetPath = deletePlan.TargetPath;
            plan.Arguments = deletePlan.Arguments;
            plan.Reason = deletePlan.Reason;
            return;
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(userInstruction, "zhipu", ShellOperationType.Copy, out ShellPlanDraft copyPlan))
        {
            ApplyPlanOverride(plan, copyPlan);
            return;
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(userInstruction, "zhipu", ShellOperationType.Move, out ShellPlanDraft movePlan))
        {
            ApplyPlanOverride(plan, movePlan);
            return;
        }

        if (KnownLocationFileOperationPlanner.TryPlanCopyMoveRename(userInstruction, "zhipu", ShellOperationType.Rename, out ShellPlanDraft renamePlan))
        {
            ApplyPlanOverride(plan, renamePlan);
            return;
        }

        if (CustomPathFileOperationPlanner.TryPlanRename(userInstruction, "zhipu", out ShellPlanDraft customRenamePlan))
        {
            ApplyPlanOverride(plan, customRenamePlan);
            return;
        }
    }

    private static void ApplyPlanOverride(ShellPlanDraft plan, ShellPlanDraft overridePlan)
    {
        plan.Source = overridePlan.Source;
        plan.IntentType = overridePlan.IntentType;
        plan.OperationType = overridePlan.OperationType;
        plan.RiskLevel = overridePlan.RiskLevel;
        plan.TargetLocation = overridePlan.TargetLocation;
        plan.TargetKind = overridePlan.TargetKind;
        plan.ShouldExecute = overridePlan.ShouldExecute;
        plan.AssistantMessage = overridePlan.AssistantMessage;
        plan.PowerShellCommand = overridePlan.PowerShellCommand;
        plan.RequiresConfirmation = overridePlan.RequiresConfirmation;
        plan.TargetName = overridePlan.TargetName;
        plan.TargetPath = overridePlan.TargetPath;
        plan.Arguments = overridePlan.Arguments;
        plan.Reason = overridePlan.Reason;
    }

    private static bool ShouldRebuildCommandFromStructure(ShellPlanDraft plan)
    {
        if (!plan.ShouldExecute)
        {
            return false;
        }

        if (plan.OperationType == ShellOperationType.Query &&
            plan.TargetLocation is ShellTargetLocation.Desktop
                or ShellTargetLocation.Documents
                or ShellTargetLocation.Downloads
                or ShellTargetLocation.Pictures
                or ShellTargetLocation.CurrentDirectory
                or ShellTargetLocation.CustomPath)
        {
            return true;
        }

        if (plan.OperationType == ShellOperationType.Delete &&
            (plan.TargetLocation != ShellTargetLocation.Unknown ||
             !string.IsNullOrWhiteSpace(plan.TargetPath) ||
             !string.IsNullOrWhiteSpace(plan.TargetName)))
        {
            return true;
        }

        return false;
    }

    private static string NormalizePowerShellCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.Empty;
        }

        string normalized = command;

        normalized = normalized.Replace(
            @"C:\Users\YourUsername\Desktop",
            @"$env:USERPROFILE\Desktop",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\YourUsername\Documents",
            @"$env:USERPROFILE\Documents",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\YourUsername\Downloads",
            @"$env:USERPROFILE\Downloads",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\YourUsername\Pictures",
            @"$env:USERPROFILE\Pictures",
            StringComparison.OrdinalIgnoreCase);

        normalized = normalized.Replace(
            @"C:\Users\%USERNAME%\Desktop",
            @"$env:USERPROFILE\Desktop",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\%USERNAME%\Documents",
            @"$env:USERPROFILE\Documents",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\%USERNAME%\Downloads",
            @"$env:USERPROFILE\Downloads",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\%USERNAME%\Pictures",
            @"$env:USERPROFILE\Pictures",
            StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace(
            @"C:\Users\%USERNAME%",
            @"$env:USERPROFILE",
            StringComparison.OrdinalIgnoreCase);

        normalized = Regex.Replace(
            normalized,
            @"Get-ChildItem\s+-Path\s+['""]Desktop['""]",
            "Get-ChildItem $env:USERPROFILE\\Desktop",
            RegexOptions.IgnoreCase);
        normalized = Regex.Replace(
            normalized,
            @"Get-ChildItem\s+-Path\s+['""]Documents['""]",
            "Get-ChildItem $env:USERPROFILE\\Documents",
            RegexOptions.IgnoreCase);
        normalized = Regex.Replace(
            normalized,
            @"Get-ChildItem\s+-Path\s+['""]Downloads['""]",
            "Get-ChildItem $env:USERPROFILE\\Downloads",
            RegexOptions.IgnoreCase);
        normalized = Regex.Replace(
            normalized,
            @"Get-ChildItem\s+-Path\s+['""]Pictures['""]",
            "Get-ChildItem $env:USERPROFILE\\Pictures",
            RegexOptions.IgnoreCase);

        return normalized;
    }

    private static string ExtractReplyText(string rawJson)
    {
        using JsonDocument doc = JsonDocument.Parse(rawJson);

        JsonElement contentElement = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content");

        string text = contentElement.ValueKind switch
        {
            JsonValueKind.String => contentElement.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Concat(contentElement.EnumerateArray()
                .Select(item => item.TryGetProperty("text", out JsonElement textProp)
                    ? textProp.GetString()
                    : string.Empty)),
            _ => string.Empty
        };

        text = text.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            text = text.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            text = text[start..(end + 1)];
        }

        return text;
    }
}
