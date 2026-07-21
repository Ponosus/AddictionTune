using System.Net.Http;

namespace AddictionTune.Services;

/// <summary>
/// Проверка обновлений VLC по официальному эндпоинту VideoLAN —
/// тому же, которым пользуется сам плеер VLC.
/// </summary>
public static class UpdateChecker
{
    private const string StatusUrl = "https://update.videolan.org/vlc/status-win-x86_64";

    /// <summary>
    /// Возвращает номер более новой версии VLC, если она вышла,
    /// иначе null (в том числе при любых сетевых ошибках).
    /// </summary>
    public static async Task<string?> GetNewerVlcVersionAsync(string currentLibVlcVersion)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var body = await http.GetStringAsync(StatusUrl);

            // Первая строка ответа — номер последней версии, например "3.0.21".
            var latestText = body.Split('\n')[0].Trim();
            if (!Version.TryParse(latestText, out var latest))
            {
                return null;
            }

            // Версия libvlc выглядит как "3.0.20 Vetinari" — отрезаем имя релиза.
            var currentText = currentLibVlcVersion.Split(' ')[0].Trim();
            if (!Version.TryParse(currentText, out var current))
            {
                return null;
            }

            return latest > current ? latestText : null;
        }
        catch
        {
            // Нет сети / таймаут / неожиданный формат — просто без уведомления.
            return null;
        }
    }
}
