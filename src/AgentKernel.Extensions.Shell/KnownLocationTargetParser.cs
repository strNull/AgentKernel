using System.IO;
using System.Text.RegularExpressions;

namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 从自然语言中解析已知目录、文件操作意图与英文双引号包裹的目标名称。
/// 规则：文件操作目标必须用英文双引号括起来。
/// </summary>
public static class KnownLocationTargetParser
{
    private static readonly string[] DeleteKeywords =
    [
        "删除",
        "删掉",
        "删了",
        "移除",
        "remove",
        "remove-item"
    ];

    private static readonly string[] CopyKeywords =
    [
        "复制",
        "拷贝",
        "copy",
        "copy-item"
    ];

    private static readonly string[] MoveKeywords =
    [
        "移动",
        "搬移",
        "move",
        "move-item"
    ];

    private static readonly string[] RenameKeywords =
    [
        "重命名",
        "改名",
        "改为",
        "rename",
        "rename-item"
    ];

    public static bool LooksLikeDeleteRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (string keyword in DeleteKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool LooksLikeCopyRequest(string text) => ContainsAny(text, CopyKeywords);

    public static bool LooksLikeMoveRequest(string text) => ContainsAny(text, MoveKeywords);

    public static bool LooksLikeRenameRequest(string text) => ContainsAny(text, RenameKeywords);

    public static bool TryExtractKnownLocation(string text, out ShellTargetLocation location)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            location = ShellTargetLocation.Unknown;
            return false;
        }

        if (text.Contains("桌面", StringComparison.OrdinalIgnoreCase))
        {
            location = ShellTargetLocation.Desktop;
            return true;
        }

        if (text.Contains("文档", StringComparison.OrdinalIgnoreCase))
        {
            location = ShellTargetLocation.Documents;
            return true;
        }

        if (text.Contains("下载", StringComparison.OrdinalIgnoreCase))
        {
            location = ShellTargetLocation.Downloads;
            return true;
        }

        if (text.Contains("图片", StringComparison.OrdinalIgnoreCase))
        {
            location = ShellTargetLocation.Pictures;
            return true;
        }

        location = ShellTargetLocation.Unknown;
        return false;
    }

    public static string GetLocationDisplayName(ShellTargetLocation location)
    {
        return location switch
        {
            ShellTargetLocation.Desktop => "桌面",
            ShellTargetLocation.Documents => "文档",
            ShellTargetLocation.Downloads => "下载",
            ShellTargetLocation.Pictures => "图片",
            ShellTargetLocation.CurrentDirectory => "当前目录",
            _ => "目标位置"
        };
    }

    public static bool TryExtractKnownLocationDeleteTarget(
        string text,
        out KnownLocationTargetRequest request)
    {
        request = new KnownLocationTargetRequest();

        if (!LooksLikeDeleteRequest(text))
        {
            return false;
        }

        if (!TryExtractKnownLocation(text, out ShellTargetLocation location))
        {
            return false;
        }

        if (!TryExtractEnglishQuotedName(text, out string targetName))
        {
            return false;
        }

        request = new KnownLocationTargetRequest
        {
            Location = location,
            TargetKind = Path.HasExtension(targetName)
                ? ShellTargetKind.File
                : ShellTargetKind.Unknown,
            TargetName = targetName,
            PreferExactMatch = true
        };

        return true;
    }

    public static bool TryExtractKnownLocationBinaryTarget(
        string text,
        out KnownLocationTargetRequest request)
    {
        request = new KnownLocationTargetRequest();

        if (!TryExtractKnownLocation(text, out ShellTargetLocation location))
        {
            return false;
        }

        if (!TryExtractTwoEnglishQuotedNames(text, out string sourceName, out string destinationName))
        {
            return false;
        }

        request = new KnownLocationTargetRequest
        {
            Location = location,
            TargetKind = Path.HasExtension(sourceName)
                ? ShellTargetKind.File
                : ShellTargetKind.Unknown,
            TargetName = sourceName,
            DestinationName = destinationName,
            PreferExactMatch = true
        };

        return true;
    }

    public static bool TryExtractKnownLocationDeleteIntent(
        string text,
        out ShellTargetLocation location,
        out ShellTargetKind targetKind)
    {
        location = ShellTargetLocation.Unknown;
        targetKind = ShellTargetKind.Unknown;

        if (!LooksLikeDeleteRequest(text))
        {
            return false;
        }

        if (!TryExtractKnownLocation(text, out location))
        {
            return false;
        }

        targetKind = InferTargetKind(text);
        return true;
    }

    public static string BuildQuotedTargetExample(ShellTargetLocation location, ShellTargetKind targetKind)
    {
        string locationName = GetLocationDisplayName(location);
        return targetKind switch
        {
            ShellTargetKind.Directory => $"删除{locationName}上的\"文件夹1\"",
            _ => $"删除{locationName}上的\"测试文件.txt\""
        };
    }

    public static bool HasEnglishQuotedTarget(string text)
    {
        return TryExtractEnglishQuotedName(text, out _);
    }

    public static bool TryExtractEnglishQuotedTargetName(string text, out string targetName)
    {
        return TryExtractEnglishQuotedName(text, out targetName);
    }

    public static bool HasTwoEnglishQuotedTargets(string text)
    {
        return TryExtractTwoEnglishQuotedNames(text, out _, out _);
    }

    public static bool TryExtractTwoEnglishQuotedTargetNames(
        string text,
        out string sourceName,
        out string destinationName)
    {
        return TryExtractTwoEnglishQuotedNames(text, out sourceName, out destinationName);
    }

    private static bool TryExtractEnglishQuotedName(string text, out string targetName)
    {
        targetName = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // 1. 优先尝试英文双引号匹配
        MatchCollection matches = Regex.Matches(text, "\"(?<name>[^\"]{1,260})\"");
        foreach (Match match in matches.Cast<Match>())
        {
            string candidate = NormalizeCandidate(match.Groups["name"].Value);
            if (IsValidTargetName(candidate))
            {
                targetName = candidate;
                return true;
            }
        }

        // 2. 备选方案：尝试识别“叫[xxx]的文件”、“名为[xxx]的文件”
        var semanticMatch = Regex.Match(text, "(?:叫|名为|名字是|名称是|名为)(?<name>[a-zA-Z0-9\\._\\-\u4e00-\u9fa5]+)(?:的)?(?:文件|文件夹|目录)?");
        if (semanticMatch.Success)
        {
            string candidate = NormalizeCandidate(semanticMatch.Groups["name"].Value);
            if (IsValidTargetName(candidate))
            {
                targetName = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryExtractTwoEnglishQuotedNames(
        string text,
        out string sourceName,
        out string destinationName)
    {
        sourceName = string.Empty;
        destinationName = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        MatchCollection matches = Regex.Matches(text, "\"(?<name>[^\"]{1,260})\"");
        if (matches.Count < 2)
        {
            return false;
        }

        string first = NormalizeCandidate(matches[0].Groups["name"].Value);
        string second = NormalizeCandidate(matches[1].Groups["name"].Value);

        if (!IsValidTargetName(first) || !IsValidTargetName(second))
        {
            return false;
        }

        sourceName = first;
        destinationName = second;
        return true;
    }

    private static string NormalizeCandidate(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Trim('"');
    }

    private static bool IsValidTargetName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string candidate = value.Trim();
        if (candidate.StartsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        return candidate.Any(ch => !char.IsWhiteSpace(ch));
    }

    private static ShellTargetKind InferTargetKind(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ShellTargetKind.Unknown;
        }

        if (text.Contains("文件夹", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("目录", StringComparison.OrdinalIgnoreCase))
        {
            return ShellTargetKind.Directory;
        }

        if (text.Contains("文件", StringComparison.OrdinalIgnoreCase))
        {
            return ShellTargetKind.File;
        }

        return ShellTargetKind.Unknown;
    }

    public static bool TryExtractDriveBasePath(
        string text,
        out string basePath,
        out string displayName)
    {
        basePath = string.Empty;
        displayName = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        Match driveDiskMatch = Regex.Match(text, "(?i)(?<drive>[A-Z])盘");
        if (driveDiskMatch.Success)
        {
            string drive = driveDiskMatch.Groups["drive"].Value.ToUpperInvariant();
            basePath = $"{drive}:\\";
            displayName = $"{drive}盘";
            return Directory.Exists(basePath);
        }

        Match drivePathMatch = Regex.Match(text, "(?i)\\b(?<drive>[A-Z]):\\\\");
        if (drivePathMatch.Success)
        {
            string drive = drivePathMatch.Groups["drive"].Value.ToUpperInvariant();
            basePath = $"{drive}:\\";
            displayName = $"{drive}盘";
            return Directory.Exists(basePath);
        }

        return false;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
