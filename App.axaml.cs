using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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
        var openItem = new NativeMenuItem { Header = "\u6253\u5f00\u63a7\u5236\u53f0" };
        openItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(openItem);

        var copyItem = new NativeMenuItem { Header = "\u590d\u5236\u8bbf\u95ee\u5730\u5740" };
        copyItem.Click += async (_, _) => await CopyFirstUrlAsync();
        menu.Items.Add(copyItem);

        var testItem = new NativeMenuItem { Header = "\u672c\u673a\u6d4b\u8bd5" };
        testItem.Click += async (_, _) => await ShowTestMessageAsync();
        menu.Items.Add(testItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var startItem = new NativeMenuItem { Header = "\u542f\u52a8\u7f51\u9875\u670d\u52a1" };
        startItem.Click += async (_, _) => await _server.StartAsync();
        menu.Items.Add(startItem);

        var stopItem = new NativeMenuItem { Header = "\u505c\u6b62\u7f51\u9875\u670d\u52a1" };
        stopItem.Click += async (_, _) => await _server.StopAsync();
        menu.Items.Add(stopItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem { Header = "\u9000\u51fa" };
        exitItem.Click += (_, _) => _desktop?.TryShutdown();
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(CreateTrayIconStream()),
            ToolTipText = "OpenRemoteShouter",
            Menu = menu,
            IsVisible = true
        };
        _trayIcon.Clicked += (_, _) => ShowMainWindow();
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
            ShoutDisplayMode.Popup,
            10,
            true,
            true,
            ShoutRequest.DefaultVoiceName,
            0,
            1.0f,
            ShoutRequest.DefaultTheme,
            DateTimeOffset.Now));
    }

    private static Stream CreateTrayIconStream()
    {
        const int size = 16;
        const int xorBytes = size * size * 4;
        const int andBytes = size * 4;
        const int imageBytes = 40 + xorBytes + andBytes;
        const int imageOffset = 22;

        var stream = new MemoryStream(imageOffset + imageBytes);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write((byte)size);
        writer.Write((byte)size);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(imageBytes);
        writer.Write(imageOffset);

        writer.Write(40);
        writer.Write(size);
        writer.Write(size * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(xorBytes);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (var y = size - 1; y >= 0; y--)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - 7.5;
                var dy = y - 7.5;
                var inside = dx * dx + dy * dy <= 56;
                writer.Write((byte)0x90);
                writer.Write((byte)0x74);
                writer.Write((byte)0x0E);
                writer.Write((byte)(inside ? 0xFF : 0x00));
            }
        }

        writer.Write(new byte[andBytes]);
        stream.Position = 0;
        return stream;
    }
}
