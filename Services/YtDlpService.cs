using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using AddictionTune.Models;

namespace AddictionTune.Services;

/// <summary>
/// Поиск треков и получение прямых аудио-ссылок через yt-dlp.exe.
/// Бинарник yt-dlp.exe должен лежать рядом с EXE приложения.
/// </summary>
public sealed class YtDlpService
{
    private static readonly string ExePath = Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");

    public bool IsAvailable => File.Exists(ExePath);

    /// <summary>Ищет треки на YouTube (быстрый плоский поиск без резолва каждого ролика).</summary>
    public async Task<List<Track>> SearchAsync(string query, int limit = 10)
    {
        var sanitized = query.Replace("\"", string.Empty);
        var output = await RunAsync($"--no-warnings --flat-playlist -j \"ytsearch{limit}:{sanitized}\"");

        var tracks = new List<Track>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                var id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
                var artist = root.TryGetProperty("uploader", out var uploaderElement) ? uploaderElement.GetString() : null;

                tracks.Add(new Track(id, title ?? Loc.T("untitled"), artist ?? "Music"));
            }
            catch (JsonException)
            {
                // Служебные строки в выводе просто пропускаем.
            }
        }
        return tracks;
    }

    /// <summary>Получает прямой URL аудиопотока для ролика.</summary>
    public async Task<string> ResolveStreamUrlAsync(string videoId)
    {
        var videoUrl = "https://www.youtube.com/watch?v=" + videoId;
        var output = await RunAsync($"--no-warnings -f bestaudio/best -g \"{videoUrl}\"");
        var url = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("yt-dlp не вернул ссылку на аудиопоток");
        }
        return url;
    }

    /// <summary>
    /// Просит yt-dlp обновить самого себя (ключ -U).
    /// Ошибки глотаем: нет интернета или нет прав на запись — не повод мешать работе плеера.
    /// </summary>
    public async Task TryUpdateAsync()
    {
        try
        {
            await RunAsync("-U");
        }
        catch
        {
            // Обновление необязательно — продолжаем с текущей версией.
        }
    }

    private static async Task<string> RunAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ExePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить yt-dlp.exe");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "yt-dlp завершился с ошибкой" : stderr.Trim());
        }
        return stdout;
    }
}
