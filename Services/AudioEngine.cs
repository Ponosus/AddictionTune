using LibVLCSharp.Shared;

namespace AddictionTune.Services;

/// <summary>
/// Тонкая обёртка над LibVLCSharp: воспроизведение потока, громкость, перемотка.
/// libvlc приезжает через NuGet-пакет VideoLAN.LibVLC.Windows — устанавливать VLC не нужно.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;

    /// <summary>
    /// Трек доиграл до конца. Внимание: событие приходит из потока libvlc —
    /// обработчик должен маршалить вызов в UI-поток (Dispatcher).
    /// </summary>
    public event Action? PlaybackEnded;

    public AudioEngine(int volume)
    {
        Core.Initialize();
        // Большой сетевой буфер (3 сек) + авто-переподключение —
        // чтобы воспроизведение не прерывалось при просадках интернета.
        _libVlc = new LibVLC("--no-video", "--quiet", "--network-caching=3000", "--http-reconnect");
        _player = new MediaPlayer(_libVlc) { Volume = volume };
        _player.EndReached += (_, _) => PlaybackEnded?.Invoke();
    }

    /// <summary>Версия встроенного libvlc (для проверки обновлений VLC).</summary>
    public string LibVlcVersion => _libVlc.Version;

    public int Volume
    {
        get => _player.Volume;
        set => _player.Volume = value;
    }

    public void Play(string url)
    {
        // Явный Stop перед новым треком: после EndReached плеер остаётся
        // в состоянии Ended и без этого может не запустить следующий трек.
        _player.Stop();
        using var media = new Media(_libVlc, url, FromType.FromLocation);
        _player.Play(media);
    }

    /// <summary>Переключает паузу. Возвращает true, если после вызова музыка играет.</summary>
    public bool TogglePause()
    {
        if (_player.IsPlaying)
        {
            _player.Pause();
            return false;
        }
        _player.Play();
        return true;
    }

    public void Stop() => _player.Stop();

    /// <summary>(текущее время, длительность) в миллисекундах.</summary>
    public (long TimeMs, long LengthMs) Progress => (_player.Time, _player.Length);

    /// <summary>Перемотка на позицию 0.0–1.0.</summary>
    public void Seek(float fraction) => _player.Position = Math.Clamp(fraction, 0f, 1f);

    public void Dispose()
    {
        _player.Dispose();
        _libVlc.Dispose();
    }
}
