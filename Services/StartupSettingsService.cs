using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

/// <summary>Stores settings in an application-selected file, independently of launchers and system environment variables.</summary>
public sealed class StartupSettingsService
{
    private const string Prefix = "OPEN_REMOTE_SHOUTER_";
    private const int MaximumSettingsBytes = 64 * 1024;
    private const int MaximumLocationBytes = 8 * 1024;
    private static readonly string[] PropertyNames =
    [
        "allowLan", "trustedProxyIps", "allowTrustedProxySetup", "trustedProxySetupToken",
        "httpsCertificatePath", "httpsCertificatePassword", "requireHttps"
    ];
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly bool _useLocationFile;
    private byte[]? _loadedBytes;
    private byte[]? _loadedLocationBytes;
    private bool _hasLoaded;

    public string SettingsPath { get; private set; }
    public string DefaultSettingsPath { get; }
    public string LocationFilePath { get; }
    public string BackupPath => SettingsPath + ".bak";

    // An explicit settingsPath bypasses location metadata, useful for isolated callers and verification.
    public StartupSettingsService(string? settingsPath = null, string? appDirectory = null)
    {
        var directory = Path.GetFullPath(appDirectory ?? AppContext.BaseDirectory);
        DefaultSettingsPath = Path.Combine(directory, "startup-settings.json");
        LocationFilePath = Path.Combine(directory, "startup-settings-location.json");
        _useLocationFile = settingsPath is null;
        SettingsPath = settingsPath is not null
            ? Path.GetFullPath(settingsPath)
            : ResolveLocation(ReadSnapshot(LocationFilePath, MaximumLocationBytes));
        RejectMetadataDestination(SettingsPath);
    }

    public StartupSettings Load()
    {
        var locationBytes = _useLocationFile ? ReadSnapshot(LocationFilePath, MaximumLocationBytes) : null;
        var path = _useLocationFile ? ResolveLocation(locationBytes) : SettingsPath;
        var bytes = ReadSnapshot(path, MaximumSettingsBytes);
        if (bytes is null && locationBytes is not null)
        {
            throw new InvalidOperationException($"找不到已选择的启动配置：{path}。请恢复该文件，或移走程序目录中的 startup-settings-location.json 后重新配置。");
        }

        var settings = bytes is null ? ReadEnvironmentSettings() : ParseSettings(bytes);
        SettingsPath = path;
        _loadedBytes = bytes;
        _loadedLocationBytes = locationBytes;
        _hasLoaded = true;
        return settings;
    }

    public void Save(StartupSettings settings, string? settingsPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!_hasLoaded)
        {
            Load();
        }

        var destination = Path.GetFullPath(settingsPath ?? SettingsPath);
        RejectMetadataDestination(destination);
        var normalized = Validate(settings, destination);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(normalized, JsonOptions);
        if (bytes.Length > MaximumSettingsBytes)
        {
            throw new InvalidOperationException("启动设置不能超过 64 KiB，请缩短配置内容。");
        }

        EnsureLoadedFilesUnchanged();
        var sameDestination = PathsEqual(destination, SettingsPath);
        var destinationBytes = sameDestination ? _loadedBytes : ReadSnapshot(destination, MaximumSettingsBytes);
        try
        {
            WriteAtomic(destination, bytes, destinationBytes, MaximumSettingsBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                "无法保存所选目录中的启动设置，请检查目录及配置、备份文件的写入权限。配置不会改存到其他位置。", ex);
        }

        byte[]? newLocationBytes = _loadedLocationBytes;
        if (_useLocationFile && (!PathsEqual(destination, DefaultSettingsPath) || _loadedLocationBytes is not null))
        {
            try
            {
                if (!sameDestination)
                {
                    EnsureSnapshot(SettingsPath, _loadedBytes, MaximumSettingsBytes);
                }

                // Keep the default portable when the entire application directory is moved.
                var storedPath = PathsEqual(destination, DefaultSettingsPath) ? "startup-settings.json" : destination;
                newLocationBytes = JsonSerializer.SerializeToUtf8Bytes(new { settingsPath = storedPath }, JsonOptions);
                WriteAtomic(LocationFilePath, newLocationBytes, _loadedLocationBytes, MaximumLocationBytes);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"配置已保存到 {destination}，但程序目录中的保存位置记录未能更新。原配置文件仍保留，请检查程序目录权限并重新打开设置后重试。", ex);
            }
        }

        SettingsPath = destination;
        _loadedBytes = bytes;
        _loadedLocationBytes = newLocationBytes;
    }

    /// <summary>Call before creating services. Updates only this process's environment.</summary>
    public void ApplySavedSettingsToEnvironment()
    {
        var settings = Load();
        if (_loadedBytes is null)
        {
            return;
        }

        settings = Validate(settings, SettingsPath);
        var values = new (string Name, string Value)[]
        {
            ("ALLOW_LAN", settings.AllowLan ? "1" : "0"),
            ("TRUSTED_PROXY_IPS", settings.TrustedProxyIps),
            ("ALLOW_TRUSTED_PROXY_SETUP", settings.AllowTrustedProxySetup ? "1" : "0"),
            ("TRUSTED_PROXY_SETUP_TOKEN", settings.TrustedProxySetupToken),
            ("HTTPS_CERT_PATH", settings.HttpsCertificatePath),
            ("HTTPS_CERT_PASSWORD", settings.HttpsCertificatePassword),
            ("REQUIRE_HTTPS", settings.RequireHttps ? "1" : "0")
        };
        foreach (var (name, value) in values)
        {
            Environment.SetEnvironmentVariable(Prefix + name, value.Length == 0 ? null : value, EnvironmentVariableTarget.Process);
        }
    }

    public static string GenerateSetupToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static StartupSettings Validate(StartupSettings settings, string settingsPath)
    {
        var strings = new[]
        {
            settings.TrustedProxyIps, settings.TrustedProxySetupToken,
            settings.HttpsCertificatePath, settings.HttpsCertificatePassword
        };
        if (strings.Any(value => value is null || value.Contains('\0')))
        {
            throw new InvalidOperationException("启动设置的文本值不能为空值或包含 NUL 字符。");
        }

        if (strings.Any(value => value.Length > 32766))
        {
            throw new InvalidOperationException("单项启动设置超过了进程环境变量的长度限制。");
        }

        var proxyIps = settings.TrustedProxyIps.Trim();
        var proxyTokens = proxyIps.Split(
            [',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (proxyTokens.Length > 32)
        {
            throw new InvalidOperationException("可信中转 IP 最多可以填写 32 个。");
        }

        foreach (var token in proxyTokens)
        {
            var addressText = token.Length >= 2 && token[0] == '[' && token[^1] == ']'
                ? token[1..^1]
                : token;
            if (!IPAddress.TryParse(addressText, out var address))
            {
                throw new InvalidOperationException($"可信中转 IP“{token}”无效，请填写具体 IP 地址。");
            }

            address = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new InvalidOperationException("可信中转 IP 不能使用 0.0.0.0 或 :: 等通配地址。");
            }
        }

        if (settings.AllowTrustedProxySetup)
        {
            if (proxyTokens.Length == 0)
            {
                throw new InvalidOperationException("允许远程首次初始化时，必须填写至少一个可信中转 IP。");
            }

            var tokenLength = Encoding.UTF8.GetByteCount(settings.TrustedProxySetupToken);
            if (string.IsNullOrWhiteSpace(settings.TrustedProxySetupToken) || tokenLength is < 32 or > 512)
            {
                throw new InvalidOperationException("远程初始化令牌必须包含 32 至 512 个 UTF-8 字节，可以点击“生成随机令牌”。");
            }
        }

        var certificatePath = settings.HttpsCertificatePath.Trim();
        if (certificatePath.Length == 0)
        {
            if (settings.RequireHttps)
            {
                throw new InvalidOperationException("强制 HTTPS 时，必须配置可用的 PFX 证书。");
            }
        }
        else
        {
            try
            {
                certificatePath = Path.GetFullPath(certificatePath, Path.GetDirectoryName(settingsPath)!);
                using var certificate = new X509Certificate2(
                    certificatePath, settings.HttpsCertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
                if (!certificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("HTTPS PFX 证书必须包含私钥。");
                }
            }
            catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new InvalidOperationException("无法读取 HTTPS PFX 证书，请检查证书路径和密码。", ex);
            }
        }

        return settings with { TrustedProxyIps = proxyIps, HttpsCertificatePath = certificatePath };
    }

    private string ResolveLocation(byte[]? bytes)
    {
        if (bytes is null)
        {
            return DefaultSettingsPath;
        }

        try
        {
            using var document = ParseDocument(bytes);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1
                || !root.TryGetProperty("settingsPath", out var property) || property.ValueKind != JsonValueKind.String)
            {
                throw new JsonException();
            }

            var path = property.GetString();
            if (string.IsNullOrWhiteSpace(path) || path.Contains('\0'))
            {
                throw new JsonException();
            }

            if (path == "startup-settings.json")
            {
                return DefaultSettingsPath;
            }

            if (!Path.IsPathFullyQualified(path))
            {
                throw new JsonException();
            }

            path = Path.GetFullPath(path);
            RejectMetadataDestination(path);
            return path;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                "程序目录中的 startup-settings-location.json 格式无效，请恢复该文件的 .bak 备份，或移走该位置记录后重新配置。", ex);
        }
    }

    private static StartupSettings ParseSettings(byte[] bytes)
    {
        try
        {
            using var document = ParseDocument(bytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException();
            }

            var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!PropertyNames.Contains(property.Name, StringComparer.Ordinal)
                    || !properties.TryAdd(property.Name, property.Value))
                {
                    throw new JsonException();
                }
            }

            if (properties.Count != PropertyNames.Length)
            {
                throw new JsonException();
            }

            foreach (var name in PropertyNames)
            {
                var kind = properties[name].ValueKind;
                var isBoolean = name is "allowLan" or "allowTrustedProxySetup" or "requireHttps";
                if (isBoolean ? kind is not (JsonValueKind.True or JsonValueKind.False) : kind != JsonValueKind.String)
                {
                    throw new JsonException();
                }
            }

            var settings = document.RootElement.Deserialize<StartupSettings>(JsonOptions) ?? throw new JsonException();
            if (new[] { settings.TrustedProxyIps, settings.TrustedProxySetupToken, settings.HttpsCertificatePath, settings.HttpsCertificatePassword }
                .Any(value => value.Contains('\0')))
            {
                throw new JsonException();
            }

            return settings;
        }
        catch (JsonException ex)
        {
            // Keep source JSON and secret values out of diagnostics.
            throw new InvalidOperationException(
                "startup-settings.json 格式无效。请恢复同目录的 .bak 备份，或移走该配置及程序目录中的保存位置记录后重新配置。", ex);
        }
    }

    private static JsonDocument ParseDocument(byte[] bytes)
    {
        var payload = bytes.AsMemory();
        if (payload.Span.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
        {
            payload = payload[3..];
        }

        return JsonDocument.Parse(payload);
    }

    private static byte[]? ReadSnapshot(string path, int maximumBytes)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > maximumBytes)
            {
                throw new InvalidOperationException($"配置文件 {path} 超过允许的大小，请检查文件内容。");
            }

            var bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"无法读取启动配置：{path}。请检查文件权限。", ex);
        }
    }

    private void EnsureLoadedFilesUnchanged()
    {
        EnsureSnapshot(SettingsPath, _loadedBytes, MaximumSettingsBytes);
        if (_useLocationFile)
        {
            EnsureSnapshot(LocationFilePath, _loadedLocationBytes, MaximumLocationBytes);
        }
    }

    private static void EnsureSnapshot(string path, byte[]? expected, int maximumBytes)
    {
        var current = ReadSnapshot(path, maximumBytes);
        if (current is null != (expected is null)
            || (current is not null && !current.AsSpan().SequenceEqual(expected)))
        {
            throw new InvalidOperationException("启动配置或保存位置在打开设置后已被其他程序修改，请关闭并重新打开设置后再保存。");
        }
    }

    private static void WriteAtomic(string path, byte[] bytes, byte[]? expected, int maximumBytes)
    {
        EnsureSnapshot(path, expected, maximumBytes);
        var directory = Path.GetDirectoryName(path)!;
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var temporaryPath = Path.Combine(directory, $".startup-settings-{Guid.NewGuid():N}.tmp");
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var stream = new FileStream(temporaryPath, options))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            EnsureSnapshot(path, expected, maximumBytes);
            if (expected is null)
            {
                File.Move(temporaryPath, path);
            }
            else
            {
                MakePrivate(path);
                File.Replace(temporaryPath, path, path + ".bak");
                MakePrivate(path + ".bak");
            }

            MakePrivate(path);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Temporary files already have private permissions.
            }
        }
    }

    private void RejectMetadataDestination(string path)
    {
        if (PathsEqual(path, LocationFilePath) || PathsEqual(path, LocationFilePath + ".bak"))
        {
            throw new InvalidOperationException("启动配置文件不能覆盖程序的保存位置记录或其备份。");
        }
    }

    private static bool PathsEqual(string left, string right) => string.Equals(
        Path.GetFullPath(left), Path.GetFullPath(right), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static void MakePrivate(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static StartupSettings ReadEnvironmentSettings()
    {
        static string Get(string suffix) => Environment.GetEnvironmentVariable(Prefix + suffix) ?? string.Empty;
        var allowLan = ParseBoolean(Get("ALLOW_LAN"))
            ?? ParseBoolean(Get("ALLOW_DIRECT_IP"))
            ?? IsTruthy(Get("ALLOW_INSECURE_HTTP"));
        return new StartupSettings(
            allowLan, Get("TRUSTED_PROXY_IPS"), IsTruthy(Get("ALLOW_TRUSTED_PROXY_SETUP")),
            Get("TRUSTED_PROXY_SETUP_TOKEN"), Get("HTTPS_CERT_PATH"), Get("HTTPS_CERT_PASSWORD"), IsTruthy(Get("REQUIRE_HTTPS")));
    }

    private static bool? ParseBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" or "on" or "enable" or "enabled" => true,
        "0" or "false" or "no" or "off" or "disable" or "disabled" => false,
        _ => null
    };

    private static bool IsTruthy(string value) => value.Equals("1", StringComparison.OrdinalIgnoreCase)
        || value.Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
        || value.Equals("on", StringComparison.OrdinalIgnoreCase);
}
