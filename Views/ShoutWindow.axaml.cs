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
    private readonly TranslateTransform _accentTransform = new();
    private readonly TranslateTransform _bodyTransform = new();
    private readonly TranslateTransform _frameTransform = new();
    private readonly TranslateTransform _closeTransform = new();
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
        AccentBand.RenderTransform = _accentTransform;
        BodySurface.RenderTransform = _bodyTransform;
        MessageFrame.RenderTransform = _frameTransform;
        CloseButton.RenderTransform = _closeTransform;
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
        PrepareEntranceState();

        Opened += ShoutWindow_OnOpened;
        Closed += ShoutWindow_OnClosed;
        KeyDown += ShoutWindow_OnKeyDown;
        SizeChanged += (_, _) => UpdateMessageLayout();
    }

    public bool ShouldStopSpeechOnClose { get; set; } = true;

    private void PrepareEntranceState()
    {
        // Set this before Show() so the platform cannot paint one completed
        // frame before the Opened animation gets its first dispatcher turn.
        _accentTransform.Y = -1000;
        _bodyTransform.Y = -10000;
        _frameTransform.Y = -1000;
        _closeTransform.Y = -1000;
        CloseButton.Opacity = 0;
    }

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
        // Keep the window itself transparent so the existing desktop remains
        // visible while the title and body surfaces enter from above.
        Background = Brushes.Transparent;
        RootGrid.Background = Brushes.Transparent;
        AccentBand.Background = _accentGradient;
        BodySurface.Background = palette.Page;
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

        const double titleDurationMs = 720;
        const double bodyDelayMs = 100;
        const double bodyDurationMs = 900;
        const double frameDelayMs = 260;
        const double frameDurationMs = 820;
        const double closeDelayMs = 180;
        const double closeDurationMs = 650;
        var titleTravel = Math.Max(AccentBand.Bounds.Height, 210) + 16;
        var bodyTravel = Math.Max(BodySurface.Bounds.Height, ClientSize.Height - AccentBand.Bounds.Height) + 16;
        _accentTransform.Y = -titleTravel;
        _bodyTransform.Y = -bodyTravel;
        _frameTransform.Y = -220;
        _closeTransform.Y = -120;
        CloseButton.Opacity = 0;

        var started = DateTime.UtcNow;
        while (!_closed)
        {
            var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
            var titleProgress = Math.Clamp(elapsed / titleDurationMs, 0, 1);
            var bodyProgress = Math.Clamp((elapsed - bodyDelayMs) / bodyDurationMs, 0, 1);
            var frameProgress = Math.Clamp((elapsed - frameDelayMs) / frameDurationMs, 0, 1);
            var closeProgress = Math.Clamp((elapsed - closeDelayMs) / closeDurationMs, 0, 1);
            var titleEased = EaseOutExpo(titleProgress, 7.2);
            var bodyEased = EaseOutExpo(bodyProgress, 5.4);
            var frameEased = EaseOutExpo(frameProgress, 7.8);
            var closeEased = EaseOutExpo(closeProgress, 6.5);

            _accentTransform.Y = -titleTravel * (1 - titleEased);
            _bodyTransform.Y = -bodyTravel * (1 - bodyEased);
            _frameTransform.Y = -220 * (1 - frameEased);
            _closeTransform.Y = -120 * (1 - closeEased);
            CloseButton.Opacity = closeEased;

            if (titleProgress >= 1 && bodyProgress >= 1 && frameProgress >= 1 && closeProgress >= 1)
            {
                break;
            }

            await Task.Delay(16);
        }

        if (!_closed)
        {
            _accentTransform.Y = 0;
            _bodyTransform.Y = 0;
            _frameTransform.Y = 0;
            _closeTransform.Y = 0;
            CloseButton.Opacity = 1;
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

    private static double EaseOutExpo(double progress, double strength)
    {
        progress = Math.Clamp(progress, 0, 1);
        var tail = Math.Pow(2, -strength);
        return (1 - Math.Pow(2, -strength * progress)) / (1 - tail);
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
                "blue" => Create("#EFF6FF", "#1E40AF", "#2563EB", "#60A5FA", "#FFFFFF", "#BFDBFE"),
                "blue-dark" => Create("#0B1F3A", "#1E40AF", "#2563EB", "#60A5FA", "#111D35", "#315A9E"),
                "green" => Create("#F0FDF4", "#047857", "#15803D", "#4ADE80", "#FFFFFF", "#BBF7D0"),
                "green-dark" => Create("#092B22", "#047857", "#15803D", "#34D399", "#102A24", "#2C8A70"),
                "amber" => Create("#FFFBEB", "#B45309", "#D97706", "#F59E0B", "#FFFFFF", "#FDE68A"),
                "amber-dark" => Create("#33220B", "#B45309", "#D97706", "#FBBF24", "#2A1B0A", "#9A6A22"),
                "rose" => Create("#FFF1F2", "#BE123C", "#E11D48", "#FB7185", "#FFFFFF", "#FECDD3"),
                "rose-dark" => Create("#3A111F", "#BE123C", "#E11D48", "#FB7185", "#2E1320", "#A74A68"),
                "violet" => Create("#F5F3FF", "#6D28D9", "#7C3AED", "#A78BFA", "#FFFFFF", "#DDD6FE"),
                "violet-dark" => Create("#21133D", "#6D28D9", "#7C3AED", "#A78BFA", "#21182F", "#7652B6"),
                "indigo" => Create("#EEF2FF", "#4F46E5", "#6366F1", "#818CF8", "#FFFFFF", "#C7D2FE"),
                "indigo-dark" => Create("#171A3A", "#4F46E5", "#6366F1", "#818CF8", "#191B32", "#5C65B6"),
                "magenta" => Create("#FDF4FF", "#C026D3", "#D946EF", "#E879F9", "#FFFFFF", "#F5D0FE"),
                "magenta-dark" => Create("#351333", "#C026D3", "#D946EF", "#E879F9", "#2B182C", "#A958A9"),
                "orange" => Create("#FFF7ED", "#EA580C", "#F97316", "#FB923C", "#FFFFFF", "#FED7AA"),
                "orange-dark" => Create("#3A1D0B", "#EA580C", "#F97316", "#FB923C", "#2D1A10", "#B76A38"),
                "emerald" => Create("#ECFDF5", "#047857", "#059669", "#34D399", "#FFFFFF", "#A7F3D0"),
                "emerald-dark" => Create("#092B25", "#047857", "#059669", "#34D399", "#102A25", "#2C8A72"),
                "cyan-dark" => Create("#0B2B33", "#0E7490", "#0891B2", "#22D3EE", "#10262C", "#287A88"),
                _ => Create("#ECFEFF", "#05616B", "#087F8C", "#22D3EE", "#FFFFFF", "#A5F3FC")
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
