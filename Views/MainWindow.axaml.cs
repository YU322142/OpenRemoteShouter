using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RemoteShouter.Models;
using RemoteShouter.Services;

namespace RemoteShouter.Views;

public partial class MainWindow : Window
{
    private readonly ShoutServer _server;
    private readonly ShoutDisplayService _displayService;
    private readonly Bitmap _appLogo;
    private bool _serverActionRunning;

    public MainWindow()
        : this(CreateDesignServer(out var displayService), displayService)
    {
    }

    public MainWindow(ShoutServer server, ShoutDisplayService displayService)
    {
        InitializeComponent();
        Icon = new WindowIcon(AppIconFactory.CreateStream());
        _appLogo = new Bitmap(AppIconFactory.CreateStream());
        AppLogo.Source = _appLogo;
        _server = server;
        _displayService = displayService;
        _displayService.CurrentMessageChanged += DisplayService_OnCurrentMessageChanged;

        Opened += MainWindow_OnOpened;
        Closed += MainWindow_OnClosed;
        RefreshStatus();
    }

    private async void MainWindow_OnOpened(object? sender, EventArgs e)
    {
        await StartServerAsync();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _displayService.CurrentMessageChanged -= DisplayService_OnCurrentMessageChanged;
        _appLogo.Dispose();
    }

    private void DisplayService_OnCurrentMessageChanged(object? sender, ShoutMessage? message)
    {
        CurrentMessageText.Text = message is null
            ? "\u5f53\u524d\u6ca1\u6709\u6b63\u5728\u663e\u793a\u7684\u558a\u8bdd\u3002"
            : $"\u6b63\u5728\u663e\u793a\uff1a{message.Title}";
    }

    private async void StartButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await StartServerAsync();
    }

    private async void StopButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!await AdminPasswordWindow.ConfirmAsync(_server, this, "确认停止网页服务"))
        {
            return;
        }

        await RunServerActionAsync(async () =>
        {
            await _server.StopAsync();
            RefreshStatus();
        });
    }

    private async void ExitButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!await AdminPasswordWindow.ConfirmAsync(_server, this, "确认退出 OpenRemoteShouter"))
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
    }

    private async void CloseDisplayButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _displayService.CloseAsync();
    }

    private async void TestButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await _displayService.ShowAsync(new ShoutMessage(
            "\u672c\u673a\u6d4b\u8bd5",
            "\u8fd9\u662f\u4e00\u6761 OpenRemoteShouter \u6d4b\u8bd5\u6d88\u606f\u3002\n\u5982\u679c\u4f60\u80fd\u770b\u5230\u8fd9\u4e2a\u7a97\u53e3\uff0c\u663e\u793a\u94fe\u8def\u5de5\u4f5c\u6b63\u5e38\u3002",
            ShoutDisplayMode.Fullscreen,
            10,
            true,
            true,
            ShoutRequest.DefaultVoiceName,
            0,
            1.0f,
            ShoutRequest.DefaultTheme,
            DateTimeOffset.Now));
    }

    private async void CopyButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var text = UrlsListBox.SelectedItem as string
                   ?? UrlsListBox.ItemsSource?.Cast<string>().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
            StatusText.Text = "\u5df2\u590d\u5236";
        }
    }

    private async Task StartServerAsync()
    {
        await RunServerActionAsync(async () =>
        {
            await _server.StartAsync();
            RefreshStatus();
        });
    }

    private async Task RunServerActionAsync(Func<Task> action)
    {
        if (_serverActionRunning)
        {
            return;
        }

        _serverActionRunning = true;
        SetButtonsEnabled(false);

        try
        {
            SetError(null);
            await action();
        }
        catch (Exception ex)
        {
            SetError($"\u670d\u52a1\u64cd\u4f5c\u5931\u8D25\uff1a{ex.Message}");
            RefreshStatus();
        }
        finally
        {
            _serverActionRunning = false;
            SetButtonsEnabled(true);
        }
    }

    public void RefreshStatus()
    {
        var status = _server.Status;
        StatusText.Text = status.IsRunning ? "\u8fd0\u884c\u4e2d" : "\u5df2\u505c\u6b62";
        StatusText.Foreground = status.IsRunning
            ? new SolidColorBrush(Color.Parse("#107C10"))
            : new SolidColorBrush(Color.Parse("#B10E1C"));
        StatusSurface.Background = status.IsRunning
            ? new SolidColorBrush(Color.Parse("#F1FAF1"))
            : new SolidColorBrush(Color.Parse("#FDE7E9"));
        StatusIndicator.Fill = status.IsRunning
            ? new SolidColorBrush(Color.Parse("#107C10"))
            : new SolidColorBrush(Color.Parse("#B10E1C"));
        UrlsListBox.ItemsSource = status.Urls;
        StartButton.IsEnabled = !status.IsRunning;
        StopButton.IsEnabled = status.IsRunning;
        CopyButton.IsEnabled = status.Urls.Count > 0;

        var statusError = status.Error ?? status.SpeechError;
        if (!string.IsNullOrWhiteSpace(statusError))
        {
            SetError($"\u670d\u52a1\u72b6\u6001\uff1a{statusError}\nLog: {status.LogFilePath}");
        }
        else
        {
            SetError(null);
        }
    }

    public void ShowServiceError(string message)
    {
        SetError($"服务操作失败：{message}");
    }

    private void SetError(string? message)
    {
        var hasError = !string.IsNullOrWhiteSpace(message);
        ErrorText.Text = message ?? string.Empty;
        ErrorInfoBar.IsVisible = hasError;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        StartButton.IsEnabled = enabled;
        StopButton.IsEnabled = enabled;
        CopyButton.IsEnabled = enabled;
        TestButton.IsEnabled = enabled;
        CloseDisplayButton.IsEnabled = enabled;
        ExitButton.IsEnabled = enabled;
    }

    private static ShoutServer CreateDesignServer(out ShoutDisplayService displayService)
    {
        displayService = new ShoutDisplayService(new SpeechQueueService());
        return new ShoutServer(displayService);
    }
}
