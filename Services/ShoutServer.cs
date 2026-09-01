using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

public sealed class ShoutServer
{
    private readonly ShoutDisplayService _displayService;
    private readonly AccountService _accountService = new();
    private readonly int _port;
    private readonly X509Certificate2? _httpsCertificate;
    private readonly bool _allowInsecureHttp;
    private WebApplication? _app;
    private string? _lastError;

    public ShoutServer(ShoutDisplayService displayService, int port = 21212)
    {
        _displayService = displayService;
        _port = port;
        _httpsCertificate = LoadHttpsCertificate();
        _allowInsecureHttp = IsEnvironmentTruthy(
            Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP"));
        if (_httpsCertificate is null
            && IsEnvironmentTruthy(Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_REQUIRE_HTTPS")))
        {
            throw new InvalidOperationException(
                "HTTPS is required, but OPEN_REMOTE_SHOUTER_HTTPS_CERT_PATH is not configured.");
        }
    }

    public ServerStatus Status => new(
        _app is not null,
        _port,
        BuildDisplayUrls(),
        AudioPlaybackService.GetPlaybackBackendDescription(),
        AppLogService.LogFilePath,
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
            AppLogService.Info($"Starting shout server. port={_port}");
            // WebApplicationBuilder automatically inserts DeveloperExceptionPage
            // ahead of the user pipeline for the Development environment.  The
            // desktop app serves a LAN-facing API, so force a production-style
            // pipeline in that environment as well; otherwise framework
            // diagnostics would bypass the generic handler below and expose
            // stack traces, headers, and cookies to remote callers.
            var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var dotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var requestedEnvironment = aspNetCoreEnvironment ?? dotNetEnvironment;
            var safeEnvironment = string.Equals(
                                      aspNetCoreEnvironment,
                                      Environments.Development,
                                      StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(
                                      dotNetEnvironment,
                                      Environments.Development,
                                      StringComparison.OrdinalIgnoreCase)
                ? Environments.Production
                : requestedEnvironment;

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [],
                ApplicationName = typeof(ShoutServer).Assembly.GetName().Name,
                EnvironmentName = safeEnvironment
            });

            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
                if (_httpsCertificate is null)
                {
                    if (_allowInsecureHttp)
                    {
                        options.ListenAnyIP(_port);
                    }
                    else
                    {
                        // Keep an unencrypted development/default instance
                        // local-only.  LAN exposure must be an explicit opt-in
                        // or use the HTTPS certificate path below.
                        options.ListenLocalhost(_port);
                    }
                }
                else
                {
                    options.ListenAnyIP(_port, listenOptions => listenOptions.UseHttps(_httpsCertificate));
                }

                // Every endpoint accepts small JSON/form payloads only.  Keep
                // unauthenticated login/setup and authenticated shout requests
                // from buffering arbitrarily large bodies or hanging forever.
                options.Limits.MaxRequestBodySize = 64 * 1024;
                options.Limits.MaxRequestLineSize = 8 * 1024;
                options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                options.Limits.MaxConcurrentConnections = 256;
                options.Limits.MaxConcurrentUpgradedConnections = 64;
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = 32;
                options.KeyLengthLimit = 256;
                options.ValueLengthLimit = 16 * 1024;
                options.MultipartBodyLengthLimit = 64 * 1024;
            });
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.MaxDepth = 16;
            });

            var app = builder.Build();
            app.Use(async (context, next) =>
            {
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                context.Items["CspNonce"] = nonce;
                ApplySecurityHeaders(context, nonce);
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                    {
                        // A conservative hint for both the five-minute
                        // failure locks and the shorter account soft window.
                        // Clients should still honor a subsequent response if
                        // the server's limiter policy changes.
                        context.Response.Headers["Retry-After"] = "60";
                    }

                    return Task.CompletedTask;
                });
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    AppLogService.Error("Unhandled HTTP request", ex);
                    if (context.Response.HasStarted)
                    {
                        // Headers/body may already be on the wire.  Do not
                        // append exception details; terminate the response.
                        context.Abort();
                        return;
                    }

                    context.Response.Clear();
                    ApplySecurityHeaders(context, nonce);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = context.Request.Path.StartsWithSegments("/api")
                        ? "application/json; charset=utf-8"
                        : "text/plain; charset=utf-8";

                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        await context.Response.WriteAsJsonAsync(
                            new { ok = false, error = "Internal server error." });
                    }
                    else
                    {
                        await context.Response.WriteAsync("Internal server error.");
                    }
                }
            });
            MapRoutes(app);

            await app.StartAsync();
            _app = app;
            _lastError = null;
            if (_httpsCertificate is null)
            {
                var scope = _allowInsecureHttp ? "all interfaces" : "loopback only";
                AppLogService.Info(
                    $"WARNING: web server is using plaintext HTTP on {scope}. Configure "
                    + "OPEN_REMOTE_SHOUTER_HTTPS_CERT_PATH (and optionally "
                    + "OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD) before exposing it to an untrusted network."
                    + (_allowInsecureHttp
                        ? " OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP=1 explicitly enables LAN HTTP."
                        : " Set OPEN_REMOTE_SHOUTER_ALLOW_INSECURE_HTTP=1 only for a deliberate legacy LAN HTTP deployment."));
            }
            AppLogService.Info($"Shout server started. urls={string.Join(", ", BuildDisplayUrls())}");
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            AppLogService.Error("Failed to start shout server", ex);
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
        AppLogService.Info("Stopping shout server.");
        await app.StopAsync();
        await app.DisposeAsync();
        AppLogService.Info("Shout server stopped.");
    }

    private void MapRoutes(WebApplication app)
    {
        app.MapGet("/", (HttpContext context) =>
        {
            var nonce = context.Items["CspNonce"] as string ?? string.Empty;
            return Results.Content(WebUiHtml.Build(nonce), "text/html; charset=utf-8");
        });

        app.MapGet("/api/auth/state", (HttpContext context) =>
        {
            if (!_accountService.IsDatabaseAvailable)
            {
                return ApiError("Accounts database could not be loaded.", StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Json(new { ok = true, state = _accountService.GetAuthState(context.Request) });
        });

        app.MapPost("/api/auth/setup", async (HttpContext context) =>
        {
            if (!IsLocalRequest(context))
            {
                return ApiError(
                    "Initial admin setup must be completed from this computer.",
                    StatusCodes.Status403Forbidden);
            }

            var requestGuard = RequireSameOriginJson(context);
            if (requestGuard is not null)
            {
                return requestGuard;
            }

            var parsedRequest = await TryReadJsonAsync<SetupAdminRequest>(context.Request);
            if (!parsedRequest.IsValid)
            {
                return ApiError("Invalid JSON request.", StatusCodes.Status400BadRequest);
            }

            var request = parsedRequest.Value ?? new SetupAdminRequest(null, null, null);
            var result = _accountService.SetupAdmin(request, GetRemoteKey(context));
            if (!result.Ok || result.Session is null)
            {
                return ApiError(result.Error ?? "Setup failed.", result.StatusCode);
            }

            _accountService.SetSessionCookie(context.Response, context.Request, result.Session);
            return Results.Json(new { ok = true, state = new AuthState(false, result.Session.User, result.Session.CsrfToken) });
        });

        app.MapPost("/api/auth/login", async (HttpContext context) =>
        {
            var requestGuard = RequireSameOriginJson(context);
            if (requestGuard is not null)
            {
                return requestGuard;
            }

            var parsedRequest = await TryReadJsonAsync<LoginRequest>(context.Request);
            if (!parsedRequest.IsValid)
            {
                return ApiError("Invalid JSON request.", StatusCodes.Status400BadRequest);
            }

            var request = parsedRequest.Value ?? new LoginRequest(null, null);
            var result = _accountService.Login(request, GetRemoteKey(context));
            if (!result.Ok || result.Session is null)
            {
                return ApiError(result.Error ?? "Login failed.", result.StatusCode);
            }

            _accountService.SetSessionCookie(context.Response, context.Request, result.Session);
            return Results.Json(new { ok = true, state = new AuthState(false, result.Session.User, result.Session.CsrfToken) });
        });

        app.MapPost("/api/auth/logout", (HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            _accountService.Logout(session!);
            _accountService.ClearSessionCookie(context.Response, context.Request);
            return Results.Json(new { ok = true });
        });

        app.MapPost("/api/auth/password", async (HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            var contentGuard = RequireJsonContentType(context);
            if (contentGuard is not null)
            {
                return contentGuard;
            }

            var parsedRequest = await TryReadJsonAsync<ChangePasswordRequest>(context.Request);
            if (!parsedRequest.IsValid)
            {
                return ApiError("Invalid JSON request.", StatusCodes.Status400BadRequest);
            }

            var request = parsedRequest.Value ?? new ChangePasswordRequest(null, null);
            var result = _accountService.ChangePassword(session!.User, request);
            if (!result.Ok)
            {
                return ApiError(result.Error ?? "Password change failed.", result.StatusCode);
            }

            _accountService.ClearSessionCookie(context.Response, context.Request);
            return Results.Json(new { ok = true });
        });

        app.MapGet("/api/status", (HttpContext context) =>
        {
            var error = RequireSession(context, out var session);
            if (error is not null)
            {
                return error;
            }

            var status = Status;
            if (!_accountService.IsCurrentAdmin(session!.User))
            {
                // Absolute local paths and backend exception text are useful
                // to the desktop operator but needlessly disclose host data
                // to ordinary LAN users.
                status = status with
                {
                    LogFilePath = string.Empty,
                    SpeechError = null,
                    Error = null
                };
            }

            return Results.Json(status);
        });

        app.MapGet("/api/voices", (HttpContext context) =>
        {
            var error = RequireSession(context, out _);
            return error ?? Results.Json(EdgeTtsClient.AvailableVoices);
        });

        app.MapPost("/api/shout", async (HttpRequest request) =>
        {
            var error = RequireSession(request.HttpContext, out _, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            var shoutRequest = await ReadRequestAsync(request);
            var parsed = shoutRequest.ToMessage();

            if (!parsed.IsValid || parsed.Message is null)
            {
                return Results.BadRequest(new { ok = false, error = parsed.Error });
            }

            await _displayService.ShowAsync(parsed.Message);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/close", async (HttpContext context) =>
        {
            var error = RequireSession(context, out _, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            await _displayService.CloseAsync();
            return Results.Ok(new { ok = true });
        });

        app.MapGet("/api/users", (HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireAdmin: true);
            if (error is not null)
            {
                return error;
            }

            var users = _accountService.ListUsers(session!.User);
            return users.Ok
                ? Results.Json(new { ok = true, users = users.Users })
                : ApiError("Admin permission required.", StatusCodes.Status403Forbidden);
        });

        app.MapPost("/api/users", async (HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireAdmin: true, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            var contentGuard = RequireJsonContentType(context);
            if (contentGuard is not null)
            {
                return contentGuard;
            }

            var parsedRequest = await TryReadJsonAsync<CreateUserRequest>(context.Request);
            if (!parsedRequest.IsValid)
            {
                return ApiError("Invalid JSON request.", StatusCodes.Status400BadRequest);
            }

            var request = parsedRequest.Value ?? new CreateUserRequest(null, null, null, false);
            var result = _accountService.CreateUser(request, session!.User);
            return result.Ok
                ? Results.Json(new { ok = true, user = result.User })
                : ApiError(result.Error ?? "User creation failed.", result.StatusCode);
        });

        app.MapPut("/api/users/{username}", async (string username, HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireAdmin: true, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            var contentGuard = RequireJsonContentType(context);
            if (contentGuard is not null)
            {
                return contentGuard;
            }

            var parsedRequest = await TryReadJsonAsync<UpdateUserRequest>(context.Request);
            if (!parsedRequest.IsValid)
            {
                return ApiError("Invalid JSON request.", StatusCodes.Status400BadRequest);
            }

            var request = parsedRequest.Value ?? new UpdateUserRequest(null, null, null, null);
            var result = _accountService.UpdateUser(username, request, session!.User);
            return result.Ok
                ? Results.Json(new { ok = true, user = result.User })
                : ApiError(result.Error ?? "User update failed.", result.StatusCode);
        });

        app.MapDelete("/api/users/{username}", (string username, HttpContext context) =>
        {
            var error = RequireSession(context, out var session, requireAdmin: true, requireCsrf: true);
            if (error is not null)
            {
                return error;
            }

            var result = _accountService.DeleteUser(username, session!.User);
            return result.Ok
                ? Results.Json(new { ok = true })
                : ApiError(result.Error ?? "User deletion failed.", result.StatusCode);
        });
    }

    private IResult? RequireSession(
        HttpContext context,
        out AccountSession? session,
        bool requireAdmin = false,
        bool requireCsrf = false)
    {
        session = _accountService.GetSession(context.Request);
        if (session is null)
        {
            _accountService.ClearSessionCookie(context.Response, context.Request);
            return ApiError("Login required.", StatusCodes.Status401Unauthorized);
        }

        if (requireCsrf && !IsSameOriginRequest(context))
        {
            return ApiError("Cross-origin requests are not allowed.", StatusCodes.Status403Forbidden);
        }

        if (requireCsrf && !_accountService.ValidateCsrf(session, context.Request))
        {
            return ApiError("Security token expired. Please sign in again.", StatusCodes.Status403Forbidden);
        }

        if (requireAdmin && !_accountService.IsCurrentAdmin(session.User))
        {
            return ApiError("Admin permission required.", StatusCodes.Status403Forbidden);
        }

        return null;
    }

    private static IResult ApiError(string error, int statusCode)
    {
        return Results.Json(new { ok = false, error }, statusCode: statusCode);
    }

    private static IResult? RequireSameOriginJson(HttpContext context)
    {
        if (!IsSameOriginRequest(context))
        {
            return ApiError("Cross-origin requests are not allowed.", StatusCodes.Status403Forbidden);
        }

        var mediaType = context.Request.ContentType?
            .Split(';', 2, StringSplitOptions.TrimEntries)[0];
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return ApiError("JSON request body required.", StatusCodes.Status415UnsupportedMediaType);
        }

        return null;
    }

    private static IResult? RequireJsonContentType(HttpContext context)
    {
        var mediaType = context.Request.ContentType?
            .Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            ? null
            : ApiError("JSON request body required.", StatusCodes.Status415UnsupportedMediaType);
    }

    private static bool IsSameOriginRequest(HttpContext context)
    {
        var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
        if (fetchSite.Equals("cross-site", StringComparison.OrdinalIgnoreCase)
            || fetchSite.Equals("cross-origin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }

        if (origin.Equals("null", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            || !string.Equals(originUri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var requestPort = context.Request.Host.Port
                          ?? (context.Request.IsHttps ? 443 : 80);
        var originPort = originUri.IsDefaultPort
            ? (originUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80)
            : originUri.Port;
        return requestPort == originPort;
    }

    private static string GetRemoteKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsLocalRequest(HttpContext context)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;
        if (remoteAddress is null)
        {
            return false;
        }

        if (!IPAddress.IsLoopback(remoteAddress))
        {
            return false;
        }

        // A browser can reach a loopback listener through a DNS name whose
        // address is rebound to 127.0.0.1.  In that case Origin and Host can
        // still match even though the page was supplied by an attacker.  The
        // unauthenticated first-run setup therefore accepts only an explicit
        // localhost name or loopback IP literal.
        var host = context.Request.Host.Host;
        var isExplicitLoopbackHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                                     || (IPAddress.TryParse(host, out var hostAddress)
                                         && IPAddress.IsLoopback(hostAddress));
        if (!isExplicitLoopbackHost)
        {
            return false;
        }

        // Do not treat a loopback reverse proxy as proof that the browser is
        // local.  Setup must be performed against the app directly; otherwise
        // an untrusted proxy could forward a remote initialization request.
        return !context.Request.Headers.ContainsKey("Forwarded")
               && !context.Request.Headers.ContainsKey("X-Forwarded-For")
               && !context.Request.Headers.ContainsKey("X-Forwarded-Host")
               && !context.Request.Headers.ContainsKey("X-Forwarded-Proto")
               && !context.Request.Headers.ContainsKey("X-Real-IP")
               && !context.Request.Headers.ContainsKey("X-Original-For")
               && !context.Request.Headers.ContainsKey("X-Original-Host");
    }

    private static void ApplySecurityHeaders(HttpContext context, string nonce)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Content-Security-Policy"] =
            "default-src 'self'; img-src 'self' data:; "
            + $"style-src 'self' 'nonce-{nonce}'; script-src 'self' 'nonce-{nonce}'; "
            + "connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

        if (context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path == "/")
        {
            headers["Cache-Control"] = "no-store";
        }

        // HSTS is only meaningful after a TLS request.  Keeping this
        // conditional lets deployments terminate TLS in front of the app
        // without teaching HTTP clients to cache an insecure policy.
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
    }

    private static async Task<ShoutRequest> ReadRequestAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
        {
            try
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
            catch (InvalidDataException)
            {
                return new ShoutRequest { Message = string.Empty };
            }
        }

        try
        {
            var body = await request.ReadFromJsonAsync<ShoutRequest>();
            return body ?? new ShoutRequest();
        }
        catch (JsonException)
        {
            return new ShoutRequest { Message = string.Empty };
        }
        catch (InvalidOperationException)
        {
            return new ShoutRequest { Message = string.Empty };
        }
        catch (NotSupportedException)
        {
            return new ShoutRequest { Message = string.Empty };
        }
    }

    private static async Task<(bool IsValid, T? Value)> TryReadJsonAsync<T>(HttpRequest request)
    {
        try
        {
            return (true, await request.ReadFromJsonAsync<T>());
        }
        catch (JsonException)
        {
            return (false, default);
        }
        catch (InvalidOperationException)
        {
            return (false, default);
        }
        catch (NotSupportedException)
        {
            return (false, default);
        }
    }

    private static bool IsTruthy(string value)
    {
        return value.Equals("on", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> BuildDisplayUrls()
    {
        var scheme = _httpsCertificate is null ? "http" : "https";
        var urls = new List<string>
        {
            $"{scheme}://localhost:{_port}/",
            $"{scheme}://127.0.0.1:{_port}/"
        };

        if (_httpsCertificate is null && !_allowInsecureHttp)
        {
            return urls.ToArray();
        }

        try
        {
            var host = Dns.GetHostName();
            var addresses = Dns.GetHostEntry(host).AddressList
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .Where(address => !IPAddress.IsLoopback(address))
                .Select(address => $"{scheme}://{address}:{_port}/");

            urls.AddRange(addresses);
        }
        catch
        {
            // Local URLs above are still usable.
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static X509Certificate2? LoadHttpsCertificate()
    {
        var certificatePath = Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_HTTPS_CERT_PATH");
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            return null;
        }

        certificatePath = Path.GetFullPath(certificatePath);
        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException("Configured HTTPS certificate was not found.", certificatePath);
        }

        var password = Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_HTTPS_CERT_PASSWORD");
        try
        {
            return new X509Certificate2(
                certificatePath,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Configured HTTPS certificate could not be loaded.",
                ex);
        }
    }

    private static bool IsEnvironmentTruthy(string? value)
    {
        return value is not null
               && (value.Equals("1", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("on", StringComparison.OrdinalIgnoreCase));
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
