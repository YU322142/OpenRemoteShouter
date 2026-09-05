using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using RemoteShouter.Models;

namespace RemoteShouter.Services;

public sealed class SpeechQueueService
{
    private const string CacheMaxBytesEnvironmentVariable = "OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_BYTES";
    private const string CacheMaxFilesEnvironmentVariable = "OPEN_REMOTE_SHOUTER_TTS_CACHE_MAX_FILES";
    private const long DefaultMaxCacheBytes = 256L * 1024 * 1024;
    private const long MinimumMaxCacheBytes = 16L * 1024 * 1024;
    private const long MaximumMaxCacheBytes = 2L * 1024 * 1024 * 1024;
    private const int DefaultMaxCacheFiles = 512;
    private const int MinimumMaxCacheFiles = 16;
    private const int MaximumMaxCacheFiles = 4096;
    private static readonly object CacheLock = new();

    private readonly EdgeTtsClient _edgeTtsClient = new();
    private readonly AudioPlaybackService _audioPlaybackService = new();
    private readonly Queue<SpeechWorkItem> _queue = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _currentCancellation;
    private bool _isProcessing;

    public string? LastError { get; private set; }

    public Task SpeakLatestAsync(
        string text,
        string voiceName,
        int rate,
        float volume,
        Action<TimeSpan>? durationAvailable = null,
        Action? speechFailed = null)
    {
        var normalized = NormalizeSpeechText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.CompletedTask;
        }

        StopInternal();

        // Keep all downstream work on the fixed voice allow-list.  Besides
        // matching the TTS client, this prevents untrusted text from reaching
        // logs, cache keys, or any future filesystem path construction.
        var safeVoiceName = EdgeTtsClient.AvailableVoices
            .FirstOrDefault(voice => string.Equals(voice.ShortName, voiceName, StringComparison.OrdinalIgnoreCase))
            ?.ShortName
            ?? ShoutRequest.DefaultVoiceName;
        var workItem = new SpeechWorkItem(normalized, safeVoiceName, rate, volume, durationAvailable, speechFailed);
        lock (_lock)
        {
            _queue.Enqueue(workItem);
            if (_isProcessing)
            {
                return Task.CompletedTask;
            }

            _isProcessing = true;
            _ = ProcessQueueAsync();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        StopInternal();
        return Task.CompletedTask;
    }

    private void StopInternal()
    {
        lock (_lock)
        {
            _queue.Clear();
            _currentCancellation?.Cancel();
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            SpeechWorkItem workItem;
            CancellationTokenSource cts;

            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }

                workItem = _queue.Dequeue();
                cts = new CancellationTokenSource();
                _currentCancellation = cts;
            }

            try
            {
                LastError = null;
                AppLogService.Info(
                    $"Speech started. voice={workItem.VoiceName}, format={EdgeTtsClient.CurrentOutputFormat}, rate={workItem.Rate}, volume={workItem.Volume.ToString("0.00", CultureInfo.InvariantCulture)}, length={workItem.Text.Length}");
                var filePath = await EnsureSpeechCacheAsync(workItem, cts.Token);
                var duration = AudioPlaybackService.TryGetDuration(filePath);
                if (duration is not null && workItem.DurationAvailable is not null)
                {
                    try
                    {
                        workItem.DurationAvailable(duration.Value);
                    }
                    catch (Exception ex)
                    {
                        AppLogService.Error("Speech duration callback failed", ex);
                    }
                }
                else if (duration is null)
                {
                    try
                    {
                        workItem.SpeechFailed?.Invoke();
                    }
                    catch (Exception callbackError)
                    {
                        AppLogService.Error("Speech duration fallback callback failed", callbackError);
                    }
                }
                await _audioPlaybackService.PlayAsync(filePath, workItem.Volume, cts.Token);
                AppLogService.Info("Speech completed.");
            }
            catch (OperationCanceledException)
            {
                AppLogService.Info("Speech canceled.");
                // Expected when a newer shout replaces the current one.
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppLogService.Error("Speech failed", ex);
                try
                {
                    workItem.SpeechFailed?.Invoke();
                }
                catch (Exception callbackError)
                {
                    AppLogService.Error("Speech failure callback failed", callbackError);
                }
            }
            finally
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_currentCancellation, cts))
                    {
                        _currentCancellation = null;
                    }
                }

                cts.Dispose();
            }
        }
    }

    private async Task<string> EnsureSpeechCacheAsync(
        SpeechWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var cacheDirectory = GetCacheDirectory();
        if (IsReparsePoint(cacheDirectory))
        {
            throw new InvalidDataException("Speech cache directory must not be a reparse point.");
        }

        EnforceCacheQuota(cacheDirectory, protectedPath: null);
        var outputFormat = EdgeTtsClient.CurrentOutputFormat;
        var cachePath = GetCachePath(workItem, cacheDirectory, outputFormat);
        if (IsReparsePoint(cachePath))
        {
            throw new InvalidDataException("Speech cache contains a reparse point.");
        }

        if (File.Exists(cachePath))
        {
            var existingLength = new FileInfo(cachePath).Length;
            if (existingLength > 0 && existingLength <= EdgeTtsClient.MaximumAudioBytes)
            {
                AppLogService.Info($"Speech cache hit. path={cachePath}, bytes={existingLength}");
                return cachePath;
            }

            File.Delete(cachePath);
        }

        Directory.CreateDirectory(cacheDirectory);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(18));

        var audio = await _edgeTtsClient.SynthesizeAsync(
            workItem.Text,
            workItem.VoiceName,
            outputFormat,
            workItem.Rate,
            workItem.Volume,
            timeout.Token);

        if (audio.Length == 0 || audio.LongLength > EdgeTtsClient.MaximumAudioBytes)
        {
            throw new InvalidDataException("Speech service returned an audio payload that is too large.");
        }

        var tempPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, audio, cancellationToken);
            TryApplyPrivateFilePermissions(tempPath);

            try
            {
                File.Move(tempPath, cachePath, overwrite: false);
            }
            catch (IOException) when (File.Exists(cachePath) && !IsReparsePoint(cachePath))
            {
                // Another local instance may have populated the same digest.
                // Keep the first complete file and discard our duplicate.
            }
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // Best-effort cleanup of an interrupted cache write.
            }
        }

        EnforceCacheQuota(cacheDirectory, cachePath);
        AppLogService.Info($"Speech cache written. path={cachePath}, bytes={audio.Length}");
        return cachePath;
    }

    private static string GetCachePath(
        SpeechWorkItem workItem,
        string cacheDirectory,
        string outputFormat)
    {
        var data = Encoding.UTF8.GetBytes(
            string.Join(
                '\n',
                workItem.VoiceName,
                outputFormat,
                workItem.Rate.ToString(CultureInfo.InvariantCulture),
                workItem.Volume.ToString("0.00", CultureInfo.InvariantCulture),
                workItem.Text));
        var hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

        // The digest already includes the voice name.  Do not use the request
        // value as a directory component: an untrusted voice name could contain
        // rooted or parent-relative path segments and escape the cache folder.
        return Path.Combine(cacheDirectory, $"{hash}{EdgeTtsClient.GetAudioFileExtension(outputFormat)}");
    }

    private static void EnforceCacheQuota(string directory, string? protectedPath)
    {
        lock (CacheLock)
        {
            CacheEntry[] entries;
            try
            {
                entries = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => IsCacheFile(path) && !IsReparsePoint(path))
                    .Select(path =>
                    {
                        var info = new FileInfo(path);
                        return new CacheEntry(path, info.Length, info.LastWriteTimeUtc);
                    })
                    .OrderBy(entry => entry.LastWriteTimeUtc)
                    .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch
            {
                // Cache cleanup is best effort; speech can still proceed.
                return;
            }

            var maxBytes = GetMaxCacheBytes();
            var maxFiles = GetMaxCacheFiles();
            var totalBytes = entries.Sum(entry => entry.Length);
            var fileCount = entries.Length;
            var deleted = 0;

            foreach (var entry in entries)
            {
                if ((totalBytes <= maxBytes && fileCount <= maxFiles)
                    || string.Equals(entry.Path, protectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(entry.Path);
                    totalBytes -= entry.Length;
                    fileCount--;
                    deleted++;
                }
                catch
                {
                    // A file may be in use or removed by another process.
                }
            }

            if (deleted > 0)
            {
                AppLogService.Info(
                    $"Speech cache cleanup removed {deleted} file(s). bytesRemaining={totalBytes}, filesRemaining={fileCount}");
            }
        }
    }

    private static string GetCacheDirectory()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
        {
            localData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        var path = Path.Combine(localData, "OpenRemoteShouter", "EdgeTTS");
        try
        {
            Directory.CreateDirectory(path);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
        catch
        {
            // Cache hardening is best effort; synthesis can still proceed if
            // the platform does not expose Unix permission APIs.
        }

        return path;
    }

    private static bool IsCacheFile(string path)
    {
        var extension = Path.GetExtension(path);
        if (!extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length == 64 && name.All(Uri.IsHexDigit);
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

    private static long GetMaxCacheBytes()
    {
        var configured = Environment.GetEnvironmentVariable(CacheMaxBytesEnvironmentVariable);
        return long.TryParse(
                   configured,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value >= MinimumMaxCacheBytes
            ? Math.Min(value, MaximumMaxCacheBytes)
            : DefaultMaxCacheBytes;
    }

    private static int GetMaxCacheFiles()
    {
        var configured = Environment.GetEnvironmentVariable(CacheMaxFilesEnvironmentVariable);
        return int.TryParse(
                   configured,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value >= MinimumMaxCacheFiles
            ? Math.Min(value, MaximumMaxCacheFiles)
            : DefaultMaxCacheFiles;
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

    private static string NormalizeSpeechText(string text)
    {
        return string.Join(
            Environment.NewLine,
            text.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));
    }

    private sealed record SpeechWorkItem(
        string Text,
        string VoiceName,
        int Rate,
        float Volume,
        Action<TimeSpan>? DurationAvailable,
        Action? SpeechFailed);

    private sealed record CacheEntry(
        string Path,
        long Length,
        DateTime LastWriteTimeUtc);
}
