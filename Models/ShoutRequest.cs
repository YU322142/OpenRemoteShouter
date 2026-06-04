namespace RemoteShouter.Models;

public sealed class ShoutRequest
{
    public const string DefaultVoiceName = "zh-CN-XiaoyiNeural";
    public const string DefaultTheme = "cyan";

    public string? Title { get; set; }

    public string? Message { get; set; }

    public string? Mode { get; set; } = "fullscreen";

    public int DurationSeconds { get; set; } = 10;

    public bool Topmost { get; set; } = true;

    public bool SpeechEnabled { get; set; } = true;

    public string? VoiceName { get; set; } = DefaultVoiceName;

    public int SpeechRate { get; set; } = 0;

    public float SpeechVolume { get; set; } = 1.0f;

    public string? Theme { get; set; } = DefaultTheme;

    public (bool IsValid, string? Error, ShoutMessage? Message) ToMessage()
    {
        var body = (Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return (false, "\u558a\u8bdd\u5185\u5bb9\u4e0d\u80fd\u4e3a\u7a7a\u3002", null);
        }

        var normalizedMode = (Mode ?? "fullscreen").Trim().ToLowerInvariant();
        var mode = normalizedMode switch
        {
            "fullscreen" or "full" or "screen" or "\u5168\u5c4f" => ShoutDisplayMode.Fullscreen,
            "popup" or "window" or "dialog" or "\u5f39\u7a97" => ShoutDisplayMode.Popup,
            _ => ShoutDisplayMode.Fullscreen
        };

        var duration = Math.Clamp(DurationSeconds, 0, 3600);
        var title = string.IsNullOrWhiteSpace(Title) ? "\u8fdc\u7a0b\u558a\u8bdd" : Title.Trim();
        var voiceName = string.IsNullOrWhiteSpace(VoiceName) ? DefaultVoiceName : VoiceName.Trim();
        var rate = Math.Clamp(SpeechRate, -100, 100);
        var volume = Math.Clamp(SpeechVolume, 0.0f, 1.0f);
        var theme = NormalizeTheme(Theme);

        return (true, null, new ShoutMessage(
            title,
            body,
            mode,
            duration,
            Topmost,
            SpeechEnabled,
            voiceName,
            rate,
            volume,
            theme,
            DateTimeOffset.Now));
    }

    private static string NormalizeTheme(string? theme)
    {
        return (theme ?? DefaultTheme).Trim().ToLowerInvariant() switch
        {
            "blue" => "blue",
            "green" => "green",
            "amber" => "amber",
            "rose" => "rose",
            "violet" => "violet",
            _ => DefaultTheme
        };
    }
}
