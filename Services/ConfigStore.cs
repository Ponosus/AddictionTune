using System.IO;
using System.Text.Json;

namespace AddictionTune.Services;

/// <summary>Все пользовательские настройки приложения.</summary>
public sealed class AppConfig
{
    public bool ShowOnboarding { get; set; } = true;

    /// <summary>Выбранный язык ("ru"/"en"/"es"). null — ещё не выбирали, показать экран выбора.</summary>
    public string? Language { get; set; }
}

/// <summary>
/// Читает и пишет config.json рядом с EXE.
/// Ошибки чтения/записи не роняют приложение — используются значения по умолчанию.
/// </summary>
public sealed class ConfigStore
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppConfig();
            }
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig();
        }
        catch (Exception)
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, SerializerOptions));
        }
        catch (Exception)
        {
            // Не критично: в худшем случае онбординг покажется ещё раз.
        }
    }
}
