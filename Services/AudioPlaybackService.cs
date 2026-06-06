using System.Diagnostics;
using System.Globalization;
using NAudio.Wave;

namespace RemoteShouter.Services;

public sealed class AudioPlaybackService
{
    private const string PlayerEnvironmentVariable = "OPEN_REMOTE_SHOUTER_AUDIO_PLAYER";

    private static readonly string[] PlayerCandidates =
    [
        "ffplay",
        "mpv",
        "pw-play",
        "paplay",
        "aplay",
        "cvlc",
        "vlc"
    ];

    public async Task PlayAsync(string filePath, float volume, CancellationToken cancellationToken)
    {
        var audioFile = new FileInfo(filePath);
        AppLogService.Info(
            $"Audio playback requested. file={filePath}, exists={audioFile.Exists}, bytes={(audioFile.Exists ? audioFile.Length : 0)}, volume={volume.ToString("0.00", CultureInfo.InvariantCulture)}");

        if (OperatingSystem.IsWindows())
        {
            AppLogService.Info("Audio backend selected: Windows NAudio playback.");
            await PlayWithNAudioAsync(filePath, volume, cancellationToken);
            return;
        }

        AppLogService.Info(
            $"Audio environment. {FormatEnvironmentValue(PlayerEnvironmentVariable)}, {FormatEnvironmentValue("XDG_RUNTIME_DIR")}, {FormatEnvironmentValue("PULSE_SERVER")}, {FormatEnvironmentValue("PIPEWIRE_RUNTIME_DIR")}, {FormatEnvironmentValue("DBUS_SESSION_BUS_ADDRESS")}");

        var allPlayers = FindSystemPlayers().ToArray();
        var skippedPlayers = allPlayers
            .Where(player => !IsPlayerCompatibleWithFile(player, filePath))
            .ToArray();
        if (skippedPlayers.Length > 0)
        {
            AppLogService.Info(
                $"Audio backend skipped for file type: {string.Join(", ", skippedPlayers.Select(Path.GetFileName))}.");
        }

        var players = allPlayers
            .Where(player => IsPlayerCompatibleWithFile(player, filePath))
            .ToArray();
        AppLogService.Info(
            players.Length == 0
                ? "Audio backend candidates: none."
                : $"Audio backend candidates: {string.Join(", ", players.Select(Path.GetFileName))}.");

        if (players.Length == 0)
        {
            throw new PlatformNotSupportedException(
                "No audio player was found. On Linux, install pulseaudio-utils(paplay), alsa-utils(aplay), ffmpeg(ffplay), or mpv.");
        }

        var failures = new List<string>();
        foreach (var player in players)
        {
            try
            {
                AppLogService.Info($"Trying audio player: {player}");
                await PlayWithExternalPlayerAsync(player, filePath, volume, cancellationToken);
                AppLogService.Info($"Audio player succeeded: {player}");
                return;
            }
            catch (OperationCanceledException)
            {
                AppLogService.Info($"Audio player canceled: {player}");
                throw;
            }
            catch (Exception ex)
            {
                AppLogService.Error($"Audio player failed: {player}", ex);
                failures.Add($"{Path.GetFileName(player)}: {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            "All available audio players failed. " + string.Join(" | ", failures));
    }

    public static string? FindSystemPlayer()
    {
        return FindSystemPlayers().FirstOrDefault();
    }

    public static string GetPlaybackBackendDescription()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows NAudio playback";
        }

        var players = FindSystemPlayers()
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        if (players.Length > 0)
        {
            return $"External players: {string.Join(", ", players)}";
        }

        return "No player found. Install pulseaudio-utils(paplay), alsa-utils(aplay), ffmpeg(ffplay), or mpv.";
    }

    private static string? FindOnPath(string command)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [string.Empty];

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, command + extension);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> FindSystemPlayers()
    {
        var configuredPlayer = Environment.GetEnvironmentVariable(PlayerEnvironmentVariable);
        var candidates = string.IsNullOrWhiteSpace(configuredPlayer)
            ? PlayerCandidates
            : [configuredPlayer, .. PlayerCandidates];

        return candidates
            .Select(ResolvePlayer)
            .Where(path => path is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)!;
    }

    private static string? ResolvePlayer(string commandOrPath)
    {
        if (commandOrPath.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            var fullPath = Path.GetFullPath(commandOrPath);
            return File.Exists(fullPath) ? fullPath : null;
        }

        return FindOnPath(commandOrPath);
    }

    private static bool IsPlayerCompatibleWithFile(string playerPath, string filePath)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var playerName = Path.GetFileNameWithoutExtension(playerPath);
        return !string.Equals(playerName, "aplay", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(playerName, "paplay", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task PlayWithExternalPlayerAsync(
        string playerPath,
        string filePath,
        float volume,
        CancellationToken cancellationToken)
    {
        var playerName = Path.GetFileNameWithoutExtension(playerPath).ToLowerInvariant();
        var startInfo = new ProcessStartInfo
        {
            FileName = playerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        AddPlayerArguments(startInfo, playerName, filePath, volume);
        AppLogService.Info(
            $"Audio player command. player={playerPath}, args={FormatArguments(startInfo.ArgumentList)}");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start audio player: {playerPath}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            AppLogService.Info(
                $"Audio player exited. player={playerPath}, exitCode={process.ExitCode}, stdout={TrimForLog(stdout)}, stderr={TrimForLog(stderr)}");
            if (process.ExitCode != 0)
            {
                var details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new InvalidOperationException(
                    $"Audio player exited with code {process.ExitCode}: {details.Trim()}");
            }
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            throw;
        }
    }

    private static void AddPlayerArguments(
        ProcessStartInfo startInfo,
        string playerName,
        string filePath,
        float volume)
    {
        var percentVolume = Math.Clamp((int)Math.Round(volume * 100), 0, 100);
        switch (playerName)
        {
            case "paplay":
                startInfo.ArgumentList.Add(
                    $"--volume={Math.Clamp((int)(volume * 65536), 0, 65536).ToString(CultureInfo.InvariantCulture)}");
                startInfo.ArgumentList.Add(filePath);
                break;
            case "aplay":
                startInfo.ArgumentList.Add("-q");
                startInfo.ArgumentList.Add(filePath);
                break;
            case "pw-play":
                startInfo.ArgumentList.Add(filePath);
                break;
            case "ffplay":
                startInfo.ArgumentList.Add("-nodisp");
                startInfo.ArgumentList.Add("-autoexit");
                startInfo.ArgumentList.Add("-loglevel");
                startInfo.ArgumentList.Add("error");
                startInfo.ArgumentList.Add("-volume");
                startInfo.ArgumentList.Add(percentVolume.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add(filePath);
                break;
            case "mpv":
                startInfo.ArgumentList.Add("--no-video");
                startInfo.ArgumentList.Add($"--volume={percentVolume.ToString(CultureInfo.InvariantCulture)}");
                startInfo.ArgumentList.Add(filePath);
                break;
            case "cvlc":
            case "vlc":
                startInfo.ArgumentList.Add("--intf");
                startInfo.ArgumentList.Add("dummy");
                startInfo.ArgumentList.Add("--play-and-exit");
                startInfo.ArgumentList.Add("--gain");
                startInfo.ArgumentList.Add(volume.ToString("0.00", CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add(filePath);
                break;
            default:
                startInfo.ArgumentList.Add(filePath);
                break;
        }
    }

    private static string FormatArguments(IEnumerable<string> arguments)
    {
        return string.Join(" ", arguments.Select(QuoteArgument));
    }

    private static string QuoteArgument(string argument)
    {
        return argument.Any(char.IsWhiteSpace)
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }

    private static string FormatEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? $"{name}=<empty>" : $"{name}={value}";
    }

    private static async Task PlayWithNAudioAsync(
        string filePath,
        float volume,
        CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            using var reader = new AudioFileReader(filePath)
            {
                Volume = Math.Clamp(volume, 0.0f, 1.0f)
            };
            using var output = new WaveOutEvent();
            output.Init(reader);

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            output.PlaybackStopped += (_, args) =>
            {
                if (args.Exception is not null)
                {
                    completion.TrySetException(args.Exception);
                }
                else
                {
                    completion.TrySetResult();
                }
            };

            using var registration = cancellationToken.Register(() =>
            {
                output.Stop();
                completion.TrySetCanceled(cancellationToken);
            });

            output.Play();
            await completion.Task;
        }, cancellationToken);
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process may already be gone.
        }
    }

    private static string TrimForLog(string value)
    {
        value = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return value.Length <= 500 ? value : value[..500] + "...";
    }
}
