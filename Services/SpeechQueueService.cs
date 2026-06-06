using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace RemoteShouter.Services;

public sealed class SpeechQueueService
{
    private readonly EdgeTtsClient _edgeTtsClient = new();
    private readonly AudioPlaybackService _audioPlaybackService = new();
    private readonly Queue<SpeechWorkItem> _queue = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _currentCancellation;
    private bool _isProcessing;

    public string? LastError { get; private set; }

    public Task SpeakLatestAsync(string text, string voiceName, int rate, float volume)
    {
        var normalized = NormalizeSpeechText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.CompletedTask;
        }

        StopInternal();

        var workItem = new SpeechWorkItem(normalized, voiceName, rate, volume);
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
        var cachePath = GetCachePath(workItem);
        if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
        {
            AppLogService.Info($"Speech cache hit. path={cachePath}, bytes={new FileInfo(cachePath).Length}");
            return cachePath;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(18));

        var audio = await _edgeTtsClient.SynthesizeAsync(
            workItem.Text,
            workItem.VoiceName,
            EdgeTtsClient.CurrentOutputFormat,
            workItem.Rate,
            workItem.Volume,
            timeout.Token);
        await File.WriteAllBytesAsync(cachePath, audio, cancellationToken);
        AppLogService.Info($"Speech cache written. path={cachePath}, bytes={audio.Length}");
        return cachePath;
    }

    private static string GetCachePath(SpeechWorkItem workItem)
    {
        var data = Encoding.UTF8.GetBytes(
            string.Join(
                '\n',
                workItem.VoiceName,
                EdgeTtsClient.CurrentOutputFormat,
                workItem.Rate.ToString(CultureInfo.InvariantCulture),
                workItem.Volume.ToString("0.00", CultureInfo.InvariantCulture),
                workItem.Text));
        var hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        return Path.Combine(GetCacheDirectory(), workItem.VoiceName, $"{hash}{GetSpeechFileExtension()}");
    }

    private static string GetSpeechFileExtension()
    {
        return OperatingSystem.IsWindows() ? ".mp3" : ".wav";
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

        return Path.Combine(localData, "OpenRemoteShouter", "EdgeTTS");
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
        float Volume);
}
