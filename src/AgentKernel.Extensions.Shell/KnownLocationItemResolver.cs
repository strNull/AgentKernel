using System.IO;

namespace AgentKernel.Extensions.Shell;

public enum KnownLocationResolutionStatus
{
    NotFound = 0,
    SingleMatch = 1,
    Ambiguous = 2
}

public sealed class KnownLocationResolution
{
    public KnownLocationResolutionStatus Status { get; init; }

    public string BasePath { get; init; } = string.Empty;

    public string RequestedName { get; init; } = string.Empty;

    public string ResolvedName { get; init; } = string.Empty;

    public string ResolvedPath { get; init; } = string.Empty;

    public ShellTargetKind ResolvedTargetKind { get; init; } = ShellTargetKind.Unknown;

    public IReadOnlyList<string> Candidates { get; init; } = [];
}

/// <summary>
/// 负责把已知目录中的目标名称解析成真实文件系统路径。
/// 匹配规则：
/// 1. 有后缀时优先按文件精确匹配
/// 2. 无后缀时先匹配文件，再匹配文件夹
/// 3. 依次进行：精确匹配 -> 去后缀精确匹配 -> 名称包含匹配
/// </summary>
public static class KnownLocationItemResolver
{
    public static KnownLocationResolution ResolveSingle(
        ShellTargetLocation location,
        string requestedName,
        ShellTargetKind targetKind = ShellTargetKind.Unknown,
        bool preferExactMatch = false)
    {
        if (location == ShellTargetLocation.Unknown || string.IsNullOrWhiteSpace(requestedName))
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.NotFound,
                RequestedName = requestedName?.Trim() ?? string.Empty
            };
        }

        string basePath = ResolveBasePath(location);
        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.NotFound,
                BasePath = basePath,
                RequestedName = requestedName.Trim()
            };
        }

        string rawRequested = requestedName.Trim();
        DirectoryInfo directory = new(basePath);
        List<FileSystemInfo> candidates = MatchSmart(directory, rawRequested, targetKind);

        if (candidates.Count == 1)
        {
            FileSystemInfo match = candidates[0];
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.SingleMatch,
                BasePath = basePath,
                RequestedName = rawRequested,
                ResolvedName = match.Name,
                ResolvedPath = match.FullName,
                ResolvedTargetKind = match is DirectoryInfo
                    ? ShellTargetKind.Directory
                    : ShellTargetKind.File
            };
        }

        if (candidates.Count > 1)
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.Ambiguous,
                BasePath = basePath,
                RequestedName = rawRequested,
                Candidates = candidates
                    .Select(entry => entry.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray()
            };
        }

        return new KnownLocationResolution
        {
            Status = KnownLocationResolutionStatus.NotFound,
            BasePath = basePath,
            RequestedName = rawRequested
        };
    }

    public static KnownLocationResolution ResolveSingleFromBasePath(
        string basePath,
        string requestedName,
        ShellTargetKind targetKind = ShellTargetKind.Unknown,
        bool preferExactMatch = false)
    {
        if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(requestedName))
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.NotFound,
                BasePath = basePath ?? string.Empty,
                RequestedName = requestedName?.Trim() ?? string.Empty
            };
        }

        if (!Directory.Exists(basePath))
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.NotFound,
                BasePath = basePath,
                RequestedName = requestedName.Trim()
            };
        }

        string rawRequested = requestedName.Trim();
        DirectoryInfo directory = new(basePath);
        List<FileSystemInfo> candidates = MatchSmart(directory, rawRequested, targetKind);

        if (candidates.Count == 1)
        {
            FileSystemInfo match = candidates[0];
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.SingleMatch,
                BasePath = basePath,
                RequestedName = rawRequested,
                ResolvedName = match.Name,
                ResolvedPath = match.FullName,
                ResolvedTargetKind = match is DirectoryInfo
                    ? ShellTargetKind.Directory
                    : ShellTargetKind.File
            };
        }

        if (candidates.Count > 1)
        {
            return new KnownLocationResolution
            {
                Status = KnownLocationResolutionStatus.Ambiguous,
                BasePath = basePath,
                RequestedName = rawRequested,
                Candidates = candidates
                    .Select(entry => entry.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray()
            };
        }

        return new KnownLocationResolution
        {
            Status = KnownLocationResolutionStatus.NotFound,
            BasePath = basePath,
            RequestedName = rawRequested
        };
    }

    private static List<FileSystemInfo> MatchSmart(
        DirectoryInfo directory,
        string requestedName,
        ShellTargetKind kindHint)
    {
        string target = requestedName.Trim();
        bool hasExtension = Path.HasExtension(target);
        ShellTargetKind effectiveKind = hasExtension && kindHint != ShellTargetKind.Directory
            ? ShellTargetKind.File
            : kindHint;

        IEnumerable<FileSystemInfo> fileItems = effectiveKind == ShellTargetKind.Directory
            ? Enumerable.Empty<FileSystemInfo>()
            : directory.EnumerateFiles().Cast<FileSystemInfo>();
        IEnumerable<FileSystemInfo> dirItems = effectiveKind == ShellTargetKind.File
            ? Enumerable.Empty<FileSystemInfo>()
            : directory.EnumerateDirectories().Cast<FileSystemInfo>();

        List<FileSystemInfo> files = fileItems.ToList();
        List<FileSystemInfo> dirs = dirItems.ToList();

        List<FileSystemInfo> exactFiles = files
            .Where(file => string.Equals(file.Name, target, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exactFiles.Count > 0)
        {
            return exactFiles;
        }

        List<FileSystemInfo> exactDirs = dirs
            .Where(dir => string.Equals(dir.Name, target, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exactDirs.Count > 0)
        {
            return exactDirs;
        }

        if (hasExtension)
        {
            return [];
        }

        List<FileSystemInfo> noExtensionFiles = files
            .Where(file => string.Equals(
                Path.GetFileNameWithoutExtension(file.Name),
                target,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (noExtensionFiles.Count > 0)
        {
            return noExtensionFiles;
        }

        List<FileSystemInfo> noExtensionDirs = dirs
            .Where(dir => string.Equals(
                Path.GetFileNameWithoutExtension(dir.Name),
                target,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (noExtensionDirs.Count > 0)
        {
            return noExtensionDirs;
        }

        List<FileSystemInfo> fuzzyFiles = files
            .Where(file => file.Name.Contains(target, StringComparison.OrdinalIgnoreCase))
            .ToList();
        List<FileSystemInfo> fuzzyDirs = dirs
            .Where(dir => dir.Name.Contains(target, StringComparison.OrdinalIgnoreCase))
            .Cast<FileSystemInfo>()
            .ToList();

        if (fuzzyFiles.Count > 0 && fuzzyDirs.Count > 0)
        {
            return fuzzyFiles.Concat(fuzzyDirs).ToList();
        }

        if (fuzzyFiles.Count > 0)
        {
            return fuzzyFiles;
        }

        return fuzzyDirs;
    }

    private static string ResolveBasePath(ShellTargetLocation location)
    {
        return location switch
        {
            ShellTargetLocation.Desktop => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ShellTargetLocation.Documents => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ShellTargetLocation.Downloads => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"),
            ShellTargetLocation.Pictures => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            _ => string.Empty
        };
    }
}
