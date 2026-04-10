namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 将结构化 Shell 草案转换成受控的 PowerShell 命令。
/// </summary>
public static class ShellCommandBuilder
{
    public static string BuildCommand(ShellPlanDraft plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!string.IsNullOrWhiteSpace(plan.PowerShellCommand))
        {
            return plan.PowerShellCommand.Trim();
        }

        return plan.OperationType switch
        {
            ShellOperationType.Query => BuildQueryCommand(plan),
            ShellOperationType.Launch => BuildLaunchCommand(plan),
            ShellOperationType.Delete => BuildDeleteCommand(plan),
            ShellOperationType.Copy => BuildCopyCommand(plan),
            ShellOperationType.Move => BuildMoveCommand(plan),
            ShellOperationType.Rename => BuildRenameCommand(plan),
            _ => string.Empty
        };
    }

    private static string BuildQueryCommand(ShellPlanDraft plan)
    {
        return plan.TargetLocation switch
        {
            ShellTargetLocation.CurrentDirectory => "Get-ChildItem",
            ShellTargetLocation.Desktop => "Get-ChildItem $env:USERPROFILE\\Desktop",
            ShellTargetLocation.Documents => "Get-ChildItem $env:USERPROFILE\\Documents",
            ShellTargetLocation.Downloads => "Get-ChildItem $env:USERPROFILE\\Downloads",
            ShellTargetLocation.Pictures => "Get-ChildItem $env:USERPROFILE\\Pictures",
            ShellTargetLocation.CustomPath when !string.IsNullOrWhiteSpace(plan.TargetPath)
                => $"Get-ChildItem -LiteralPath '{EscapeSingleQuoted(plan.TargetPath)}'",
            _ => string.Empty
        };
    }

    private static string BuildLaunchCommand(ShellPlanDraft plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.TargetPath))
        {
            return string.IsNullOrWhiteSpace(plan.Arguments)
                ? $"Start-Process -LiteralPath '{EscapeSingleQuoted(plan.TargetPath)}'"
                : $"Start-Process -LiteralPath '{EscapeSingleQuoted(plan.TargetPath)}' -ArgumentList '{EscapeSingleQuoted(plan.Arguments)}'";
        }

        if (string.IsNullOrWhiteSpace(plan.TargetName))
        {
            return string.Empty;
        }

        string normalizedTarget = NormalizeLaunchTarget(plan.TargetName);
        return string.IsNullOrWhiteSpace(plan.Arguments)
            ? $"Start-Process {normalizedTarget}"
            : $"Start-Process {normalizedTarget} -ArgumentList '{EscapeSingleQuoted(plan.Arguments)}'";
    }

    private static string BuildDeleteCommand(ShellPlanDraft plan)
    {
        string targetExpression = ResolveDeleteTargetExpression(plan);
        if (string.IsNullOrWhiteSpace(targetExpression))
        {
            return string.Empty;
        }

        return
            $"$target = {targetExpression}; " +
            "if (Test-Path -LiteralPath $target -PathType Container) { " +
            "Remove-Item -LiteralPath $target -Recurse -Force " +
            "} else { " +
            "Remove-Item -LiteralPath $target -Force " +
            "}";
    }

    private static string BuildCopyCommand(ShellPlanDraft plan)
    {
        string sourceExpression = ResolveDeleteTargetExpression(plan);
        if (string.IsNullOrWhiteSpace(sourceExpression))
        {
            return string.Empty;
        }

        string destinationExpression = ResolveDestinationExpression(plan);
        if (string.IsNullOrWhiteSpace(destinationExpression))
        {
            return string.Empty;
        }

        return
            $"$source = {sourceExpression}; " +
            $"$dest = {destinationExpression}; " +
            "if (Test-Path -LiteralPath $source -PathType Container) { " +
            "Copy-Item -LiteralPath $source -Destination $dest -Recurse -Force " +
            "} else { " +
            "Copy-Item -LiteralPath $source -Destination $dest -Force " +
            "}";
    }

    private static string BuildMoveCommand(ShellPlanDraft plan)
    {
        string sourceExpression = ResolveDeleteTargetExpression(plan);
        if (string.IsNullOrWhiteSpace(sourceExpression))
        {
            return string.Empty;
        }

        string destinationExpression = ResolveDestinationExpression(plan);
        if (string.IsNullOrWhiteSpace(destinationExpression))
        {
            return string.Empty;
        }

        return
            $"$source = {sourceExpression}; " +
            $"$dest = {destinationExpression}; " +
            "if (Test-Path -LiteralPath $source -PathType Container) { " +
            "Move-Item -LiteralPath $source -Destination $dest -Force " +
            "} else { " +
            "Move-Item -LiteralPath $source -Destination $dest -Force " +
            "}";
    }

    private static string BuildRenameCommand(ShellPlanDraft plan)
    {
        string sourceExpression = ResolveDeleteTargetExpression(plan);
        if (string.IsNullOrWhiteSpace(sourceExpression))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(plan.Arguments))
        {
            return string.Empty;
        }

        return
            $"$source = {sourceExpression}; " +
            $"Rename-Item -LiteralPath $source -NewName '{EscapeSingleQuoted(plan.Arguments)}' -Force";
    }

    private static string ResolveDeleteTargetExpression(ShellPlanDraft plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.TargetPath))
        {
            return $"'{EscapeSingleQuoted(plan.TargetPath)}'";
        }

        if (string.IsNullOrWhiteSpace(plan.TargetName))
        {
            return string.Empty;
        }

        string basePathExpression = ResolveLocationBaseExpression(plan.TargetLocation);
        if (string.IsNullOrWhiteSpace(basePathExpression))
        {
            return string.Empty;
        }

        return $"(Join-Path {basePathExpression} '{EscapeSingleQuoted(plan.TargetName)}')";
    }

    private static string ResolveLocationBaseExpression(ShellTargetLocation location)
    {
        return location switch
        {
            ShellTargetLocation.Desktop => "(Join-Path $env:USERPROFILE 'Desktop')",
            ShellTargetLocation.Documents => "(Join-Path $env:USERPROFILE 'Documents')",
            ShellTargetLocation.Downloads => "(Join-Path $env:USERPROFILE 'Downloads')",
            ShellTargetLocation.Pictures => "(Join-Path $env:USERPROFILE 'Pictures')",
            _ => string.Empty
        };
    }

    private static string ResolveDestinationExpression(ShellPlanDraft plan)
    {
        if (!string.IsNullOrWhiteSpace(plan.Arguments))
        {
            string basePathExpression = ResolveLocationBaseExpression(plan.TargetLocation);
            if (!string.IsNullOrWhiteSpace(basePathExpression))
            {
                return $"(Join-Path {basePathExpression} '{EscapeSingleQuoted(plan.Arguments)}')";
            }

            return $"'{EscapeSingleQuoted(plan.Arguments)}'";
        }

        return string.Empty;
    }

    private static string NormalizeLaunchTarget(string targetName)
    {
        string normalized = targetName.Trim();

        return normalized.ToLowerInvariant() switch
        {
            "计算器" or "calc" => "calc",
            "记事本" or "notepad" => "notepad",
            "资源管理器" or "explorer" => "explorer",
            _ => $"'{EscapeSingleQuoted(normalized)}'"
        };
    }

    private static string EscapeSingleQuoted(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
