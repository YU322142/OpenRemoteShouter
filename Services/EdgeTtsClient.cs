using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

public sealed class EdgeTtsClient
{
    private const string ChromiumVersion = "143.0.3650.75";
    private const string ChromiumMajorVersion = "143";
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    public static IReadOnlyList<EdgeTtsVoice> AvailableVoices { get; } =
    [
        new(
            "Microsoft Server Speech Text to Speech Voice (zh-CN, XiaoxiaoNeural)",
            "zh-CN-XiaoxiaoNeural",
            "zh-CN",
            "audio-24khz-48kbitrate-mono-mp3"),
        new(
            "Microsoft Server Speech Text to Speech Voice (zh-CN, XiaoyiNeural)",
            "zh-CN-XiaoyiNeural",
            "zh-CN",
            "audio-24khz-48kbitrate-mono-mp3"),
        new(
            "Microsoft Server Speech Text to Speech Voice (zh-CN, YunxiNeural)",
            "zh-CN-YunxiNeural",
            "zh-CN",
            "audio-24khz-48kbitrate-mono-mp3")
    ];

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceShortName,
        string outputFormat,
        int rate,
        float volume,
        CancellationToken cancellationToken)
    {
        var voice = AvailableVoices.FirstOrDefault(x =>
            string.Equals(x.ShortName, voiceShortName, StringComparison.OrdinalIgnoreCase))
            ?? AvailableVoices[0];

        var requestId = Guid.NewGuid().ToString("N");
        using var ws = new ClientWebSocket();
        ConfigureWebSocket(ws);

        var uri = new Uri(
            "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1" +
            $"?TrustedClientToken={TrustedClientToken}" +
            $"&ConnectionId={requestId}" +
            $"&Sec-MS-GEC={GenerateSecMsGecToken()}" +
            $"&Sec-MS-GEC-Version=1-{ChromiumVersion}");

        await ws.ConnectAsync(uri, cancellationToken);
        await SendTextAsync(ws, BuildAudioConfig(outputFormat), cancellationToken);
        await SendTextAsync(ws, BuildSsmlMessage(requestId, voice, rate, volume, text), cancellationToken);

        return await ReceiveAudioAsync(ws, cancellationToken);
    }

    private static void ConfigureWebSocket(ClientWebSocket ws)
    {
        ws.Options.Cookies = new CookieContainer();
        ws.Options.Cookies.SetCookies(
            new Uri("https://speech.platform.bing.com"),
            $"muid={Guid.NewGuid().ToString().ToUpperInvariant().Replace("-", string.Empty)};");
        ws.Options.HttpVersion = HttpVersion.Version11;
        ws.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] =
                $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumMajorVersion}.0.0.0 Safari/537.36 Edg/{ChromiumMajorVersion}.0.0.0",
            ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8",
            ["Pragma"] = "no-cache",
            ["Cache-Control"] = "no-cache",
            ["Origin"] = "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold",
            ["Accept"] = "*/*"
        };

        foreach (var header in headers)
        {
            ws.Options.SetRequestHeader(header.Key, header.Value);
        }
    }

    private static async Task SendTextAsync(
        ClientWebSocket ws,
        string text,
        CancellationToken cancellationToken)
    {
        await ws.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private static async Task<byte[]> ReceiveAudioAsync(
        ClientWebSocket ws,
        CancellationToken cancellationToken)
    {
        var audio = new List<byte>();
        var receiveBuffer = new byte[32 * 1024];
        var messageBuffer = new List<byte>();

        while (ws.State is WebSocketState.Open or WebSocketState.Connecting)
        {
            if (ws.State == WebSocketState.Connecting)
            {
                await Task.Delay(10, cancellationToken);
                continue;
            }

            var result = await ws.ReceiveAsync(receiveBuffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            messageBuffer.AddRange(receiveBuffer.Take(result.Count));
            if (!result.EndOfMessage)
            {
                continue;
            }

            var message = messageBuffer.ToArray();
            messageBuffer.Clear();

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(message);
                if (text.Contains("Path:turn.end", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                AppendAudioPayload(message, audio);
            }
        }

        if (audio.Count == 0)
        {
            throw new InvalidOperationException("EdgeTTS 未返回音频数据。");
        }

        return audio.ToArray();
    }

    private static void AppendAudioPayload(byte[] message, List<byte> audio)
    {
        var span = new ReadOnlySpan<byte>(message);
        if (span.Length < 2)
        {
            return;
        }

        var headerLength = BinaryPrimitives.ReadUInt16BigEndian(span[..2]);
        var payloadStart = 2 + headerLength;
        if (span.Length <= payloadStart)
        {
            return;
        }

        audio.AddRange(span[payloadStart..].ToArray());
    }

    private static string BuildAudioConfig(string outputFormat)
    {
        return
            "Content-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n" +
            "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"" +
            outputFormat +
            "\"}}}}";
    }

    private static string BuildSsmlMessage(
        string requestId,
        EdgeTtsVoice voice,
        int rate,
        float volume,
        string text)
    {
        var ssml = BuildSsml(voice, rate, volume, text);
        return
            $"X-RequestId:{requestId}\r\n" +
            "Content-Type:application/ssml+xml\r\n" +
            $"X-Timestamp:{GetTimestamp()}Z\r\n" +
            "Path:ssml\r\n\r\n" +
            ssml;
    }

    private static string BuildSsml(
        EdgeTtsVoice voice,
        int rate,
        float volume,
        string text)
    {
        var escapedText = SecurityElement.Escape(text) ?? string.Empty;
        var ssmlVolume = Math.Clamp((int)Math.Round(volume * 100), 0, 100);
        return
            $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{voice.Locale}'>" +
            $"<voice name='{voice.Name}'>" +
            $"<prosody pitch='+0Hz' rate='{FormatPercentage(rate)}' volume='{ssmlVolume}'>" +
            escapedText +
            "</prosody></voice></speak>";
    }

    private static string FormatPercentage(int input)
    {
        return input.ToString("+#;-#;0", CultureInfo.InvariantCulture) + "%";
    }

    private static string GetTimestamp()
    {
        return DateTimeOffset.UtcNow.ToString(
            "ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'",
            CultureInfo.InvariantCulture);
    }

    private static string GenerateSecMsGecToken()
    {
        var ticks = DateTime.Now.ToFileTimeUtc();
        ticks -= ticks % 3_000_000_000;
        var tokenSource = ticks + TrustedClientToken;
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(tokenSource)));
    }
}
