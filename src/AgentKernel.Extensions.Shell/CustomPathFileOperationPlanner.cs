using System.IO;
using System.Text.RegularExpressions;

namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 处理带完整路径的文件操作（当前先支持重命名）。
/// 规则：目标必须用英文双引号包裹。
/// </summary>
public static class CustomPathFileOperationPlanner
{
    public static bool TryPlanRename(
        string userInstruction,
        string source,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        if (!KnownLocationTargetParser.LooksLikeRenameRequest(userInstruction))
        {
            return false;
        }

        List<string> quoted = ExtractQuotedSegments(userInstruction);
        if (quoted.Count < 2)
        {
            return BuildMissingQuotePlan(source, out plan);
        }

        string rawPath = quoted[0];
        string newName = quoted[1];

        if (!LooksLikePath(rawPath))
        {
            return false;
        }

        if (ContainsPathSeparator(newName))
        {
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Rename,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = ShellTargetKind.Unknown,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage = "请把新名称只写成名称本身，不要包含路径。例如：把\"C:\\Path\\旧名字.txt\"改名为\"新名字.txt\"。",
                Reason = "重命名的目标名称包含路径，需改为纯名称。"
            };
            return true;
        }

        string normalizedPath = NormalizePath(rawPath);
        if (!File.Exists(normalizedPath) && !Directory.Exists(normalizedPath))
        {
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Rename,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = ShellTargetKind.Unknown,
                TargetPath = normalizedPath,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage = $"我没有在这个路径找到目标：\"{normalizedPath}\"。请确认路径是否正确。",
                Reason = "重命名目标路径不存在。"
            };
            return true;
        }

        ShellTargetKind kind = Directory.Exists(normalizedPath)
            ? ShellTargetKind.Directory
            : ShellTargetKind.File;

        string directoryScopeNote = kind == ShellTargetKind.Directory
            ? "\n\n注意：当前目标是文件夹，重命名后文件夹内部内容不变，但会影响引用该文件夹的路径。"
            : string.Empty;

        plan = new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Rename,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = kind,
            TargetName = Path.GetFileName(normalizedPath),
            TargetPath = normalizedPath,
            Arguments = newName,
            ShouldExecute = true,
            RequiresConfirmation = true,
            AssistantMessage = $"这条操作需要你先确认，我已经准备把“{Path.GetFileName(normalizedPath)}”重命名为“{newName}”。{directoryScopeNote}",
            Reason = kind == ShellTargetKind.Directory
                ? "识别为文件夹重命名请求，需要确认后执行。"
                : "识别为文件重命名请求，需要确认后执行。"
        };

        return true;
    }

    private static bool BuildMissingQuotePlan(string source, out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Rename,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = ShellTargetKind.Unknown,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage = "为了安全处理重命名，请把原路径和新名称都用英文双引号括起来。例如：把\"C:\\Path\\旧名字.txt\"改名为\"新名字.txt\"。",
            Reason = "重命名请求缺少英文双引号包裹的路径/名称。"
        };
        return true;
    }

    private static List<string> ExtractQuotedSegments(string text)
    {
        List<string> results = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        MatchCollection matches = Regex.Matches(text, "\"(?<name>[^\"]{1,520})\"");
        foreach (Match match in matches.Cast<Match>())
        {
            string candidate = match.Groups["name"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                results.Add(candidate);
            }
        }

        return results;
    }

    private static bool LooksLikePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return trimmed.Contains("\\", StringComparison.Ordinal)
               || trimmed.Contains("/", StringComparison.Ordinal)
               || Regex.IsMatch(trimmed, "^[a-zA-Z]:\\\\");
    }

    private static bool ContainsPathSeparator(string value)
    {
        return value.Contains("\\", StringComparison.Ordinal) ||
               value.Contains("/", StringComparison.Ordinal);
    }

    private static string NormalizePath(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.StartsWith("~\\", StringComparison.Ordinal))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, trimmed[2..]);
        }

        return trimmed;
    }
}
