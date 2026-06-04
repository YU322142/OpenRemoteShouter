namespace RemoteShouter.Models;

public sealed record EdgeTtsVoice(
    string Name,
    string ShortName,
    string Locale,
    string SuggestedCodec);
