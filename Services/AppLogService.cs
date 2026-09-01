using System.Text;
using System.Globalization;

namespace RemoteShouter.Services;

public static class AppLogService
{
    private static readonly object Lock = new();
    private const string DataDirectoryEnvironmentVariable = "OPEN_REMOTE_SHOUTER_DATA_DIR";
    private const string LogFileEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_FILE";
    private const string ConsoleLogEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_CONSOLE";
    private const string LogMaxBytesEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_MAX_BYTES";
    private const string LogMaxFilesEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_MAX_FILES";
    private const int MaxLogMessageLength = 16 * 1024;
    private const int TruncationMarkerLength = 3;
    private const long DefaultMaxLogBytes = 10L * 1024 * 1024;
    private const long MinimumMaxLogBytes = 256L * 1024;
    private const long MaximumMaxLogBytes = 256L * 1024 * 1024;
    private const int DefaultMaxRotatedLogFiles = 3;
    private const int MinimumMaxRotatedLogFiles = 1;
    private const int MaximumMaxRotatedLogFiles = 10;

    public static string DataDirectory
    {
        get
        {
            var configuredPath = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
            return string.IsNullOrWhiteSpace(configuredPath)
                ? GetDefaultDataDirectory()
                : Path.GetFullPath(configuredPath);
        }
    }

    public static string LogFilePath
    {
        get
        {
            var configuredPath = Environment.GetEnvironmentVariable(LogFileEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            return Path.Combine(DataDirectory, "OpenRemoteShouter.log");
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message}: {exception}");
    }

    private static void Write(string level, string message)
    {
        message = SanitizeMessage(message);
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
        var wroteFile = false;
        var logFilePath = string.Empty;
        var mirrorToConsole = true;

        try
        {
            logFilePath = LogFilePath;
            mirrorToConsole = ShouldMirrorToConsole;
            lock (Lock)
            {
                wroteFile = TryAppendLogLine(logFilePath, line);
            }
        }
        catch
        {
            // Logging must never break the shout path.
        }

        if (mirrorToConsole || !wroteFile)
        {
            WriteToConsole(line);
        }
    }

    private static bool TryAppendLogLine(string path, string line)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (IsReparsePoint(path))
        {
            // Check before File.Exists as well: a dangling symlink reports
            // false from File.Exists but would still be followed by append.
            return false;
        }

        var maxBytes = GetMaxLogBytes();
        var lineBytes = Encoding.UTF8.GetByteCount(line);
        if (lineBytes > maxBytes)
        {
            return false;
        }

        if (File.Exists(path))
        {
            if (IsReparsePoint(path))
            {
                // Never follow a log-file symlink or other reparse point.
                return false;
            }

            var currentLength = new FileInfo(path).Length;
            if (currentLength > maxBytes - lineBytes && !RotateLog(path, GetMaxRotatedLogFiles()))
            {
                return false;
            }
        }

        File.AppendAllText(path, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        TryApplyPrivateFilePermissions(path);
        return true;
    }

    private static bool RotateLog(string path, int maxRotatedFiles)
    {
        if (!File.Exists(path) || IsReparsePoint(path))
        {
            return !File.Exists(path);
        }

        var oldestPath = $"{path}.{maxRotatedFiles}";
        if (!TryDeleteRegularFile(oldestPath))
        {
            return false;
        }

        for (var index = maxRotatedFiles - 1; index >= 1; index--)
        {
            var sourcePath = $"{path}.{index}";
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var destinationPath = $"{path}.{index + 1}";
            if (IsReparsePoint(sourcePath) || IsReparsePoint(destinationPath))
            {
                return false;
            }

            File.Move(sourcePath, destinationPath, overwrite: true);
            TryApplyPrivateFilePermissions(destinationPath);
        }

        var firstRotatedPath = $"{path}.1";
        if (IsReparsePoint(firstRotatedPath))
        {
            return false;
        }

        File.Move(path, firstRotatedPath, overwrite: true);
        TryApplyPrivateFilePermissions(firstRotatedPath);
        return true;
    }

    private static bool TryDeleteRegularFile(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        if (IsReparsePoint(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static long GetMaxLogBytes()
    {
        var configured = Environment.GetEnvironmentVariable(LogMaxBytesEnvironmentVariable);
        return long.TryParse(
                   configured,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value >= MinimumMaxLogBytes
            ? Math.Min(value, MaximumMaxLogBytes)
            : DefaultMaxLogBytes;
    }

    private static int GetMaxRotatedLogFiles()
    {
        var configured = Environment.GetEnvironmentVariable(LogMaxFilesEnvironmentVariable);
        return int.TryParse(
                   configured,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value >= MinimumMaxRotatedLogFiles
            ? Math.Min(value, MaximumMaxRotatedLogFiles)
            : DefaultMaxRotatedLogFiles;
    }

    private static void TryApplyPrivateFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best-effort hardening only.
        }
    }

    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(message.Length, MaxLogMessageLength));
        foreach (var character in message)
        {
            var encoded = character switch
            {
                '\r' or '\n' => "\\n",
                _ when char.IsControl(character) => "\\u" + ((int)character).ToString("X4", CultureInfo.InvariantCulture),
                _ => character.ToString()
            };

            if (builder.Length + encoded.Length + TruncationMarkerLength > MaxLogMessageLength)
            {
                AppendTruncationMarker(builder);
                break;
            }

            builder.Append(encoded);
        }

        return builder.ToString();
    }

    private static void AppendTruncationMarker(StringBuilder builder)
    {
        var remaining = MaxLogMessageLength - builder.Length;
        if (remaining <= 0)
        {
            return;
        }

        builder.Append(remaining >= TruncationMarkerLength
            ? "..."
            : new string('.', remaining));
    }

    private static bool ShouldMirrorToConsole
    {
        get
        {
            var configuredConsole = Environment.GetEnvironmentVariable(ConsoleLogEnvironmentVariable);
            if (IsTruthy(configuredConsole))
            {
                return true;
            }

            return !OperatingSystem.IsWindows()
                   && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LogFileEnvironmentVariable));
        }
    }

    private static void WriteToConsole(string line)
    {
        try
        {
            Console.Error.Write(line);
        }
        catch
        {
            // Logging must never break the shout path.
        }
    }

    private static bool IsTruthy(string? value)
    {
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDefaultDataDirectory()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
        {
            localData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        return Path.Combine(localData, "OpenRemoteShouter");
    }
}
