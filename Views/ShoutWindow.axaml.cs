using Avalonia;
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
    private readonly DispatcherTimer _fullscreenReassertTimer;
    private readonly DispatcherTimer _gradientTimer;
    private readonly TranslateTransform _titleTransform = new();
    private readonly TranslateTransform _messageTransform = new();
    private LinearGradientBrush? _accentGradient;
    private Color _accentStart;
    private Color _accentMiddle;
    private Color _accentEnd;
    private double _gradientPhase;
    private int _remainingSeconds;
    private bool _closed;
    private bool _opened;
    private bool _durationReady;
    private int _fullscreenReassertTicks;

    public ShoutWindow()
        : this(new ShoutMessage(
            "OpenRemoteShouter",
            "Preview message",
            ShoutDisplayMode.Fullscreen,
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
        TitleText.RenderTransform = _titleTransform;
        MessageText.RenderTransform = _messageTransform;
        _message = message;
        _remainingSeconds = Math.Max(10, message.DurationSeconds);
        _durationReady = !message.SpeechEnabled;

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

        _fullscreenReassertTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _fullscreenReassertTimer.Tick += FullscreenReassertTimer_OnTick;

        _gradientTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _gradientTimer.Tick += GradientTimer_OnTick;

        ConfigureWindow();
        ApplyTheme();
        ApplyMessage();

        Opened += ShoutWindow_OnOpened;
        Closed += ShoutWindow_OnClosed;
        KeyDown += ShoutWindow_OnKeyDown;
        SizeChanged += (_, _) => UpdateMessageLayout();
    }

    public bool ShouldStopSpeechOnClose { get; set; } = true;

    public void SetDisplayDuration(TimeSpan duration)
    {
        if (_closed)
        {
            return;
        }

        _remainingSeconds = Math.Max(10, (int)Math.Ceiling(duration.TotalSeconds));
        _durationReady = true;
        if (_opened)
        {
            _countdownTimer.Start();
        }
    }

    private void ConfigureWindow()
    {
        Topmost = _message.Topmost;
        SystemDecorations = SystemDecorations.None;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowState = WindowState.FullScreen;
        ShowInTaskbar = false;
        MessageFrame.Padding = new Avalonia.Thickness(56);
        CountdownText.IsVisible = false;
    }

    private void UpdateMessageLayout()
    {
        var padding = MessageFrame.Padding;
        var frameWidth = MessageFrame.Bounds.Width > 0 ? MessageFrame.Bounds.Width : Math.Max(0, ClientSize.Width - 68);
        var frameHeight = MessageFrame.Bounds.Height > 0 ? MessageFrame.Bounds.Height : Math.Max(0, ClientSize.Height - AccentBand.Bounds.Height - 68);
        var availableWidth = Math.Max(240, frameWidth - padding.Left - padding.Right);
        var availableHeight = Math.Max(160, frameHeight - padding.Top - padding.Bottom);
        var textWidth = Math.Min(availableWidth, 1700);
        var fontSize = CalculateMessageFontSize(_message.Message, textWidth, availableHeight, true);

        MessageViewport.MinHeight = availableHeight;
        MessageText.Width = textWidth;
        MessageText.MaxWidth = textWidth;
        MessageText.FontSize = fontSize;
        MessageText.LineHeight = Math.Ceiling(fontSize * 1.32);

        var titleWidth = Math.Max(320, ClientSize.Width - 220);
        TitleText.MaxWidth = titleWidth;
        TitleText.FontSize = CalculateTitleFontSize(_message.Title, titleWidth);
    }

    private void ApplyTheme()
    {
        var palette = ThemePalette.For(_message.Theme);
        _accentStart = palette.AccentStart;
        _accentMiddle = palette.AccentMiddle;
        _accentEnd = palette.AccentEnd;
        _accentGradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(_accentStart, 0),
                new GradientStop(_accentMiddle, 0.52),
                new GradientStop(_accentEnd, 1)
            }
        };
        Background = palette.Page;
        RootGrid.Background = palette.Page;
        AccentBand.Background = _accentGradient;
        EyebrowText.Foreground = palette.HeaderSubtleText;
        TitleText.Foreground = palette.HeaderText;
        CountdownText.Foreground = palette.HeaderSubtleText;
        MessageFrame.Background = palette.Card;
        MessageFrame.BorderBrush = palette.Border;
        MessageFrame.BorderThickness = new Avalonia.Thickness(1);
        MessageText.Foreground = palette.Text;
        CloseButton.Foreground = palette.HeaderText;
        CloseButton.Background = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));
        CloseButton.BorderBrush = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255));
        CloseButton.BorderThickness = new Avalonia.Thickness(1);
    }

    private void ApplyMessage()
    {
        Title = _message.Title;
        EyebrowText.Text = "OpenRemoteShouter";
        TitleText.Text = _message.Title;
        MessageText.Text = _message.Message;
        Dispatcher.UIThread.Post(UpdateMessageLayout, DispatcherPriority.Loaded);
        UpdateCountdownText();
    }

    private void ShoutWindow_OnOpened(object? sender, EventArgs e)
    {
        ApplyFullscreenState();
        _fullscreenReassertTicks = 0;
        _fullscreenReassertTimer.Start();
        _gradientTimer.Start();

        PlatformTopmostService.Apply(this, _message.Topmost);
        Dispatcher.UIThread.Post(UpdateMessageLayout, DispatcherPriority.Loaded);

        if (_message.Topmost && OperatingSystem.IsWindows())
        {
            _topmostReassertTimer.Start();
        }

        _opened = true;
        if (_durationReady)
        {
            _countdownTimer.Start();
        }

        _ = PlayEntranceAnimationAsync();
    }

    private void ShoutWindow_OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _opened = false;
        _countdownTimer.Stop();
        _topmostReassertTimer.Stop();
        _fullscreenReassertTimer.Stop();
        _gradientTimer.Stop();
    }

    private void GradientTimer_OnTick(object? sender, EventArgs e)
    {
        if (_accentGradient is null || _closed)
        {
            return;
        }

        _gradientPhase += 0.035;
        var wave = (Math.Sin(_gradientPhase) + 1) / 2;
        var drift = 0.28 + (wave * 0.44);
        _accentGradient.StartPoint = new RelativePoint(drift - 0.55, 0, RelativeUnit.Relative);
        _accentGradient.EndPoint = new RelativePoint(drift + 0.95, 1, RelativeUnit.Relative);

        if (_accentGradient.GradientStops.Count >= 3)
        {
            _accentGradient.GradientStops[1].Color = Blend(_accentMiddle, _accentEnd, wave * 0.25);
        }
    }

    public async Task PlayEntranceAnimationAsync()
    {
        if (_closed)
        {
            return;
        }

        const double durationMs = 760;
        _titleTransform.Y = -72;
        _messageTransform.Y = -92;
        TitleText.Opacity = 0;
        MessageText.Opacity = 0;

        var started = DateTime.UtcNow;
        while (!_closed)
        {
            var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
            var progress = Math.Clamp(elapsed / durationMs, 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);

            _titleTransform.Y = -72 * (1 - eased);
            _messageTransform.Y = -92 * (1 - eased);
            TitleText.Opacity = eased;
            MessageText.Opacity = eased;

            if (progress >= 1)
            {
                break;
            }

            await Task.Delay(16);
        }

        if (!_closed)
        {
            _titleTransform.Y = 0;
            _messageTransform.Y = 0;
            TitleText.Opacity = 1;
            MessageText.Opacity = 1;
        }
    }

    private void FullscreenReassertTimer_OnTick(object? sender, EventArgs e)
    {
        ApplyFullscreenState();
        _fullscreenReassertTicks++;

        if (_fullscreenReassertTicks >= 8)
        {
            _fullscreenReassertTimer.Stop();
        }
    }

    private void ApplyFullscreenState()
    {
        WindowState = WindowState.FullScreen;

        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var bounds = screen.Bounds;
        var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;

        Position = bounds.Position;
        Width = Math.Ceiling(bounds.Width / scaling);
        Height = Math.Ceiling(bounds.Height / scaling);
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
        CountdownText.Text = string.Empty;
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

    private static double CalculateTitleFontSize(string text, double width)
    {
        var units = Math.Max(1, text.Sum(GetCharWidthUnit));
        var target = Math.Max(28, Math.Min(54, width / Math.Max(8, units * 0.52)));
        return Math.Round(target);
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (byte)(from.A + ((to.A - from.A) * amount)),
            (byte)(from.R + ((to.R - from.R) * amount)),
            (byte)(from.G + ((to.G - from.G) * amount)),
            (byte)(from.B + ((to.B - from.B) * amount)));
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
        IBrush HeaderText,
        IBrush HeaderSubtleText,
        IBrush Card,
        IBrush Border,
        IBrush Text,
        Color AccentStart,
        Color AccentMiddle,
        Color AccentEnd)
    {
        public static ThemePalette For(string theme)
        {
            return theme switch
            {
                "blue" => Create("#EFF6FF", "#1D4ED8", "#60A5FA", "#2563EB", "#FFFFFF", "#BFDBFE"),
                "green" => Create("#F0FDF4", "#166534", "#4ADE80", "#15803D", "#FFFFFF", "#BBF7D0"),
                "amber" => Create("#FFFBEB", "#92400E", "#F59E0B", "#B45309", "#FFFFFF", "#FDE68A"),
                "rose" => Create("#FFF1F2", "#9F1239", "#FB7185", "#BE123C", "#FFFFFF", "#FECDD3"),
                "violet" => Create("#F5F3FF", "#5B21B6", "#A78BFA", "#6D28D9", "#FFFFFF", "#DDD6FE"),
                _ => Create("#ECFEFF", "#0E7490", "#22D3EE", "#0891B2", "#FFFFFF", "#A5F3FC")
            };
        }

        private static ThemePalette Create(
            string page,
            string accentStart,
            string accentMiddle,
            string accentEnd,
            string card,
            string border)
        {
            var headerText = ContrastColor(accentStart);
            var headerSubtleText = headerText == "#FFFFFF" ? "#E5E7EB" : "#374151";
            var text = ContrastColor(card);
            return new ThemePalette(
                Brush(page),
                Brush(headerText),
                Brush(headerSubtleText),
                Brush(card),
                Brush(border),
                Brush(text),
                Color.Parse(accentStart),
                Color.Parse(accentMiddle),
                Color.Parse(accentEnd));
        }

        private static string ContrastColor(string background)
        {
            var color = Color.Parse(background);
            var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
            return luminance >= 0.58 ? "#111827" : "#FFFFFF";
        }

        private static SolidColorBrush Brush(string color)
        {
            return new SolidColorBrush(Color.Parse(color));
        }
    }
}
