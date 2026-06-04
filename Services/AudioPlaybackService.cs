using System.Diagnostics;
using NAudio.Wave;

namespace RemoteShouter.Services;

public sealed class AudioPlaybackService
{
    private static readonly string[] PlayerCandidates =
    [
        "ffplay",
        "mpv",
        "mpg123",
        "cvlc",
        "vlc"
    ];

    public async Task PlayAsync(string filePath, float volume, CancellationToken cancellationToken)
    {
        var player = FindSystemPlayer();
        if (player is not null)
        {
            await PlayWithExternalPlayerAsync(player, filePath, volume, cancellationToken);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            await PlayWithNAudioAsync(filePath, volume, cancellationToken);
            return;
        }

        throw new PlatformNotSupportedException(
            "No audio player was found. On Linux, install ffmpeg(ffplay), mpv, or mpg123.");
    }

    public static string? FindSystemPlayer()
    {
        return PlayerCandidates
            .Select(FindOnPath)
            .FirstOrDefault(path => path is not null);
    }

    public static string GetPlaybackBackendDescription()
    {
        var externalPlayer = FindSystemPlayer();
        if (externalPlayer is not null)
        {
            return externalPlayer;
        }

        return OperatingSystem.IsWindows()
            ? "Windows NAudio playback"
            : "No player found. Install ffmpeg(ffplay), mpv, or mpg123.";
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

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start audio player: {playerPath}");

        try
        {
            await process.WaitForExitAsync(cancellationToken);
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
            case "ffplay":
                startInfo.ArgumentList.Add("-nodisp");
                startInfo.ArgumentList.Add("-autoexit");
                startInfo.ArgumentList.Add("-loglevel");
                startInfo.ArgumentList.Add("quiet");
                startInfo.ArgumentList.Add("-volume");
                startInfo.ArgumentList.Add(percentVolume.ToString());
                startInfo.ArgumentList.Add(filePath);
                break;
            case "mpv":
                startInfo.ArgumentList.Add("--no-video");
                startInfo.ArgumentList.Add("--really-quiet");
                startInfo.ArgumentList.Add($"--volume={percentVolume}");
                startInfo.ArgumentList.Add(filePath);
                break;
            case "mpg123":
                startInfo.ArgumentList.Add("-q");
                startInfo.ArgumentList.Add("-f");
                startInfo.ArgumentList.Add(Math.Clamp((int)(volume * 32768), 0, 32768).ToString());
                startInfo.ArgumentList.Add(filePath);
                break;
            case "cvlc":
            case "vlc":
                startInfo.ArgumentList.Add("--intf");
                startInfo.ArgumentList.Add("dummy");
                startInfo.ArgumentList.Add("--play-and-exit");
                startInfo.ArgumentList.Add("--quiet");
                startInfo.ArgumentList.Add("--gain");
                startInfo.ArgumentList.Add(volume.ToString("0.00"));
                startInfo.ArgumentList.Add(filePath);
                break;
            default:
                startInfo.ArgumentList.Add(filePath);
                break;
        }
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
}
