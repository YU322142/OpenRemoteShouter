using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using RemoteShouter.Services;

namespace RemoteShouter;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                AppLogService.Error("Unhandled application exception", ex);
            }
            else
            {
                AppLogService.Error($"Unhandled application exception: {eventArgs.ExceptionObject}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            AppLogService.Error("Unobserved task exception", eventArgs.Exception);
            eventArgs.SetObserved();
        };

        AppLogService.Info(
            $"Application starting. os={Environment.OSVersion}, processPath={Environment.ProcessPath}, logFile={AppLogService.LogFilePath}");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            AppLogService.Info("Application exited.");
        }
        catch (Exception ex)
        {
            AppLogService.Error("Application crashed", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        ConfigureLinuxX11Environment();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(BuildX11PlatformOptions())
            .WithInterFont()
            .LogToTrace();
    }

    private static void ConfigureLinuxX11Environment()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        if (ShouldForceSoftwareRendering())
        {
            SetEnvironmentVariableIfEmpty("LIBGL_ALWAYS_SOFTWARE", "1");
            SetEnvironmentVariableIfEmpty("GALLIUM_DRIVER", "llvmpipe");
            SetEnvironmentVariableIfEmpty("AVALONIA_RENDERING_FORCE_SOFTWARE", "1");
        }

        if (!ShouldEnableLinuxIme())
        {
            Environment.SetEnvironmentVariable("XMODIFIERS", "@im=none");
            Environment.SetEnvironmentVariable("GTK_IM_MODULE", "xim");
            Environment.SetEnvironmentVariable("QT_IM_MODULE", "xim");
        }
    }

    private static X11PlatformOptions BuildX11PlatformOptions()
    {
        var options = new X11PlatformOptions
        {
            EnableIme = ShouldEnableLinuxIme(),
            UseDBusMenu = !IsLoongArch64(),
            UseDBusFilePicker = !IsLoongArch64()
        };

        if (ShouldForceSoftwareRendering())
        {
            options.RenderingMode = [X11RenderingMode.Software];
            options.ShouldRenderOnUIThread = true;
            options.UseRetainedFramebuffer = true;
        }

        return options;
    }

    private static bool ShouldForceSoftwareRendering()
    {
        var parsed = ParseBooleanSwitch(Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_SOFTWARE_RENDERING"));
        return parsed ?? IsLoongArch64();
    }

    private static bool ShouldEnableLinuxIme()
    {
        var parsed = ParseBooleanSwitch(Environment.GetEnvironmentVariable("OPEN_REMOTE_SHOUTER_X11_ENABLE_IME"));
        if (parsed.HasValue)
        {
            return parsed.Value;
        }

        if (!IsLoongArch64())
        {
            return true;
        }

        return HasFcitxDbusService();
    }

    private static bool HasFcitxDbusService()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS")))
        {
            return false;
        }

        return TryFindFcitxWithProcess(
                   "dbus-send",
                   [
                       "--session",
                       "--dest=org.freedesktop.DBus",
                       "--type=method_call",
                       "--print-reply",
                       "/org/freedesktop/DBus",
                       "org.freedesktop.DBus.ListNames"
                   ])
               || TryFindFcitxWithProcess(
                   "busctl",
                   [
                       "--user",
                       "--no-pager",
                       "--no-legend",
                       "list"
                   ]);
    }

    private static bool TryFindFcitxWithProcess(string fileName, string[] arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }.AddArguments(arguments));

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(1500))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            return process.ExitCode == 0
                   && output.Contains("org.fcitx.Fcitx", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static bool? ParseBooleanSwitch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" or "enable" or "enabled" => true,
            "0" or "false" or "no" or "off" or "disable" or "disabled" => false,
            _ => null
        };
    }

    private static bool IsLoongArch64()
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.LoongArch64;
    }

    private static void SetEnvironmentVariableIfEmpty(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo AddArguments(this ProcessStartInfo startInfo, IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
