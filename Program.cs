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
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
