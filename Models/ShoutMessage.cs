namespace RemoteShouter.Models;

public sealed record ShoutMessage(
    string Title,
    string Message,
    ShoutDisplayMode Mode,
    int DurationSeconds,
    bool Topmost,
    bool SpeechEnabled,
    string VoiceName,
    int SpeechRate,
    float SpeechVolume,
    string Theme,
    DateTimeOffset ReceivedAt);
