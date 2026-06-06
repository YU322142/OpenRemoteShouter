namespace RemoteShouter.Services;

public static class AppLogService
{
    private static readonly object Lock = new();

    public static string LogFilePath => Path.Combine(GetDataDirectory(), "OpenRemoteShouter.log");

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
        try
        {
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}";
            lock (Lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
            // Logging must never break the shout path.
        }
    }

    private static string GetDataDirectory()
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
