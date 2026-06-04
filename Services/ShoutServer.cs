using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

public sealed class ShoutServer
{
    private readonly ShoutDisplayService _displayService;
    private readonly int _port;
    private WebApplication? _app;
    private string? _lastError;

    public ShoutServer(ShoutDisplayService displayService, int port = 21212)
    {
        _displayService = displayService;
        _port = port;
    }

    public ServerStatus Status => new(
        _app is not null,
        _port,
        BuildDisplayUrls(),
        AudioPlaybackService.GetPlaybackBackendDescription(),
        _displayService.SpeechError,
        _lastError);

    public async Task StartAsync()
    {
        if (_app is not null)
        {
            return;
        }

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [],
                ApplicationName = typeof(ShoutServer).Assembly.GetName().Name
            });

            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(_port);
            });
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });

            var app = builder.Build();
            MapRoutes(app);

            await app.StartAsync();
            _app = app;
            _lastError = null;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_app is null)
        {
            return;
        }

        var app = _app;
        _app = null;
        await app.StopAsync();
        await app.DisposeAsync();
    }

    private void MapRoutes(WebApplication app)
    {
        app.MapGet("/", () => Results.Content(BuildIndexHtml(), "text/html; charset=utf-8"));

        app.MapGet("/api/status", () => Results.Json(Status));

        app.MapGet("/api/voices", () => Results.Json(EdgeTtsClient.AvailableVoices));

        app.MapPost("/api/shout", async (HttpRequest request) =>
        {
            var shoutRequest = await ReadRequestAsync(request);
            var parsed = shoutRequest.ToMessage();

            if (!parsed.IsValid || parsed.Message is null)
            {
                return Results.BadRequest(new { ok = false, error = parsed.Error });
            }

            await _displayService.ShowAsync(parsed.Message);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/close", async () =>
        {
            await _displayService.CloseAsync();
            return Results.Ok(new { ok = true });
        });
    }

    private static async Task<ShoutRequest> ReadRequestAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            return new ShoutRequest
            {
                Title = form["title"],
                Message = form["message"],
                Mode = form["mode"],
                Theme = form["theme"],
                Topmost = IsTruthy(form["topmost"].ToString()),
                DurationSeconds = int.TryParse(form["durationSeconds"], out var seconds) ? seconds : 10,
                SpeechEnabled = IsTruthy(form["speechEnabled"].ToString()),
                VoiceName = form["voiceName"],
                SpeechRate = int.TryParse(form["speechRate"], out var rate) ? rate : 0,
                SpeechVolume = float.TryParse(
                    form["speechVolume"],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var volume)
                    ? volume
                    : 1.0f
            };
        }

        var body = await request.ReadFromJsonAsync<ShoutRequest>();
        return body ?? new ShoutRequest();
    }

    private static bool IsTruthy(string value)
    {
        return value.Equals("on", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> BuildDisplayUrls()
    {
        var urls = new List<string>
        {
            $"http://localhost:{_port}/",
            $"http://127.0.0.1:{_port}/"
        };

        try
        {
            var host = Dns.GetHostName();
            var addresses = Dns.GetHostEntry(host).AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .Where(address => !IPAddress.IsLoopback(address))
                .Select(address => $"http://{address}:{_port}/");

            urls.AddRange(addresses);
        }
        catch
        {
            // Local URLs above are still usable.
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string BuildIndexHtml()
    {
        return """
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>OpenRemoteShouter</title>
  <style>
    :root {
      color-scheme: light;
      font-family: "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", system-ui, sans-serif;
      background: #ecfeff;
      color: #102a30;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 28px;
      background:
        linear-gradient(135deg, rgba(14, 116, 144, .16), transparent 38%),
        linear-gradient(315deg, rgba(15, 118, 110, .13), transparent 42%),
        #ecfeff;
    }
    main {
      width: min(760px, 100%);
      background: rgba(255, 255, 255, .94);
      border: 1px solid rgba(14, 116, 144, .18);
      border-radius: 8px;
      box-shadow: 0 24px 80px rgba(15, 23, 42, .14);
      padding: 24px;
    }
    h1 {
      margin: 0 0 18px;
      font-size: clamp(28px, 5vw, 44px);
      letter-spacing: 0;
    }
    label {
      display: block;
      margin: 16px 0 8px;
      font-weight: 700;
    }
    input[type="text"], input[type="number"], textarea, select {
      width: 100%;
      border: 1px solid rgba(14, 116, 144, .22);
      border-radius: 6px;
      padding: 12px 14px;
      font: inherit;
      color: inherit;
      background: #fff;
    }
    textarea {
      min-height: 190px;
      resize: vertical;
      font-size: 20px;
      line-height: 1.55;
    }
    .row {
      display: flex;
      gap: 14px;
      flex-wrap: wrap;
      align-items: center;
    }
    .grow { flex: 1 1 220px; }
    .choice {
      display: inline-flex;
      gap: 8px;
      align-items: center;
      min-height: 40px;
      padding-right: 14px;
    }
    .actions {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      margin-top: 22px;
    }
    button {
      border: 0;
      border-radius: 6px;
      padding: 12px 18px;
      font: inherit;
      font-weight: 800;
      cursor: pointer;
      color: white;
      background: #0e7490;
    }
    button.secondary { background: #9b3f37; }
    #result {
      min-height: 28px;
      margin-top: 16px;
      font-weight: 700;
    }
    @media (max-width: 560px) {
      body { padding: 14px; }
      main { padding: 18px; }
      textarea { min-height: 150px; }
      button { width: 100%; }
    }
  </style>
</head>
<body>
  <main>
    <h1>OpenRemoteShouter</h1>
    <form id="shoutForm">
      <label for="title">&#x6807;&#x9898;</label>
      <input id="title" name="title" type="text" value="OpenRemoteShouter" maxlength="60">

      <label for="message">&#x5185;&#x5bb9;</label>
      <textarea id="message" name="message" maxlength="3000" required autofocus></textarea>

      <label>&#x663e;&#x793a;&#x65b9;&#x5f0f;</label>
      <div class="row">
        <span class="choice"><input type="radio" id="modeFullscreen" name="mode" value="fullscreen" checked><label for="modeFullscreen">&#x5168;&#x5c4f;&#x7f6e;&#x9876;</label></span>
        <span class="choice"><input type="radio" id="modePopup" name="mode" value="popup"><label for="modePopup">&#x5f39;&#x7a97;&#x663e;&#x793a;</label></span>
      </div>

      <label for="theme">&#x663e;&#x793a;&#x8272;&#x8c03;</label>
      <select id="theme" name="theme">
        <option value="cyan" selected>&#x9752;&#x8272;</option>
        <option value="blue">&#x84dd;&#x8272;</option>
        <option value="green">&#x7eff;&#x8272;</option>
        <option value="amber">&#x7425;&#x73c0;&#x8272;</option>
        <option value="rose">&#x73ab;&#x7470;&#x8272;</option>
        <option value="violet">&#x7d2b;&#x8272;</option>
      </select>

      <label for="durationSeconds">&#x81ea;&#x52a8;&#x5173;&#x95ed;&#x5012;&#x8ba1;&#x65f6;&#xff0c;0 &#x8868;&#x793a;&#x4e0d;&#x81ea;&#x52a8;&#x5173;&#x95ed;</label>
      <input id="durationSeconds" name="durationSeconds" type="number" min="0" max="3600" value="10">

      <label class="choice"><input id="topmost" name="topmost" type="checkbox" checked> &#x7a97;&#x53e3;&#x7f6e;&#x9876;</label>

      <label class="choice"><input id="speechEnabled" name="speechEnabled" type="checkbox" checked> &#x542f;&#x7528;&#x8bed;&#x97f3;&#x64ad;&#x62a5;</label>

      <label for="voiceName">EdgeTTS &#x8bf4;&#x8bdd;&#x4eba;</label>
      <select id="voiceName" name="voiceName">
        <option value="zh-CN-XiaoxiaoNeural">&#x5c0f;&#x6653;&#xff08;&#x5973;&#x58f0;&#xff09;</option>
        <option value="zh-CN-XiaoyiNeural" selected>&#x5c0f;&#x827a;&#xff08;&#x5973;&#x58f0;&#xff09;</option>
        <option value="zh-CN-YunxiNeural">&#x4e91;&#x5e0c;&#xff08;&#x7537;&#x58f0;&#xff09;</option>
      </select>

      <div class="row">
        <div class="grow">
          <label for="speechRate">&#x8bed;&#x901f;</label>
          <input id="speechRate" name="speechRate" type="number" min="-100" max="100" value="0">
        </div>
        <div class="grow">
          <label for="speechVolume">&#x8bed;&#x97f3;&#x97f3;&#x91cf;</label>
          <input id="speechVolume" name="speechVolume" type="number" min="0" max="1" step="0.05" value="1">
        </div>
      </div>

      <div class="actions">
        <button type="submit">&#x53d1;&#x9001;&#x558a;&#x8bdd;</button>
        <button class="secondary" type="button" id="closeButton">&#x5173;&#x95ed;&#x5f53;&#x524d;&#x663e;&#x793a;</button>
      </div>
      <div id="result" role="status" aria-live="polite"></div>
    </form>
  </main>

  <script>
    const form = document.getElementById('shoutForm');
    const result = document.getElementById('result');
    const closeButton = document.getElementById('closeButton');

    form.addEventListener('submit', async event => {
      event.preventDefault();
      const data = new FormData(form);
      const payload = {
        title: data.get('title'),
        message: data.get('message'),
        mode: data.get('mode'),
        theme: data.get('theme'),
        durationSeconds: Number(data.get('durationSeconds')),
        topmost: data.get('topmost') === 'on',
        speechEnabled: data.get('speechEnabled') === 'on',
        voiceName: data.get('voiceName'),
        speechRate: Number(data.get('speechRate')),
        speechVolume: Number(data.get('speechVolume'))
      };

      result.textContent = '\u6b63\u5728\u53d1\u9001...';
      const response = await fetch('/api/shout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json; charset=utf-8' },
        body: JSON.stringify(payload)
      });

      const body = await response.json();
      result.textContent = response.ok && body.ok ? '\u5df2\u53d1\u9001\u3002' : (body.error || '\u53d1\u9001\u5931\u8d25\u3002');
    });

    closeButton.addEventListener('click', async () => {
      result.textContent = '\u6b63\u5728\u5173\u95ed...';
      const response = await fetch('/api/close', { method: 'POST' });
      result.textContent = response.ok ? '\u5df2\u5173\u95ed\u5f53\u524d\u663e\u793a\u3002' : '\u5173\u95ed\u5931\u8d25\u3002';
    });
  </script>
</body>
</html>
""";
    }
}
