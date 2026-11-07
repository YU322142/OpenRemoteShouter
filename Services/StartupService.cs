using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace RemoteShouter.Services;

/// <summary>Manages per-user startup entries without administrator permissions.</summary>
public static class StartupService
{
    private const string WindowsRunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string WindowsValueName = "OpenRemoteShouter";
    private const string LinuxDesktopFileName = "open-remote-shouter.desktop";
    private const string MacLaunchAgentFileName = "com.openremoteshouter.plist";

    public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public static bool IsEnabled
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey(WindowsRunKey, writable: false);
                return !string.IsNullOrWhiteSpace(key?.GetValue(WindowsValueName) as string);
            }

            return File.Exists(GetStartupFilePath());
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("当前平台不支持开机自启动设置。");
        }

        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.CreateSubKey(WindowsRunKey, writable: true)
                           ?? throw new InvalidOperationException("无法打开当前用户的 Windows 启动项。");
            if (enabled)
            {
                key.SetValue(WindowsValueName, BuildWindowsCommand(), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(WindowsValueName, throwOnMissingValue: false);
            }

            return;
        }

        var path = GetStartupFilePath();
        if (!enabled)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        var directory = Path.GetDirectoryName(path)
                        ?? throw new InvalidOperationException("无法确定开机自启动配置目录。");
        Directory.CreateDirectory(directory);
        var content = OperatingSystem.IsLinux() ? BuildLinuxDesktopEntry() : BuildMacLaunchAgent();
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        TryApplyPrivateFilePermissions(path);
    }

    public static bool IsAutostartupInvocation(string[] args)
    {
        return args.Any(argument => argument.Equals("--autostartup", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildWindowsCommand()
    {
        var (program, arguments) = GetLaunchParts();
        return string.Join(' ', new[] { program }.Concat(arguments).Select(QuoteCommandLineArgument));
    }

    private static string BuildLinuxDesktopEntry()
    {
        var (program, arguments) = GetLaunchParts();
        return "[Desktop Entry]\n"
               + "Type=Application\n"
               + "Name=OpenRemoteShouter\n"
               + $"Exec={QuoteDesktopArgument(program)}{string.Concat(arguments.Select(argument => " " + QuoteDesktopArgument(argument)))}\n"
               + "Terminal=false\n"
               + "X-GNOME-Autostart-enabled=true\n";
    }

    private static string BuildMacLaunchAgent()
    {
        var (program, arguments) = GetLaunchParts();
        var values = new[] { program }.Concat(arguments)
            .Select(value => $"    <string>{SecurityElement.Escape(value)}</string>");
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
               + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
               + "<plist version=\"1.0\"><dict>\n"
               + "  <key>Label</key><string>com.openremoteshouter</string>\n"
               + "  <key>ProgramArguments</key><array>\n"
               + string.Join('\n', values)
               + "\n  </array>\n  <key>RunAtLoad</key><true/>\n</dict></plist>\n";
    }

    private static (string Program, string[] Arguments) GetLaunchParts()
    {
        var processPath = Environment.ProcessPath
                          ?? throw new InvalidOperationException("无法确定 OpenRemoteShouter 的启动路径。");
        if (OperatingSystem.IsLinux()
            && RuntimeInformation.ProcessArchitecture == Architecture.LoongArch64
            && string.Equals(processPath, Path.Combine(AppContext.BaseDirectory, "dotnet"), StringComparison.Ordinal))
        {
            // The Old World package's script sets the runtime and native library paths.
            var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "run.sh"));
            if (!File.Exists(script))
            {
                throw new InvalidOperationException("未找到龙芯发布包的 run.sh，无法配置开机自启动。");
            }

            return (script, ["--foreground", "--autostartup"]);
        }

        if (!Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return (processPath, ["--autostartup"]);
        }

        var dllPath = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
        {
            throw new InvalidOperationException("无法找到便携版的 OpenRemoteShouter.dll。");
        }

        return (processPath, [dllPath, "--autostartup"]);
    }

    private static string GetStartupFilePath()
    {
        if (OperatingSystem.IsLinux())
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrWhiteSpace(configHome) || !Path.IsPathFullyQualified(configHome))
            {
                configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }

            return Path.Combine(configHome, "autostart", LinuxDesktopFileName);
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", MacLaunchAgentFileName);
    }

    private static string QuoteCommandLineArgument(string value)
    {
        var builder = new StringBuilder("\"");
        var slashCount = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                slashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', slashCount * 2 + 1).Append('"');
            }
            else
            {
                builder.Append('\\', slashCount).Append(character);
            }

            slashCount = 0;
        }

        builder.Append('\\', slashCount * 2).Append('"');
        return builder.ToString();
    }

    private static string QuoteDesktopArgument(string value)
    {
        var quotedValue = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
        // Desktop entry string unescaping runs before the Exec parser.
        return "\"" + quotedValue.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("%", "%%", StringComparison.Ordinal) + "\"";
    }

    private static void TryApplyPrivateFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
