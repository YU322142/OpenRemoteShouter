using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using RemoteShouter.Models;
using RemoteShouter.Services;
using RemoteShouter.Views;

namespace RemoteShouter;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private ShoutServer? _server;
    private ShoutDisplayService? _displayService;
    private SpeechQueueService? _speechService;
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private Bitmap? _trayMenuIcon;
    private bool _exitRequested;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _speechService = new SpeechQueueService();
            _displayService = new ShoutDisplayService(_speechService);
            _server = new ShoutServer(_displayService);
            _ = _server.StartAsync();

            CreateTrayIcon();

            desktop.Exit += async (_, _) =>
            {
                _trayIcon?.Dispose();
                _trayMenuIcon?.Dispose();
                if (_server is not null)
                {
                    await _server.StopAsync();
                }

                if (_speechService is not null)
                {
                    await _speechService.StopAsync();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon()
    {
        if (_server is null || _displayService is null)
        {
            return;
        }

        var menu = new NativeMenu();
        _trayMenuIcon = new Bitmap(AppIconFactory.CreateStream());
        // The branded first item keeps the logo beside OpenRemoteShouter.
        // Clicking it opens the console; the platform owns right-click menu
        // placement and rendering.
        var openItem = new NativeMenuItem { Header = "OpenRemoteShouter", Icon = _trayMenuIcon };
        openItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(openItem);

        var copyItem = new NativeMenuItem { Header = "复制访问地址" };
        copyItem.Click += async (_, _) => await CopyFirstUrlAsync();
        menu.Items.Add(copyItem);

        var testItem = new NativeMenuItem { Header = "本机测试" };
        testItem.Click += async (_, _) => await ShowTestMessageAsync();
        menu.Items.Add(testItem);
        menu.Items.Add(new NativeMenuItemSeparator());

        var startItem = new NativeMenuItem { Header = "启动网页服务" };
        startItem.Click += async (_, _) => await StartServerFromTrayAsync();
        menu.Items.Add(startItem);

        var stopItem = new NativeMenuItem { Header = "停止网页服务" };
        stopItem.Click += async (_, _) => await StopServerFromTrayAsync();
        menu.Items.Add(stopItem);
        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem { Header = "退出" };
        exitItem.Click += async (_, _) => await ExitFromTrayAsync();
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AppIconFactory.CreateStream()),
            ToolTipText = "OpenRemoteShouter",
            Menu = menu,
            IsVisible = true
        };
        // TrayIcon.Clicked is the primary/left-click action. The context menu
        // is owned by the platform and opens on right-click with native
        // placement, which is required for tray icons.
        _trayIcon.Clicked += (_, _) => ShowMainWindow();
    }

    private async Task StartServerFromTrayAsync()
    {
        if (_server is null || _server.Status.IsRunning)
        {
            return;
        }

        try
        {
            await _server.StartAsync();
            _mainWindow?.RefreshStatus();
        }
        catch (Exception ex)
        {
            AppLogService.Error("Failed to start server from tray", ex);
            _mainWindow?.ShowServiceError(ex.Message);
        }
    }

    private void ShowMainWindow()
    {
        if (_server is null || _displayService is null)
        {
            return;
        }

        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow(_server, _displayService);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }

        _desktop!.MainWindow = _mainWindow;
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private async Task CopyFirstUrlAsync()
    {
        var url = _server?.Status.Urls.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (_mainWindow is not null)
        {
            var clipboard = TopLevel.GetTopLevel(_mainWindow)?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(url);
            }
        }
    }

    private async Task ShowTestMessageAsync()
    {
        if (_displayService is null)
        {
            return;
        }

        await _displayService.ShowAsync(new ShoutMessage(
            "\u672c\u673a\u6d4b\u8bd5",
            "\u8fd9\u662f\u4e00\u6761 OpenRemoteShouter \u6d4b\u8bd5\u6d88\u606f\u3002",
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

    private async Task StopServerFromTrayAsync()
    {
        if (_server is null || !_server.Status.IsRunning)
        {
            return;
        }

        if (!await ConfirmAdministratorAsync("确认停止网页服务"))
        {
            return;
        }

        try
        {
            await _server.StopAsync();
            _mainWindow?.RefreshStatus();
        }
        catch (Exception ex)
        {
            AppLogService.Error("Failed to stop server from tray", ex);
            _mainWindow?.ShowServiceError(ex.Message);
        }
    }

    private async Task ExitFromTrayAsync()
    {
        if (_exitRequested || _server is null)
        {
            return;
        }

        if (!await ConfirmAdministratorAsync("确认退出 OpenRemoteShouter"))
        {
            return;
        }

        _exitRequested = true;
        _desktop?.TryShutdown();
    }

    private async Task<bool> ConfirmAdministratorAsync(string operation)
    {
        if (_server is null)
        {
            return false;
        }

        if (_mainWindow is null)
        {
            ShowMainWindow();
        }

        return await AdminPasswordWindow.ConfirmAsync(_server, _mainWindow, operation);
    }

}
