namespace RemoteShouter.Models;

public sealed record ServerStatus(
    bool IsRunning,
    int Port,
    IReadOnlyList<string> Urls,
    string SpeechBackend,
    string? SpeechError,
    string? Error);
