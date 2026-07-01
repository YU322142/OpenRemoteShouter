namespace RemoteShouter.Services;

public static class AppLogService
{
    private static readonly object Lock = new();
    private const string DataDirectoryEnvironmentVariable = "OPEN_REMOTE_SHOUTER_DATA_DIR";
    private const string LogFileEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_FILE";
    private const string ConsoleLogEnvironmentVariable = "OPEN_REMOTE_SHOUTER_LOG_CONSOLE";

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
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
        var wroteFile = false;

        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllText(LogFilePath, line);
                wroteFile = true;
            }
        }
        catch
        {
            // Logging must never break the shout path.
        }

        if (ShouldMirrorToConsole || !wroteFile)
        {
            WriteToConsole(line);
        }
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
