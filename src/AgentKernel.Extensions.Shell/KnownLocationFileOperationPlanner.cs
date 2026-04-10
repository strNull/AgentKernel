namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 把已知目录下的文件操作请求收成统一的结构化草案。
/// 当前先实现删除操作，后续可扩展复制、移动、重命名。
/// </summary>
public static class KnownLocationFileOperationPlanner
{
    public static bool TryPlanFromModel(
        ShellPlanDraft modelPlan,
        string source,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        if (modelPlan.IntentType != ShellIntentType.FileOperation)
        {
            return false;
        }

        if (modelPlan.OperationType is not (ShellOperationType.Delete
            or ShellOperationType.Copy
            or ShellOperationType.Move
            or ShellOperationType.Rename))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(modelPlan.TargetName))
        {
            return false;
        }

        ShellTargetKind targetKind = ResolveTargetKind(modelPlan);

        if (TryResolveKnownLocation(modelPlan.TargetLocation, out ShellTargetLocation location))
        {
            var request = new KnownLocationTargetRequest
            {
                Location = location,
                TargetKind = targetKind,
                TargetName = modelPlan.TargetName,
                DestinationName = modelPlan.Arguments ?? string.Empty,
                PreferExactMatch = true
            };

            plan = modelPlan.OperationType == ShellOperationType.Delete
                ? BuildDeletePlan(request, source)
                : BuildBinaryPlanFromModel(request, source, modelPlan.OperationType);
            return true;
        }

        if (modelPlan.TargetLocation == ShellTargetLocation.CustomPath &&
            TryResolveCustomBasePath(modelPlan.TargetPath, out string basePath, out string displayName))
        {
            var request = new KnownLocationTargetRequest
            {
                Location = ShellTargetLocation.CustomPath,
                TargetKind = targetKind,
                TargetName = modelPlan.TargetName,
                DestinationName = modelPlan.Arguments ?? string.Empty,
                PreferExactMatch = true
            };

            KnownLocationResolution resolution = KnownLocationItemResolver.ResolveSingleFromBasePath(
                basePath,
                request.TargetName,
                request.TargetKind,
                preferExactMatch: true);

            if (modelPlan.OperationType == ShellOperationType.Delete)
            {
                return BuildDriveDeletePlan(displayName, basePath, request.TargetName, request.TargetKind, resolution, source, out plan);
            }

            if (string.IsNullOrWhiteSpace(request.DestinationName))
            {
                plan = BuildMissingDestinationPlan(modelPlan.OperationType, source);
                return true;
            }

            return BuildDriveBinaryPlan(displayName, basePath, request, resolution, source, modelPlan.OperationType, out plan);
        }

        plan = BuildMissingLocationPlan(modelPlan, source);
        return true;
    }

    public static bool TryPlanDelete(
        string userInstruction,
        string source,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        if (!KnownLocationTargetParser.LooksLikeDeleteRequest(userInstruction))
        {
            return false;
        }

        if (KnownLocationTargetParser.TryExtractKnownLocationDeleteTarget(
            userInstruction,
            out KnownLocationTargetRequest request))
        {
            plan = BuildDeletePlan(request, source);
            return true;
        }

        if (TryPlanDriveDelete(userInstruction, source, out plan))
        {
            return true;
        }

        if (KnownLocationTargetParser.TryExtractKnownLocationDeleteIntent(
            userInstruction,
            out ShellTargetLocation location,
            out ShellTargetKind targetKind))
        {
            plan = BuildMissingQuotePlan(location, targetKind, source);
            return true;
        }

        return false;
    }

    public static bool TryPlanCopyMoveRename(
        string userInstruction,
        string source,
        ShellOperationType operationType,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        if (operationType is not (ShellOperationType.Copy or ShellOperationType.Move or ShellOperationType.Rename))
        {
            return false;
        }

        bool looksLikeOperation = operationType switch
        {
            ShellOperationType.Copy => KnownLocationTargetParser.LooksLikeCopyRequest(userInstruction),
            ShellOperationType.Move => KnownLocationTargetParser.LooksLikeMoveRequest(userInstruction),
            _ => KnownLocationTargetParser.LooksLikeRenameRequest(userInstruction)
        };

        if (!looksLikeOperation)
        {
            return false;
        }

        if (KnownLocationTargetParser.TryExtractKnownLocationBinaryTarget(
            userInstruction,
            out KnownLocationTargetRequest request))
        {
            plan = BuildBinaryOperationPlan(request, source, operationType);
            return true;
        }

        if (TryPlanDriveBinaryOperation(userInstruction, source, operationType, out plan))
        {
            return true;
        }

        if (KnownLocationTargetParser.TryExtractKnownLocationDeleteIntent(
            userInstruction,
            out ShellTargetLocation location,
            out ShellTargetKind targetKind))
        {
            plan = BuildMissingQuoteBinaryPlan(location, targetKind, source, operationType);
            return true;
        }

        return false;
    }

    private static ShellPlanDraft BuildDeletePlan(KnownLocationTargetRequest request, string source)
    {
        string locationName = KnownLocationTargetParser.GetLocationDisplayName(request.Location);
        KnownLocationResolution resolution = KnownLocationItemResolver.ResolveSingle(
            request.Location,
            request.TargetName,
            request.TargetKind,
            request.PreferExactMatch);

        if (resolution.Status == KnownLocationResolutionStatus.Ambiguous)
        {
            string candidates = string.Join("、", resolution.Candidates);
            return new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Delete,
                RiskLevel = ShellRiskLevel.High,
                TargetLocation = request.Location,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我在{locationName}里找到了多个叫“{request.TargetName}”的候选项：{candidates}。请再告诉我更准确的名字。",
                Reason = "删除目标存在多个候选项，需要用户进一步确认。"
            };
        }

        if (resolution.Status == KnownLocationResolutionStatus.NotFound)
        {
            return new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Delete,
                RiskLevel = ShellRiskLevel.High,
                TargetLocation = request.Location,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我先帮你看了一下，{locationName}里暂时没有找到“{request.TargetName}”。你可以再确认一下名称。",
                Reason = "删除目标未在已知目录中找到，当前不执行删除。"
            };
        }

        string directoryScopeNote = BuildDirectoryScopeNote(resolution.ResolvedTargetKind);
        string baseMessage = $"这条操作需要你先确认，我已经把删除{locationName}上的“{resolution.ResolvedName}”整理好了。";

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Delete,
            RiskLevel = ShellRiskLevel.High,
            TargetLocation = request.Location,
            TargetKind = resolution.ResolvedTargetKind,
            TargetName = resolution.ResolvedName,
            TargetPath = resolution.ResolvedPath,
            ShouldExecute = true,
            RequiresConfirmation = true,
            AssistantMessage = string.IsNullOrWhiteSpace(directoryScopeNote)
                ? baseMessage
                : $"{baseMessage}\n\n{directoryScopeNote}",
            Reason = resolution.ResolvedTargetKind == ShellTargetKind.Directory
                ? "识别为高风险文件夹删除请求，确认后会连同文件夹内部内容一起处理。"
                : "识别为高风险文件删除请求。"
        };
    }

    private static bool TryPlanDriveDelete(
        string userInstruction,
        string source,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        if (!KnownLocationTargetParser.LooksLikeDeleteRequest(userInstruction))
        {
            return false;
        }

        if (!KnownLocationTargetParser.TryExtractDriveBasePath(userInstruction, out string basePath, out string displayName))
        {
            return false;
        }

        if (!KnownLocationTargetParser.TryExtractEnglishQuotedTargetName(userInstruction, out string targetName))
        {
            plan = BuildMissingQuoteDrivePlan(displayName, source);
            return true;
        }

        ShellTargetKind targetKind = Path.HasExtension(targetName)
            ? ShellTargetKind.File
            : ShellTargetKind.Unknown;

        KnownLocationResolution resolution = KnownLocationItemResolver.ResolveSingleFromBasePath(
            basePath,
            targetName,
            targetKind,
            preferExactMatch: true);

        return BuildDriveDeletePlan(displayName, basePath, targetName, targetKind, resolution, source, out plan);
    }

    private static bool TryPlanDriveBinaryOperation(
        string userInstruction,
        string source,
        ShellOperationType operationType,
        out ShellPlanDraft plan)
    {
        plan = new ShellPlanDraft();

        bool looksLikeOperation = operationType switch
        {
            ShellOperationType.Copy => KnownLocationTargetParser.LooksLikeCopyRequest(userInstruction),
            ShellOperationType.Move => KnownLocationTargetParser.LooksLikeMoveRequest(userInstruction),
            _ => KnownLocationTargetParser.LooksLikeRenameRequest(userInstruction)
        };

        if (!looksLikeOperation)
        {
            return false;
        }

        if (!KnownLocationTargetParser.TryExtractDriveBasePath(userInstruction, out string basePath, out string displayName))
        {
            return false;
        }

        if (!KnownLocationTargetParser.TryExtractTwoEnglishQuotedTargetNames(
                userInstruction,
                out string sourceName,
                out string destinationName))
        {
            plan = BuildMissingQuoteDriveBinaryPlan(displayName, source, operationType);
            return true;
        }

        var request = new KnownLocationTargetRequest
        {
            Location = ShellTargetLocation.CustomPath,
            TargetKind = Path.HasExtension(sourceName) ? ShellTargetKind.File : ShellTargetKind.Unknown,
            TargetName = sourceName,
            DestinationName = destinationName,
            PreferExactMatch = true
        };
        KnownLocationResolution resolution = KnownLocationItemResolver.ResolveSingleFromBasePath(
            basePath,
            request.TargetName,
            request.TargetKind,
            preferExactMatch: true);

        return BuildDriveBinaryPlan(displayName, basePath, request, resolution, source, operationType, out plan);
    }

    private static bool BuildDriveDeletePlan(
        string displayName,
        string basePath,
        string targetName,
        ShellTargetKind targetKind,
        KnownLocationResolution resolution,
        string source,
        out ShellPlanDraft plan)
    {
        if (resolution.Status == KnownLocationResolutionStatus.Ambiguous)
        {
            string candidates = string.Join("、", resolution.Candidates);
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Delete,
                RiskLevel = ShellRiskLevel.High,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = targetKind,
                TargetName = targetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我在{displayName}里找到了多个叫“{targetName}”的候选项：{candidates}。请再告诉我更准确的名字。",
                Reason = "删除目标存在多个候选项，需要用户进一步确认。"
            };
            return true;
        }

        if (resolution.Status == KnownLocationResolutionStatus.NotFound)
        {
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = ShellOperationType.Delete,
                RiskLevel = ShellRiskLevel.High,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = targetKind,
                TargetName = targetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我先帮你看了一下，{displayName}里暂时没有找到“{targetName}”。你可以再确认一下名称。",
                Reason = "删除目标未在磁盘中找到，当前不执行删除。"
            };
            return true;
        }

        string directoryScopeNote = BuildDirectoryScopeNote(resolution.ResolvedTargetKind);
        string baseMessage = $"这条操作需要你先确认，我已经把删除{displayName}里的“{resolution.ResolvedName}”整理好了。";

        plan = new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Delete,
            RiskLevel = ShellRiskLevel.High,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = resolution.ResolvedTargetKind,
            TargetName = resolution.ResolvedName,
            TargetPath = resolution.ResolvedPath,
            ShouldExecute = true,
            RequiresConfirmation = true,
            AssistantMessage = string.IsNullOrWhiteSpace(directoryScopeNote)
                ? baseMessage
                : $"{baseMessage}\n\n{directoryScopeNote}",
            Reason = resolution.ResolvedTargetKind == ShellTargetKind.Directory
                ? "识别为高风险文件夹删除请求，确认后会连同文件夹内部内容一起处理。"
                : "识别为高风险文件删除请求。"
        };
        return true;
    }

    private static bool BuildDriveBinaryPlan(
        string displayName,
        string basePath,
        KnownLocationTargetRequest request,
        KnownLocationResolution resolution,
        string source,
        ShellOperationType operationType,
        out ShellPlanDraft plan)
    {
        if (resolution.Status == KnownLocationResolutionStatus.Ambiguous)
        {
            string candidates = string.Join("、", resolution.Candidates);
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = operationType,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我在{displayName}里找到了多个叫“{request.TargetName}”的候选项：{candidates}。请再告诉我更准确的名字。",
                Reason = "目标存在多个候选项，需要用户进一步确认。"
            };
            return true;
        }

        if (resolution.Status == KnownLocationResolutionStatus.NotFound)
        {
            plan = new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = operationType,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = ShellTargetLocation.CustomPath,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我先帮你看了一下，{displayName}里暂时没有找到“{request.TargetName}”。你可以再确认一下名称。",
                Reason = "目标未在磁盘中找到，当前不执行操作。"
            };
            return true;
        }

        string directoryScopeNote = BuildDirectoryScopeNote(resolution.ResolvedTargetKind);
        string baseMessage = operationType switch
        {
            ShellOperationType.Copy => $"这条操作需要你先确认，我已经把从{displayName}复制“{resolution.ResolvedName}”到“{request.DestinationName}”整理好了。",
            ShellOperationType.Move => $"这条操作需要你先确认，我已经把从{displayName}移动“{resolution.ResolvedName}”到“{request.DestinationName}”整理好了。",
            _ => $"这条操作需要你先确认，我已经把{displayName}里的“{resolution.ResolvedName}”重命名为“{request.DestinationName}”整理好了。"
        };

        plan = new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = operationType,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = resolution.ResolvedTargetKind,
            TargetName = resolution.ResolvedName,
            TargetPath = resolution.ResolvedPath,
            Arguments = request.DestinationName,
            ShouldExecute = true,
            RequiresConfirmation = true,
            AssistantMessage = string.IsNullOrWhiteSpace(directoryScopeNote)
                ? baseMessage
                : $"{baseMessage}\n\n{directoryScopeNote}",
            Reason = resolution.ResolvedTargetKind == ShellTargetKind.Directory
                ? "识别为文件夹操作请求，确认后会连同文件夹内部内容一起处理。"
                : "识别为文件操作请求，需要确认后执行。"
        };
        return true;
    }

    private static ShellPlanDraft BuildMissingQuoteDrivePlan(string displayName, string source)
    {
        string example = $"删除{displayName}上的\"测试文件.txt\"";
        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Delete,
            RiskLevel = ShellRiskLevel.High,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = ShellTargetKind.Unknown,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage =
                $"为了安全处理文件操作，请把目标名称用英文双引号括起来。例如：{example}。如果最终命中的是文件夹，本次操作会连同文件夹内部内容一起处理。",
            Reason = "文件操作未提供英文双引号包裹的目标名称，当前不执行。"
        };
    }

    private static ShellPlanDraft BuildMissingQuoteDriveBinaryPlan(
        string displayName,
        string source,
        ShellOperationType operationType)
    {
        string example = operationType switch
        {
            ShellOperationType.Copy => $"复制{displayName}的\"源文件.txt\"到\"新文件.txt\"",
            ShellOperationType.Move => $"移动{displayName}的\"源文件.txt\"到\"新文件.txt\"",
            _ => $"把{displayName}的\"旧名字.txt\"改名为\"新名字.txt\""
        };

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = operationType,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = ShellTargetLocation.CustomPath,
            TargetKind = ShellTargetKind.Unknown,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage =
                $"为了安全处理文件操作，请把源名称和目标名称都用英文双引号括起来。例如：{example}。如果最终命中的是文件夹，本次操作会连同文件夹内部内容一起处理。",
            Reason = "文件操作未提供英文双引号包裹的目标名称，当前不执行。"
        };
    }

    private static ShellPlanDraft BuildBinaryOperationPlan(
        KnownLocationTargetRequest request,
        string source,
        ShellOperationType operationType)
    {
        string locationName = KnownLocationTargetParser.GetLocationDisplayName(request.Location);
        KnownLocationResolution resolution = KnownLocationItemResolver.ResolveSingle(
            request.Location,
            request.TargetName,
            request.TargetKind,
            request.PreferExactMatch);

        if (resolution.Status == KnownLocationResolutionStatus.Ambiguous)
        {
            string candidates = string.Join("、", resolution.Candidates);
            return new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = operationType,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = request.Location,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我在{locationName}里找到了多个叫“{request.TargetName}”的候选项：{candidates}。请再告诉我更准确的名字。",
                Reason = "目标存在多个候选项，需要用户进一步确认。"
            };
        }

        if (resolution.Status == KnownLocationResolutionStatus.NotFound)
        {
            return new ShellPlanDraft
            {
                Source = source,
                IntentType = ShellIntentType.FileOperation,
                OperationType = operationType,
                RiskLevel = ShellRiskLevel.Medium,
                TargetLocation = request.Location,
                TargetKind = request.TargetKind,
                TargetName = request.TargetName,
                ShouldExecute = false,
                RequiresConfirmation = false,
                AssistantMessage =
                    $"我先帮你看了一下，{locationName}里暂时没有找到“{request.TargetName}”。你可以再确认一下名称。",
                Reason = "目标未在已知目录中找到，当前不执行操作。"
            };
        }

        if (string.IsNullOrWhiteSpace(request.DestinationName))
        {
            return BuildMissingDestinationPlan(operationType, source);
        }

        string directoryScopeNote = BuildDirectoryScopeNote(resolution.ResolvedTargetKind);
        string baseMessage = operationType switch
        {
            ShellOperationType.Copy => $"这条操作需要你先确认，我已经把从{locationName}复制“{resolution.ResolvedName}”到“{request.DestinationName}”整理好了。",
            ShellOperationType.Move => $"这条操作需要你先确认，我已经把从{locationName}移动“{resolution.ResolvedName}”到“{request.DestinationName}”整理好了。",
            _ => $"这条操作需要你先确认，我已经把{locationName}里的“{resolution.ResolvedName}”重命名为“{request.DestinationName}”整理好了。"
        };

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = operationType,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = request.Location,
            TargetKind = resolution.ResolvedTargetKind,
            TargetName = resolution.ResolvedName,
            TargetPath = resolution.ResolvedPath,
            Arguments = request.DestinationName,
            ShouldExecute = true,
            RequiresConfirmation = true,
            AssistantMessage = string.IsNullOrWhiteSpace(directoryScopeNote)
                ? baseMessage
                : $"{baseMessage}\n\n{directoryScopeNote}",
            Reason = resolution.ResolvedTargetKind == ShellTargetKind.Directory
                ? "识别为文件夹操作请求，确认后会连同文件夹内部内容一起处理。"
                : "识别为文件操作请求，需要确认后执行。"
        };
    }

    private static ShellPlanDraft BuildBinaryPlanFromModel(
        KnownLocationTargetRequest request,
        string source,
        ShellOperationType operationType)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationName) &&
            operationType is ShellOperationType.Copy or ShellOperationType.Move or ShellOperationType.Rename)
        {
            return BuildMissingDestinationPlan(operationType, source);
        }

        return BuildBinaryOperationPlan(request, source, operationType);
    }

    private static ShellPlanDraft BuildMissingDestinationPlan(ShellOperationType operationType, string source)
    {
        string actionName = operationType switch
        {
            ShellOperationType.Copy => "复制",
            ShellOperationType.Move => "移动",
            ShellOperationType.Rename => "重命名",
            _ => "操作"
        };

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = operationType,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = ShellTargetLocation.Unknown,
            TargetKind = ShellTargetKind.Unknown,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage = $"我需要你告诉我{actionName}到的目标名称，才能继续处理。",
            Reason = "缺少目标名称或新名称。"
        };
    }

    private static ShellPlanDraft BuildMissingLocationPlan(ShellPlanDraft modelPlan, string source)
    {
        ShellRiskLevel riskLevel = modelPlan.OperationType == ShellOperationType.Delete
            ? ShellRiskLevel.High
            : ShellRiskLevel.Medium;

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = modelPlan.OperationType,
            RiskLevel = riskLevel,
            TargetLocation = ShellTargetLocation.Unknown,
            TargetKind = modelPlan.TargetKind,
            TargetName = modelPlan.TargetName,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage = "我需要知道目标所在位置，例如桌面、文档、下载、图片，或者给出完整路径。",
            Reason = "目标位置不明确，无法执行文件操作。"
        };
    }

    private static bool TryResolveKnownLocation(
        ShellTargetLocation location,
        out ShellTargetLocation resolved)
    {
        resolved = location;
        return resolved is ShellTargetLocation.Desktop
            or ShellTargetLocation.Documents
            or ShellTargetLocation.Downloads
            or ShellTargetLocation.Pictures;
    }

    private static bool TryResolveCustomBasePath(
        string? targetPath,
        out string basePath,
        out string displayName)
    {
        basePath = string.Empty;
        displayName = string.Empty;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return false;
        }

        string trimmed = targetPath.Trim();
        if (trimmed.Length == 2 && trimmed[1] == ':')
        {
            trimmed = $"{trimmed}\\";
        }

        if (!Directory.Exists(trimmed))
        {
            return false;
        }

        basePath = trimmed;
        displayName = trimmed.Length >= 2 && trimmed[1] == ':'
            ? $"{trimmed[0]}盘"
            : trimmed;
        return true;
    }

    private static ShellTargetKind ResolveTargetKind(ShellPlanDraft plan)
    {
        if (plan.TargetKind != ShellTargetKind.Unknown)
        {
            return plan.TargetKind;
        }

        if (!string.IsNullOrWhiteSpace(plan.TargetName) && Path.HasExtension(plan.TargetName))
        {
            return ShellTargetKind.File;
        }

        return ShellTargetKind.Unknown;
    }

    private static ShellPlanDraft BuildMissingQuotePlan(
        ShellTargetLocation location,
        ShellTargetKind targetKind,
        string source)
    {
        string locationName = KnownLocationTargetParser.GetLocationDisplayName(location);
        string example = KnownLocationTargetParser.BuildQuotedTargetExample(location, targetKind);
        string directoryScopeNote = targetKind == ShellTargetKind.Directory
            ? "\n\n如果最终命中的是文件夹，本次操作会连同文件夹内部内容一起处理。"
            : "\n\n如果最终命中的是文件夹，本次操作会连同文件夹内部内容一起处理。";

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = ShellOperationType.Delete,
            RiskLevel = ShellRiskLevel.High,
            TargetLocation = location,
            TargetKind = targetKind,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage =
                $"为了安全处理文件操作，请把目标名称用英文双引号括起来再告诉我。例如：{example}。{directoryScopeNote}",
            Reason = $"识别为{locationName}中的文件操作请求，但当前缺少英文双引号包裹的目标名称。"
        };
    }

    private static ShellPlanDraft BuildMissingQuoteBinaryPlan(
        ShellTargetLocation location,
        ShellTargetKind targetKind,
        string source,
        ShellOperationType operationType)
    {
        string locationName = KnownLocationTargetParser.GetLocationDisplayName(location);
        string example = operationType switch
        {
            ShellOperationType.Copy => $"复制{locationName}里的\"源文件.txt\"到\"新文件.txt\"",
            ShellOperationType.Move => $"移动{locationName}里的\"源文件.txt\"到\"新文件.txt\"",
            _ => $"把{locationName}里的\"旧名字.txt\"改名为\"新名字.txt\""
        };
        string directoryScopeNote = "\n\n如果最终命中的是文件夹，本次操作会连同文件夹内部内容一起处理。";

        return new ShellPlanDraft
        {
            Source = source,
            IntentType = ShellIntentType.FileOperation,
            OperationType = operationType,
            RiskLevel = ShellRiskLevel.Medium,
            TargetLocation = location,
            TargetKind = targetKind,
            ShouldExecute = false,
            RequiresConfirmation = false,
            AssistantMessage =
                $"为了安全处理文件操作，请把源名称和目标名称都用英文双引号括起来。例如：{example}。{directoryScopeNote}",
            Reason = $"识别为{locationName}中的文件操作请求，但当前缺少英文双引号包裹的目标名称。"
        };
    }

    private static string BuildDirectoryScopeNote(ShellTargetKind targetKind)
    {
        return targetKind == ShellTargetKind.Directory
            ? "注意：当前目标是文件夹，确认后会连同文件夹内部内容一起处理。"
            : string.Empty;
    }
}
