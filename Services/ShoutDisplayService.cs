using Avalonia.Threading;
using RemoteShouter.Models;
using RemoteShouter.Views;

namespace RemoteShouter.Services;

public sealed class ShoutDisplayService
{
    private readonly SpeechQueueService _speechService;
    private ShoutWindow? _currentWindow;

    public ShoutDisplayService(SpeechQueueService speechService)
    {
        _speechService = speechService;
    }

    public event EventHandler<ShoutMessage?>? CurrentMessageChanged;

    public ShoutMessage? CurrentMessage { get; private set; }

    public string? SpeechError => _speechService.LastError;

    public Task ShowAsync(ShoutMessage message)
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            CloseCurrentWindow(stopSpeech: true);

            CurrentMessage = message;
            var window = new ShoutWindow(message);
            _currentWindow = window;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_currentWindow, window))
                {
                    _currentWindow = null;
                    CurrentMessage = null;
                    CurrentMessageChanged?.Invoke(this, null);

                    if (window.ShouldStopSpeechOnClose)
                    {
                        _ = _speechService.StopAsync();
                    }
                }
            };

            CurrentMessageChanged?.Invoke(this, message);
            window.Show();

            if (message.SpeechEnabled)
            {
                _ = _speechService.SpeakLatestAsync(
                    message.Message,
                    message.VoiceName,
                    message.SpeechRate,
                    message.SpeechVolume);
            }
        }).GetTask();
    }

    public Task CloseAsync()
    {
        return Dispatcher.UIThread.InvokeAsync(() =>
        {
            CloseCurrentWindow(stopSpeech: true);
        }).GetTask();
    }

    private void CloseCurrentWindow(bool stopSpeech)
    {
        if (_currentWindow is null)
        {
            if (stopSpeech)
            {
                _ = _speechService.StopAsync();
            }

            return;
        }

        var window = _currentWindow;
        _currentWindow = null;
        window.ShouldStopSpeechOnClose = stopSpeech;
        window.Close();
        CurrentMessage = null;
        CurrentMessageChanged?.Invoke(this, null);

        if (stopSpeech)
        {
            _ = _speechService.StopAsync();
        }
    }
}
