using AgentKernel.Abstractions;
using AgentKernel.Extensions.Chat;
using AgentKernel.Extensions.Shell;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AgentKernel.Host.Wpf;

public partial class MainWindow : Window
{
    private const double ChatInputMinHeight = 24;
    private const double ChatInputMaxHeight = 120;

    private readonly ITaskAgentService _agentService;

    public MainWindow()
    {
        InitializeComponent();

        _agentService = AgentKernelBootstrapper.BuildAgent();

        Loaded += MainWindow_Loaded;
        SendButton.Click += SendButton_Click;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        FriendNameTextBlock.Text = "小智";
        FriendStatusTextBlock.Text = "能聊天，也能帮你执行任务";
        CurrentFriendNameTextBlock.Text = "小智";
        CurrentFriendStatusTextBlock.Text = "在线 · 支持聊天与工具执行";
        ChatHeaderNameTextBlock.Text = "小智";
        ChatHeaderSubtitleTextBlock.Text = "像朋友一样聊天，也能在需要时帮你执行任务。";

        CurrentTaskTextBlock.Text = "-";
        CurrentStatusTextBlock.Text = "状态：待命";
        DebugLogTextBox.Text = string.Empty;

        MessagesPanel.Children.Clear();

        AddAssistantMessage(
            "你好呀，我是小智。你可以直接和我聊天，也可以让我帮你执行一些受控命令，比如查看 PowerShell 版本、列出当前目录、查看桌面文件，或者打开计算器。");

        AdjustChatInputHeight();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        string userInput = ChatInputTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return;
        }

        ChatInputTextBox.Clear();
        AdjustChatInputHeight();

        await RunAgentTurnAsync(userInput, showAsUser: true);
    }

    private async Task RunAgentTurnAsync(string userInput, bool showAsUser)
    {
        try
        {
            SendButton.IsEnabled = false;

            if (showAsUser)
            {
                AddUserMessage(userInput);
            }

            CurrentTaskTextBlock.Text = "planning";
            CurrentStatusTextBlock.Text = "状态：运行中";
            DebugLogTextBox.Text = string.Empty;

            TaskExecutionContext context = await _agentService.RunAsync(userInput);

            string summary = context.Outputs.TryGetValue(ShellMemoryKeys.PowershellSummary, out object? summaryValue)
                ? summaryValue?.ToString() ?? string.Empty
                : string.Empty;

            string stdout = context.WorkingMemory.TryGetValue(ShellMemoryKeys.PowershellStdout, out object? stdoutValue)
                ? stdoutValue?.ToString() ?? string.Empty
                : string.Empty;

            string stderr = context.WorkingMemory.TryGetValue(ShellMemoryKeys.PowershellStderr, out object? stderrValue)
                ? stderrValue?.ToString() ?? string.Empty
                : string.Empty;

            string command = context.WorkingMemory.TryGetValue(ShellMemoryKeys.PowershellCommand, out object? commandValue)
                ? commandValue?.ToString() ?? string.Empty
                : string.Empty;

            string chatReply = context.Outputs.TryGetValue(ChatMemoryKeys.ChatReplyText, out object? chatReplyValue)
                ? chatReplyValue?.ToString() ?? string.Empty
                : string.Empty;

            string chatReplySource = context.Outputs.TryGetValue(ChatMemoryKeys.ChatReplySource, out object? chatReplySourceValue)
                ? chatReplySourceValue?.ToString() ?? string.Empty
                : string.Empty;

            string assistantMessage = context.Outputs.TryGetValue(ShellMemoryKeys.PowershellAssistantMessage, out object? assistantMessageValue)
                ? assistantMessageValue?.ToString() ?? string.Empty
                : string.Empty;

            string shellPlanningSource = context.Outputs.TryGetValue(ShellMemoryKeys.PowershellPlanningSource, out object? shellPlanningSourceValue)
                ? shellPlanningSourceValue?.ToString() ?? string.Empty
                : string.Empty;

            string shellSummarySource = context.Outputs.TryGetValue(ShellMemoryKeys.PowershellSummarySource, out object? shellSummarySourceValue)
                ? shellSummarySourceValue?.ToString() ?? string.Empty
                : string.Empty;

            string reply = BuildAssistantReply(context, chatReply, assistantMessage, summary, stdout, stderr);
            string toolResult = BuildToolResult(command, stdout, stderr, context.TaskDefinition?.TaskType);
            bool showConfirmationActions = string.Equals(
                context.TaskDefinition?.TaskType,
                "confirmation_required",
                StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.CurrentStep, "awaiting_confirmation", StringComparison.OrdinalIgnoreCase);

            AddAssistantMessage(reply, toolResult, showConfirmationActions);

            CurrentTaskTextBlock.Text = context.CurrentStep;
            CurrentStatusTextBlock.Text = context.IsFailed
                ? "状态：失败"
                : context.IsCompleted
                    ? "状态：完成"
                    : context.IsCancelled
                        ? "状态：取消"
                        : "状态：运行中";

            DebugLogTextBox.Text = string.Join(Environment.NewLine, context.ExecutionLogs);

            if (!string.IsNullOrWhiteSpace(chatReplySource))
            {
                DebugLogTextBox.Text += $"{Environment.NewLine}聊天回复来源：{chatReplySource}";
            }

            if (!string.IsNullOrWhiteSpace(shellPlanningSource))
            {
                DebugLogTextBox.Text += $"{Environment.NewLine}Shell 规划来源：{shellPlanningSource}";
            }

            if (!string.IsNullOrWhiteSpace(shellSummarySource))
            {
                DebugLogTextBox.Text += $"{Environment.NewLine}Shell 总结来源：{shellSummarySource}";
            }
        }
        catch (Exception ex)
        {
            AddAssistantMessage("这次处理时出现了异常。", ex.Message);

            CurrentTaskTextBlock.Text = "error";
            CurrentStatusTextBlock.Text = "状态：异常";
            DebugLogTextBox.Text = ex.ToString();
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }

    private void AddUserMessage(string text)
    {
        var messageBorder = new Border
        {
            MaxWidth = 560,
            Margin = new Thickness(140, 0, 0, 18),
            Padding = new Thickness(16, 14, 16, 14),
            Background = new SolidColorBrush(Color.FromRgb(63, 118, 240)),
            CornerRadius = new CornerRadius(22),
            HorizontalAlignment = HorizontalAlignment.Right,
            ContextMenu = BuildCopyContextMenu(text)
        };

        var textBox = CreateMessageTextBox(text, Brushes.White);
        messageBorder.Child = textBox;
        MessagesPanel.Children.Add(messageBorder);

        ScrollChatToBottom();
    }

    private void AddAssistantMessage(
        string replyText,
        string? toolResult = null,
        bool showConfirmationActions = false)
    {
        string copyText = string.IsNullOrWhiteSpace(toolResult)
            ? replyText
            : $"{replyText}{Environment.NewLine}{Environment.NewLine}{toolResult}";

        var container = new Border
        {
            MaxWidth = 780,
            Margin = new Thickness(0, 0, 80, 18),
            Padding = new Thickness(18),
            Background = new SolidColorBrush(Color.FromRgb(18, 29, 51)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(49, 91, 146)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            HorizontalAlignment = HorizontalAlignment.Left,
            ContextMenu = BuildCopyContextMenu(copyText)
        };

        var stack = new StackPanel();

        stack.Children.Add(new TextBlock
        {
            Text = "小智",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(39, 211, 195))
        });

        stack.Children.Add(CreateMessageTextBox(replyText, new SolidColorBrush(Color.FromRgb(236, 244, 255))));

        if (showConfirmationActions)
        {
            stack.Children.Add(BuildConfirmationActionPanel());
        }

        if (!string.IsNullOrWhiteSpace(toolResult))
        {
            var toolCard = new Border
            {
                Margin = new Thickness(0, 14, 0, 0),
                Padding = new Thickness(14),
                Background = new SolidColorBrush(Color.FromRgb(10, 20, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(35, 64, 99)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16)
            };

            var toolStack = new StackPanel();

            toolStack.Children.Add(new TextBlock
            {
                Text = "工具结果",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(142, 164, 198))
            });

            var toolTextBox = CreateMessageTextBox(toolResult, new SolidColorBrush(Color.FromRgb(236, 244, 255)));
            toolTextBox.FontFamily = new FontFamily("Consolas");
            toolTextBox.FontSize = 13;
            toolTextBox.Margin = new Thickness(0, 8, 0, 0);
            toolStack.Children.Add(toolTextBox);

            toolCard.Child = toolStack;
            stack.Children.Add(toolCard);
        }

        container.Child = stack;
        MessagesPanel.Children.Add(container);

        ScrollChatToBottom();
    }

    private FrameworkElement BuildConfirmationActionPanel()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "风险操作确认",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(142, 164, 198))
        });

        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 0),
            Text = "确认后我才会继续执行；如果你改变主意，也可以直接取消。",
            FontSize = 12.5,
            Foreground = new SolidColorBrush(Color.FromRgb(193, 208, 232)),
            TextWrapping = TextWrapping.Wrap
        });

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var confirmButton = new Button
        {
            Content = "确认执行",
            Style = (Style)FindResource("ConfirmationPrimaryButtonStyle")
        };

        var cancelButton = new Button
        {
            Content = "取消",
            Style = (Style)FindResource("ConfirmationSecondaryButtonStyle")
        };

        void DisableActions()
        {
            confirmButton.IsEnabled = false;
            cancelButton.IsEnabled = false;
        }

        confirmButton.Click += async (_, _) =>
        {
            DisableActions();
            await RunAgentTurnAsync("确认", showAsUser: false);
        };

        cancelButton.Click += async (_, _) =>
        {
            DisableActions();
            await RunAgentTurnAsync("取消", showAsUser: false);
        };

        actions.Children.Add(confirmButton);
        actions.Children.Add(cancelButton);
        panel.Children.Add(actions);

        return panel;
    }

    private static TextBox CreateMessageTextBox(string text, Brush foreground)
    {
        return new TextBox
        {
            Margin = new Thickness(0, 10, 0, 0),
            Text = text,
            FontSize = 15,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private static ContextMenu BuildCopyContextMenu(string text)
    {
        var menu = new ContextMenu();
        var item = new MenuItem
        {
            Header = "复制消息"
        };

        item.Click += (_, _) => Clipboard.SetText(text ?? string.Empty);
        menu.Items.Add(item);
        return menu;
    }

    private void ScrollChatToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    private void AdjustChatInputHeight()
    {
        ChatInputTextBox.UpdateLayout();

        double targetHeight = ChatInputTextBox.ExtentHeight + 6;
        if (double.IsNaN(targetHeight) || double.IsInfinity(targetHeight))
        {
            targetHeight = ChatInputMinHeight;
        }

        targetHeight = Math.Max(ChatInputMinHeight, Math.Min(ChatInputMaxHeight, targetHeight));
        ChatInputTextBox.Height = targetHeight;
    }

    private static string BuildAssistantReply(
        TaskExecutionContext context,
        string chatReply,
        string assistantMessage,
        string summary,
        string stdout,
        string stderr)
    {
        if (context.IsFailed)
        {
            return $"执行失败：{context.ErrorMessage}";
        }

        if (string.Equals(context.TaskDefinition?.TaskType, "confirmation_required", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(chatReply))
            {
                return chatReply;
            }

            if (!string.IsNullOrWhiteSpace(assistantMessage))
            {
                return assistantMessage;
            }

            return "这条操作需要你先确认，我已经把拟执行命令和风险提示整理好了。";
        }

        if (!string.IsNullOrWhiteSpace(chatReply))
        {
            return chatReply;
        }

        if (!string.IsNullOrWhiteSpace(assistantMessage) &&
            !string.IsNullOrWhiteSpace(summary))
        {
            return $"{assistantMessage}\n\n{summary}";
        }

        if (!string.IsNullOrWhiteSpace(assistantMessage))
        {
            return assistantMessage;
        }

        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary;
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            return "命令已经执行，但返回了错误信息。";
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            return "命令已经执行完成，我把结果放在下面了。";
        }

        return "这次任务已经处理完成。";
    }

    private static string BuildToolResult(string command, string stdout, string stderr, string? taskType)
    {
        if (string.Equals(taskType, "confirmation_required", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string cleanStdout = NormalizeMultilineText(stdout);
        string cleanStderr = NormalizeMultilineText(stderr);

        if (!string.IsNullOrWhiteSpace(cleanStdout))
        {
            return cleanStdout;
        }

        if (!string.IsNullOrWhiteSpace(cleanStderr))
        {
            return cleanStderr;
        }

        if (!string.IsNullOrWhiteSpace(command))
        {
            return $"已执行命令：{command}";
        }

        return string.Empty;
    }

    private static string NormalizeMultilineText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Replace("\r", string.Empty).Trim();
    }

    private void ChatInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AdjustChatInputHeight();
    }

    private void ChatInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            return;
        }

        e.Handled = true;
        SendButton_Click(SendButton, new RoutedEventArgs(Button.ClickEvent, SendButton));
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
