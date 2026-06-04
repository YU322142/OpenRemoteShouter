using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RemoteShouter.Models;
using RemoteShouter.Services;

namespace RemoteShouter.Views;

public partial class ShoutWindow : Window
{
    private readonly ShoutMessage _message;
    private readonly DispatcherTimer _countdownTimer;
    private readonly DispatcherTimer _topmostReassertTimer;
    private int _remainingSeconds;

    public ShoutWindow()
        : this(new ShoutMessage(
            "OpenRemoteShouter",
            "Preview message",
            ShoutDisplayMode.Popup,
            10,
            true,
            false,
            ShoutRequest.DefaultVoiceName,
            0,
            1.0f,
            ShoutRequest.DefaultTheme,
            DateTimeOffset.Now))
    {
    }

    public ShoutWindow(ShoutMessage message)
    {
        InitializeComponent();
        _message = message;
        _remainingSeconds = message.DurationSeconds;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTimer_OnTick;

        _topmostReassertTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _topmostReassertTimer.Tick += (_, _) => PlatformTopmostService.Reassert(this);

        ConfigureWindow();
        ApplyTheme();
        ApplyMessage();

        Opened += ShoutWindow_OnOpened;
        Closed += ShoutWindow_OnClosed;
        KeyDown += ShoutWindow_OnKeyDown;
        SizeChanged += (_, _) => UpdateMessageLayout();
    }

    public bool ShouldStopSpeechOnClose { get; set; } = true;

    private void ConfigureWindow()
    {
        Topmost = _message.Topmost;

        if (_message.Mode == ShoutDisplayMode.Fullscreen)
        {
            SystemDecorations = SystemDecorations.None;
            CanResize = false;
            WindowState = WindowState.FullScreen;
            ShowInTaskbar = false;
            TitleText.FontSize = 30;
            CountdownText.FontSize = 18;
            MessageFrame.Padding = new Avalonia.Thickness(48);
        }
        else
        {
            SystemDecorations = SystemDecorations.Full;
            CanResize = true;
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
            TitleText.FontSize = 22;
            CountdownText.FontSize = 15;
            MessageFrame.Padding = new Avalonia.Thickness(34);
        }
    }

    private void UpdateMessageLayout()
    {
        var padding = MessageFrame.Padding;
        var frameWidth = MessageFrame.Bounds.Width > 0 ? MessageFrame.Bounds.Width : Math.Max(0, ClientSize.Width - 68);
        var frameHeight = MessageFrame.Bounds.Height > 0 ? MessageFrame.Bounds.Height : Math.Max(0, ClientSize.Height - HeaderBorder.Bounds.Height - 68);
        var availableWidth = Math.Max(240, frameWidth - padding.Left - padding.Right);
        var availableHeight = Math.Max(160, frameHeight - padding.Top - padding.Bottom);
        var isFullscreen = _message.Mode == ShoutDisplayMode.Fullscreen;
        var textWidth = Math.Min(availableWidth, isFullscreen ? 1500 : 720);
        var fontSize = CalculateMessageFontSize(_message.Message, textWidth, availableHeight, isFullscreen);

        MessageViewport.MinHeight = availableHeight;
        MessageText.Width = textWidth;
        MessageText.MaxWidth = textWidth;
        MessageText.FontSize = fontSize;
        MessageText.LineHeight = Math.Ceiling(fontSize * 1.32);
    }

    private void ApplyTheme()
    {
        var palette = ThemePalette.For(_message.Theme);
        Background = palette.Page;
        RootGrid.Background = palette.Page;
        HeaderBorder.Background = palette.Header;
        TitleText.Foreground = palette.HeaderText;
        CountdownText.Foreground = palette.HeaderSubtleText;
        MessageFrame.Background = palette.Card;
        MessageFrame.BorderBrush = palette.Border;
        MessageFrame.BorderThickness = new Avalonia.Thickness(1);
        MessageText.Foreground = palette.Text;
    }

    private void ApplyMessage()
    {
        Title = _message.Title;
        TitleText.Text = _message.Title;
        MessageText.Text = _message.Message;
        Dispatcher.UIThread.Post(UpdateMessageLayout, DispatcherPriority.Loaded);
        UpdateCountdownText();
    }

    private void ShoutWindow_OnOpened(object? sender, EventArgs e)
    {
        PlatformTopmostService.Apply(this, _message.Topmost);
        Dispatcher.UIThread.Post(UpdateMessageLayout, DispatcherPriority.Loaded);

        if (_message.Topmost && OperatingSystem.IsWindows())
        {
            _topmostReassertTimer.Start();
        }

        if (_message.DurationSeconds > 0)
        {
            _countdownTimer.Start();
        }
    }

    private void ShoutWindow_OnClosed(object? sender, EventArgs e)
    {
        _countdownTimer.Stop();
        _topmostReassertTimer.Stop();
    }

    private void CountdownTimer_OnTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            ShouldStopSpeechOnClose = false;
            Close();
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        CountdownText.Text = _message.DurationSeconds <= 0
            ? "\u624b\u52a8\u5173\u95ed"
            : $"{Math.Max(_remainingSeconds, 0)} \u79d2\u540e\u5173\u95ed";
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ShouldStopSpeechOnClose = true;
        Close();
    }

    private void ShoutWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ShouldStopSpeechOnClose = true;
            Close();
        }
    }

    private static double CalculateMessageFontSize(
        string text,
        double width,
        double height,
        bool isFullscreen)
    {
        var min = isFullscreen ? 24.0 : 18.0;
        var max = isFullscreen ? 96.0 : 48.0;
        var low = min;
        var high = max;

        for (var i = 0; i < 14; i++)
        {
            var mid = (low + high) / 2;
            var requiredHeight = EstimateTextHeight(text, mid, width);
            if (requiredHeight <= height * 0.92)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return Math.Round(low);
    }

    private static double EstimateTextHeight(string text, double fontSize, double width)
    {
        var lineHeight = Math.Ceiling(fontSize * 1.32);
        var unitsPerLine = Math.Max(1, width / (fontSize * 0.92));
        var totalLines = 0.0;

        foreach (var paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var units = paragraph.Sum(GetCharWidthUnit);
            totalLines += Math.Max(1, Math.Ceiling(units / unitsPerLine));
        }

        return totalLines * lineHeight;
    }

    private static double GetCharWidthUnit(char ch)
    {
        if (char.IsWhiteSpace(ch))
        {
            return 0.45;
        }

        return ch <= 0x007f ? 0.56 : 1.0;
    }

    private sealed record ThemePalette(
        IBrush Page,
        IBrush Header,
        IBrush HeaderText,
        IBrush HeaderSubtleText,
        IBrush Card,
        IBrush Border,
        IBrush Text)
    {
        public static ThemePalette For(string theme)
        {
            return theme switch
            {
                "blue" => Create("#EFF6FF", "#2563EB", "#FFFFFF", "#DBEAFE", "#FFFFFF", "#BFDBFE", "#111827"),
                "green" => Create("#F0FDF4", "#15803D", "#FFFFFF", "#DCFCE7", "#FFFFFF", "#BBF7D0", "#111827"),
                "amber" => Create("#FFFBEB", "#B45309", "#FFFFFF", "#FEF3C7", "#FFFFFF", "#FDE68A", "#111827"),
                "rose" => Create("#FFF1F2", "#BE123C", "#FFFFFF", "#FFE4E6", "#FFFFFF", "#FECDD3", "#111827"),
                "violet" => Create("#F5F3FF", "#6D28D9", "#FFFFFF", "#EDE9FE", "#FFFFFF", "#DDD6FE", "#111827"),
                _ => Create("#ECFEFF", "#0E7490", "#FFFFFF", "#CFFAFE", "#FFFFFF", "#A5F3FC", "#111827")
            };
        }

        private static ThemePalette Create(
            string page,
            string header,
            string headerText,
            string headerSubtleText,
            string card,
            string border,
            string text)
        {
            return new ThemePalette(
                Brush(page),
                Brush(header),
                Brush(headerText),
                Brush(headerSubtleText),
                Brush(card),
                Brush(border),
                Brush(text));
        }

        private static SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush(Color.Parse(color));
        }
    }
}
