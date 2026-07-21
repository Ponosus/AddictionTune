namespace AddictionTune.Models;

/// <summary>
/// Пресет атмосферы: название, цвет и поисковый запрос.
/// Описания локализованы и живут в Loc (ключ "preset_" + Name).
/// </summary>
public sealed record Preset(string Name, string Color, string Query);

/// <summary>Все доступные пресеты приложения.</summary>
public static class Presets
{
    public static readonly IReadOnlyList<Preset> All = new[]
    {
        new Preset("ACTIVE", "#E74C3C", "Breakcore Jungle mix"),
        new Preset("FOCUS", "#3498DB", "Maidcore mix"),
        new Preset("RELAX", "#2ECC71", "Ambient Lofi"),
    };
}
