using System.Windows;

namespace AddictionTune;

public partial class App : Application
{
    /// <summary>Переключает тёмную/светлую тему, подменяя словарь ресурсов.</summary>
    public void ApplyTheme(bool dark)
    {
        var uri = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        Resources.MergedDictionaries[1] = new ResourceDictionary { Source = uri };
    }
}
