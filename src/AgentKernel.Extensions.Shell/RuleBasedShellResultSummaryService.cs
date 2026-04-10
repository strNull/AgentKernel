namespace AgentKernel.Extensions.Shell;

/// <summary>
/// 基于规则的 Shell 结果总结服务。
/// </summary>
public class RuleBasedShellResultSummaryService : IShellResultSummaryService
{
    public Task<ShellResultSummaryResult> SummarizeAsync(
        string userInstruction,
        string command,
        string stdout,
        string stderr,
        bool success,
        CancellationToken cancellationToken = default)
    {
        if (!success)
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "唔，这次执行没有成功，我把错误信息放在下面给你看啦。",
                Source = "fallback"
            });
        }

        string text = userInstruction?.Trim() ?? string.Empty;

        if (text.Contains("powershell版本", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ps版本", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "好啦，我已经帮你查到当前环境的 PowerShell 版本啦。",
                Source = "fallback"
            });
        }

        if (text.Contains("当前目录", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "我已经帮你把当前目录里的内容列出来啦，你可以看看下面的结果。",
                Source = "fallback"
            });
        }

        if (text.Contains("桌面文件", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "桌面里的文件我已经帮你看到了，结果都放在下面啦。",
                Source = "fallback"
            });
        }

        if (text.Contains("文档文件", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "文档文件夹里的内容我已经帮你列出来啦。",
                Source = "fallback"
            });
        }

        if (text.Contains("下载文件", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "下载文件夹里的内容我已经帮你看好了。",
                Source = "fallback"
            });
        }

        if (text.Contains("图片文件", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "图片文件夹里的内容我已经帮你整理在下面啦。",
                Source = "fallback"
            });
        }

        if (text.Contains("计算器", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("calc", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "好呀，我已经帮你把计算器打开啦。",
                Source = "fallback"
            });
        }

        if (text.Contains("记事本", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("notepad", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ShellResultSummaryResult
            {
                SummaryText = "好呀，我已经帮你把记事本打开啦。",
                Source = "fallback"
            });
        }

        return Task.FromResult(new ShellResultSummaryResult
        {
            SummaryText = "好啦，这个操作已经执行完成，我把结果整理在下面给你看。",
            Source = "fallback"
        });
    }
}
