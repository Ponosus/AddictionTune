using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AddictionTune.Models;
using AddictionTune.Services;
// В WPF есть свой класс Track (часть Slider/ScrollBar) — явно указываем нашу модель.
using Track = AddictionTune.Models.Track;

namespace AddictionTune;

public partial class MainWindow : Window
{
    private enum AppPage { Language, Home, Onboarding, Links, Presets, Player }

    private const int DefaultVolume = 120;
    private const int MiniTitleMaxChars = 25;
    private const double MiniBarMinWidth = 580;
    private const double MiniBarMaxWidth = 780;

    private readonly ConfigStore _configStore = new();
    private readonly AppConfig _config;
    private readonly AudioEngine _engine;
    private readonly YtDlpService _ytDlp = new();
    private readonly DispatcherTimer _uiTimer;

    private List<Track> _playlist = new();
    private int _trackIndex;
    private int _onboardingIndex;
    private int _requestId; // защита от устаревших результатов фоновых операций
    private int _playlistId; // версия плейлиста (для предзагрузки следующего трека)
    private int _prefetchedIndex = -1;
    private string? _prefetchedUrl;
    private bool _mediaLoaded;
    private bool _isPlaying;
    private bool _isDragging;
    private bool _syncingVolume;
    private bool _isDark = true;
    private bool _linksFromSettings; // LinksPage открыта из настроек, а не из онбординга
    private string? _pendingVlcUpdate;
    private AppPage _currentPage = AppPage.Language;

    public MainWindow()
    {
        InitializeComponent();

        _config = _configStore.Load();

        _engine = new AudioEngine(DefaultVolume);
        // EndReached приходит из потока libvlc — маршалим в UI-поток.
        _engine.PlaybackEnded += () => Dispatcher.BeginInvoke(new Action(PlayNext));

        _syncingVolume = true;
        VolumeSlider.Value = DefaultVolume;
        MiniVolumeSlider.Value = DefaultVolume;
        _syncingVolume = false;

        BuildPresetButtons();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _uiTimer.Tick += (_, _) => UpdateProgress();
        _uiTimer.Start();

        // Язык выбирается один раз: если уже сохранён — сразу на главный экран.
        if (_config.Language is not null)
        {
            Loc.Lang = _config.Language;
            ApplyLocalization();
            ShowPage(AppPage.Home);
        }
        else
        {
            AnimateIn(LanguagePage);
        }

        _ = CheckForUpdatesAsync();
    }

    // ------------------------------------------------------------------
    // Язык и локализация
    // ------------------------------------------------------------------

    private void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string lang)
        {
            Loc.Lang = lang;
            _config.Language = lang;
            _configStore.Save(_config);
            ApplyLocalization();
            ShowPage(AppPage.Home);
        }
    }

    private void ApplyLocalization()
    {
        EnterFlowButton.Content = Loc.T("enter_flow");
        AtmosphereTitle.Text = Loc.T("atmosphere");
        OnbNextButton.Content = Loc.T("next");
        LinksTitle.Text = Loc.T("settings_about");
        AuthorText.Text = Loc.T("author") + ": bublik (Ponosus)";
        EnjoyText.Text = Loc.T("enjoy");
        LinksStartButton.Content = _linksFromSettings ? Loc.T("back") : Loc.T("lets_go");
        MenuInfo.Header = Loc.T("settings_info");
        MenuLanguage.Header = Loc.T("settings_language");
        MenuAbout.Header = Loc.T("settings_about");

        if (_pendingVlcUpdate is not null)
        {
            UpdateText.Text = string.Format(Loc.T("vlc_update"), _pendingVlcUpdate);
        }
    }

    // ------------------------------------------------------------------
    // Настройки
    // ------------------------------------------------------------------

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (SettingsButton.ContextMenu is { } menu)
        {
            menu.PlacementTarget = SettingsButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void OnMenuInfoClick(object sender, RoutedEventArgs e)
    {
        _onboardingIndex = 0;
        UpdateOnboardingPage();
        ShowPage(AppPage.Onboarding);
    }

    private void OnMenuLanguageClick(object sender, RoutedEventArgs e) => ShowPage(AppPage.Language);

    private void OnMenuAboutClick(object sender, RoutedEventArgs e)
    {
        _linksFromSettings = true;
        LinksStartButton.Content = Loc.T("back");
        ShowPage(AppPage.Links);
    }

    // ------------------------------------------------------------------
    // Построение, навигация и анимации
    // ------------------------------------------------------------------

    private void BuildPresetButtons()
    {
        foreach (var preset in Presets.All)
        {
            var button = new Button
            {
                Content = preset.Name,
                FontSize = 45,
                FontWeight = FontWeights.Bold,
                Width = 500,
                Height = 100,
                Margin = new Thickness(0, 8, 0, 8),
                Style = (Style)FindResource("GhostButton"),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Color)),
                Tag = preset,
            };
            button.Click += OnPresetClick;
            PresetsPanel.Children.Add(button);
        }
    }

    /// <summary>Мягкое появление: плавное проявление + лёгкий сдвиг снизу вверх.</summary>
    private static void AnimateIn(UIElement element)
    {
        var translate = new TranslateTransform(0, 20);
        element.RenderTransform = translate;

        var duration = TimeSpan.FromMilliseconds(280);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(20, 0, duration) { EasingFunction = ease });
    }

    private void ShowPage(AppPage page)
    {
        _currentPage = page;

        SetPageVisible(LanguagePage, page == AppPage.Language);
        SetPageVisible(HomePage, page == AppPage.Home);
        SetPageVisible(OnboardingPage, page == AppPage.Onboarding);
        SetPageVisible(LinksPage, page == AppPage.Links);
        SetPageVisible(PresetsPage, page == AppPage.Presets);
        SetPageVisible(PlayerPage, page == AppPage.Player);

        // Кнопки темы/настроек видны только на главном экране.
        var topButtons = page == AppPage.Home ? Visibility.Visible : Visibility.Collapsed;
        ThemeButton.Visibility = topButtons;
        SettingsButton.Visibility = topButtons;

        // Мини-плеер виден везде, кроме полноэкранного плеера и экрана выбора языка.
        var miniVisible = page != AppPage.Player && page != AppPage.Language && _mediaLoaded;
        var miniWasVisible = MiniBar.Visibility == Visibility.Visible;
        MiniBar.Visibility = miniVisible ? Visibility.Visible : Visibility.Collapsed;
        if (miniVisible && !miniWasVisible)
        {
            AnimateIn(MiniBar);
        }
    }

    private static void SetPageVisible(UIElement element, bool visible)
    {
        var wasVisible = element.Visibility == Visibility.Visible;
        element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (visible && !wasVisible)
        {
            AnimateIn(element);
        }
    }

    private void OnEnterFlowClick(object sender, RoutedEventArgs e)
    {
        if (_config.ShowOnboarding)
        {
            _onboardingIndex = 0;
            UpdateOnboardingPage();
            ShowPage(AppPage.Onboarding);
        }
        else
        {
            ShowPage(AppPage.Presets);
        }
    }

    private void OnBackToHomeClick(object sender, RoutedEventArgs e) => ShowPage(AppPage.Home);

    private void OnBackToPresetsClick(object sender, RoutedEventArgs e) => ShowPage(AppPage.Presets);

    // ------------------------------------------------------------------
    // Онбординг и экран об авторе
    // ------------------------------------------------------------------

    private void UpdateOnboardingPage()
    {
        var preset = Presets.All[_onboardingIndex];
        OnbTitle.Text = preset.Name;
        OnbTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Color));
        OnbDescription.Text = Loc.T("preset_" + preset.Name);
        OnbNextButton.Content = Loc.T("next");
        AnimateIn(OnbPanel);
    }

    private void OnOnboardingNextClick(object sender, RoutedEventArgs e)
    {
        if (_onboardingIndex < Presets.All.Count - 1)
        {
            _onboardingIndex++;
            UpdateOnboardingPage();
        }
        else
        {
            // После рассказа о жанрах — экран со ссылками автора.
            _linksFromSettings = false;
            LinksStartButton.Content = Loc.T("lets_go");
            ShowPage(AppPage.Links);
        }
    }

    private void OnLinksStartClick(object sender, RoutedEventArgs e)
    {
        if (_linksFromSettings)
        {
            ShowPage(AppPage.Home);
            return;
        }
        _config.ShowOnboarding = false;
        _configStore.Save(_config);
        ShowPage(AppPage.Presets);
    }

    private void OnGithubClick(object sender, RoutedEventArgs e) => OpenUrl("https://github.com/Ponosus/");

    private void OnTelegramClick(object sender, RoutedEventArgs e) => OpenUrl("https://t.me/VestronVulture");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Не удалось открыть браузер — не критично.
        }
    }

    // ------------------------------------------------------------------
    // Запуск пресета
    // ------------------------------------------------------------------

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Preset preset)
        {
            StartPreset(preset);
        }
    }

    private async void StartPreset(Preset preset)
    {
        var requestId = ++_requestId;

        _engine.Stop();
        _mediaLoaded = false;
        _isPlaying = false;
        SyncPlayButtons();
        ShowPage(AppPage.Player);
        TrackTitleText.Text = Loc.T("searching");
        ArtistText.Text = string.Empty;

        if (!_ytDlp.IsAvailable)
        {
            TrackTitleText.Text = Loc.T("no_ytdlp");
            return;
        }

        List<Track> tracks;
        try
        {
            tracks = await _ytDlp.SearchAsync(preset.Query);
        }
        catch (Exception ex)
        {
            if (requestId == _requestId)
            {
                TrackTitleText.Text = Loc.T("search_error") + ex.Message;
            }
            return;
        }

        if (requestId != _requestId)
        {
            return; // пользователь уже выбрал другой пресет
        }
        if (tracks.Count == 0)
        {
            TrackTitleText.Text = Loc.T("nothing_found");
            return;
        }

        Shuffle(tracks);
        _playlist = tracks;
        _trackIndex = 0;
        _playlistId++;
        _prefetchedIndex = -1;
        _prefetchedUrl = null;
        await PlayCurrentAsync(requestId);
    }

    private async Task PlayCurrentAsync(int requestId)
    {
        // Проблемные треки пропускаем, но не больше одного круга по плейлисту.
        for (var attempts = 0; attempts < _playlist.Count; attempts++)
        {
            var track = _playlist[_trackIndex];
            TrackTitleText.Text = track.Title;
            ArtistText.Text = track.Artist;

            try
            {
                string url;
                if (_trackIndex == _prefetchedIndex && _prefetchedUrl is not null)
                {
                    // Ссылка уже предзагружена — переключение практически без паузы.
                    url = _prefetchedUrl;
                    _prefetchedIndex = -1;
                    _prefetchedUrl = null;
                }
                else
                {
                    url = await _ytDlp.ResolveStreamUrlAsync(track.VideoId);
                }
                if (requestId != _requestId)
                {
                    return;
                }

                _engine.Play(url);
                _mediaLoaded = true;
                _isPlaying = true;
                MiniTrackButton.Content = Truncate(track.Title, MiniTitleMaxChars);
                SyncPlayButtons();
                _ = PrefetchNextAsync(_playlistId); // заранее готовим следующий трек
                return;
            }
            catch (Exception)
            {
                if (requestId != _requestId)
                {
                    return;
                }
                _trackIndex = (_trackIndex + 1) % _playlist.Count;
            }
        }
        TrackTitleText.Text = Loc.T("cant_play");
    }

    /// <summary>
    /// Заранее получает прямую ссылку на следующий трек, чтобы автопереход
    /// и кнопка «следующий» срабатывали почти мгновенно, без паузы на yt-dlp.
    /// </summary>
    private async Task PrefetchNextAsync(int playlistId)
    {
        if (_playlist.Count < 2)
        {
            return;
        }
        var nextIndex = (_trackIndex + 1) % _playlist.Count;
        try
        {
            var url = await _ytDlp.ResolveStreamUrlAsync(_playlist[nextIndex].VideoId);
            if (playlistId == _playlistId)
            {
                _prefetchedIndex = nextIndex;
                _prefetchedUrl = url;
            }
        }
        catch
        {
            // Не получилось предзагрузить — включим по обычной схеме.
        }
    }

    // ------------------------------------------------------------------
    // Управление воспроизведением
    // ------------------------------------------------------------------

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (!_mediaLoaded)
        {
            return;
        }
        _isPlaying = _engine.TogglePause();
        SyncPlayButtons();
    }

    private void SyncPlayButtons()
    {
        var symbol = _isPlaying ? "⏸" : "▶";
        PlayButton.Content = symbol;
        MiniPlayButton.Content = symbol;
    }

    private void OnNextClick(object sender, RoutedEventArgs e) => PlayNext();

    private void OnPrevClick(object sender, RoutedEventArgs e) => StepTrack(-1);

    private void PlayNext() => StepTrack(+1);

    private void StepTrack(int offset)
    {
        if (_playlist.Count == 0)
        {
            return;
        }
        _trackIndex = ((_trackIndex + offset) % _playlist.Count + _playlist.Count) % _playlist.Count;
        _ = PlayCurrentAsync(++_requestId);
    }

    private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncingVolume || _engine is null)
        {
            return;
        }
        _syncingVolume = true;
        var volume = (int)e.NewValue;
        _engine.Volume = volume;
        // Синхронизируем оба слайдера (большой плеер и мини-плеер).
        VolumeSlider.Value = volume;
        MiniVolumeSlider.Value = volume;
        _syncingVolume = false;
    }

    // Перемотка: IsMoveToPointEnabled в стиле ModernSlider переносит ползунок в точку клика,
    // а обработчики ниже применяют новую позицию к воспроизведению (и при клике, и при перетаскивании).

    private void OnSeekDragStarted(object sender, DragStartedEventArgs e) => _isDragging = true;

    private void OnSeekDragCompleted(object sender, DragCompletedEventArgs e)
    {
        ApplySeek();
        _isDragging = false;
    }

    private void OnSeekPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            ApplySeek(); // клик по дорожке без перетаскивания — прыжок на тайм-код
        }
    }

    private void ApplySeek()
    {
        if (_mediaLoaded)
        {
            _engine.Seek((float)(ProgressSlider.Value / 100.0));
        }
    }

    private void UpdateProgress()
    {
        if (!_mediaLoaded || _isDragging)
        {
            return;
        }
        var (timeMs, lengthMs) = _engine.Progress;
        if (lengthMs <= 0)
        {
            return;
        }
        ProgressSlider.Value = (double)timeMs / lengthMs * 100.0;
        CurrentTimeText.Text = FormatTime(timeMs);
        TotalTimeText.Text = FormatTime(lengthMs);
    }

    // ------------------------------------------------------------------
    // Мини-плеер и тема
    // ------------------------------------------------------------------

    private void OnMiniTrackClick(object sender, RoutedEventArgs e) => ShowPage(AppPage.Player);

    private void OnMiniCloseClick(object sender, RoutedEventArgs e)
    {
        _requestId++; // отменяем все фоновые загрузки
        _engine.Stop();
        _mediaLoaded = false;
        _isPlaying = false;
        SyncPlayButtons();
        MiniBar.Visibility = Visibility.Collapsed;
    }

    // Растягивание мини-плеера за края (в пределах MiniBarMinWidth..MiniBarMaxWidth).
    // Бар отцентрован, поэтому тянем в обе стороны симметрично (дельта удваивается).

    private void OnMiniResizeRight(object sender, DragDeltaEventArgs e) => ResizeMiniBar(e.HorizontalChange * 2);

    private void OnMiniResizeLeft(object sender, DragDeltaEventArgs e) => ResizeMiniBar(-e.HorizontalChange * 2);

    private void ResizeMiniBar(double delta) =>
        MiniBar.Width = Math.Clamp(MiniBar.Width + delta, MiniBarMinWidth, MiniBarMaxWidth);

    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        ((App)Application.Current).ApplyTheme(_isDark);
        ThemeButton.Content = _isDark ? "🌙" : "☀";
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _uiTimer.Stop();
        _engine.Dispose();
    }

    // ------------------------------------------------------------------
    // Проверка обновлений
    // ------------------------------------------------------------------

    private async Task CheckForUpdatesAsync()
    {
        // yt-dlp умеет обновлять сам себя (ключ -U) — YouTube часто ломает старые версии.
        if (_ytDlp.IsAvailable)
        {
            await _ytDlp.TryUpdateAsync();
        }

        // Проверяем новую версию VLC по официальному эндпоинту VideoLAN.
        var newerVlc = await UpdateChecker.GetNewerVlcVersionAsync(_engine.LibVlcVersion);
        if (newerVlc is not null)
        {
            _pendingVlcUpdate = newerVlc;
            UpdateText.Text = string.Format(Loc.T("vlc_update"), newerVlc);
        }
    }

    // ------------------------------------------------------------------
    // Вспомогательные функции
    // ------------------------------------------------------------------

    private static string FormatTime(long ms)
    {
        var totalSeconds = Math.Max(ms, 0) / 1000;
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "...";

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
