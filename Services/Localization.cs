namespace AddictionTune.Services;

/// <summary>
/// Простая локализация: три языка (ru/en/es), выбирается на стартовом экране.
/// </summary>
public static class Loc
{
    /// <summary>Текущий язык: "ru", "en" или "es".</summary>
    public static string Lang { get; set; } = "ru";

    public static string T(string key)
    {
        if (Strings.TryGetValue(Lang, out var table) && table.TryGetValue(key, out var value))
        {
            return value;
        }
        return Strings["en"].TryGetValue(key, out var fallback) ? fallback : key;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["ru"] = new()
        {
            ["enter_flow"] = "Войти в поток",
            ["atmosphere"] = "атмосфера:",
            ["next"] = "Далее",
            ["lets_go"] = "Поехали",
            ["searching"] = "Поиск...",
            ["no_ytdlp"] = "Не найден yt-dlp.exe рядом с программой",
            ["search_error"] = "Ошибка поиска: ",
            ["nothing_found"] = "Ничего не найдено. Проверьте интернет.",
            ["cant_play"] = "Не удалось воспроизвести ни один трек",
            ["enjoy"] = "Приятного пользования!",
            ["untitled"] = "Без названия",
            ["vlc_update"] = "Вышла новая версия VLC ({0}). Обновите пакет VideoLAN.LibVLC.Windows и пересоберите приложение.",
            ["settings_info"] = "Информация",
            ["settings_language"] = "Язык",
            ["settings_about"] = "Об авторе",
            ["back"] = "Назад",
            ["author"] = "Автор",
            ["preset_ACTIVE"] = "Высокий темп (Breakcore/Jungle).",
            ["preset_FOCUS"] = "Тяжёлые гитары, 8-битные мелодии\nи агрессивная электроника.\nИдеально для работы (Maidcore).",
            ["preset_RELAX"] = "Ambient и Lo-Fi.",
        },
        ["en"] = new()
        {
            ["enter_flow"] = "Enter the flow",
            ["atmosphere"] = "atmosphere:",
            ["next"] = "Next",
            ["lets_go"] = "Let's go",
            ["searching"] = "Searching...",
            ["no_ytdlp"] = "yt-dlp.exe not found next to the app",
            ["search_error"] = "Search error: ",
            ["nothing_found"] = "Nothing found. Check your internet.",
            ["cant_play"] = "Couldn't play any track",
            ["enjoy"] = "Enjoy!",
            ["untitled"] = "Untitled",
            ["vlc_update"] = "A new VLC version ({0}) is out. Update the VideoLAN.LibVLC.Windows package and rebuild the app.",
            ["settings_info"] = "Information",
            ["settings_language"] = "Language",
            ["settings_about"] = "About the author",
            ["back"] = "Back",
            ["author"] = "Author",
            ["preset_ACTIVE"] = "High tempo (Breakcore/Jungle).",
            ["preset_FOCUS"] = "Heavy guitars, 8-bit melodies\nand aggressive electronics.\nPerfect for work (Maidcore).",
            ["preset_RELAX"] = "Ambient and Lo-Fi.",
        },
        ["es"] = new()
        {
            ["enter_flow"] = "Entrar al flujo",
            ["atmosphere"] = "atmósfera:",
            ["next"] = "Siguiente",
            ["lets_go"] = "¡Vamos!",
            ["searching"] = "Buscando...",
            ["no_ytdlp"] = "No se encontró yt-dlp.exe junto a la aplicación",
            ["search_error"] = "Error de búsqueda: ",
            ["nothing_found"] = "No se encontró nada. Comprueba tu conexión.",
            ["cant_play"] = "No se pudo reproducir ninguna pista",
            ["enjoy"] = "¡Que lo disfrutes!",
            ["untitled"] = "Sin título",
            ["vlc_update"] = "Hay una nueva versión de VLC ({0}). Actualiza el paquete VideoLAN.LibVLC.Windows y recompila la aplicación.",
            ["settings_info"] = "Información",
            ["settings_language"] = "Idioma",
            ["settings_about"] = "Sobre el autor",
            ["back"] = "Atrás",
            ["author"] = "Autor",
            ["preset_ACTIVE"] = "Ritmo alto (Breakcore/Jungle).",
            ["preset_FOCUS"] = "Guitarras pesadas, melodías de 8 bits\ny electrónica agresiva.\nIdeal para trabajar (Maidcore).",
            ["preset_RELAX"] = "Ambient y Lo-Fi.",
        },
    };
}
