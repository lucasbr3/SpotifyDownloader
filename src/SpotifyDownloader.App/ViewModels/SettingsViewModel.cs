using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page with live theme preview.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly ILocalizationService _localization;
    private AppSettings? _current;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private AppLanguage _selectedLanguage;

    [ObservableProperty]
    private string _accentColor = "#1DB954";

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private AudioQuality _selectedQuality = AudioQuality.VeryHigh320;

    [ObservableProperty]
    private AudioFormat _selectedFormat = AudioFormat.Mp3;

    [ObservableProperty]
    private int _maxConcurrent = 3;

    [ObservableProperty]
    private bool _createArtistFolders = true;

    [ObservableProperty]
    private bool _createAlbumFolders = true;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _closeToTray;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private bool _useMica = true;

    [ObservableProperty]
    private bool _useAcrylic;

    [ObservableProperty]
    private double _windowTransparency = 1.0;

    [ObservableProperty]
    private double _cornerRadius = 8;

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private double _uiScale = 1.0;

    [ObservableProperty]
    private bool _compactMode;

    [ObservableProperty]
    private bool _animationsEnabled = true;

    [ObservableProperty]
    private bool _embedCoverArt = true;

    [ObservableProperty]
    private bool _saveMetadata = true;

    [ObservableProperty]
    private int _historyTotal;

    [ObservableProperty]
    private int _historySuccessful;

    [ObservableProperty]
    private int _historyFailed;

    [ObservableProperty]
    private string _historyTotalSize = string.Empty;

    [ObservableProperty]
    private string _cacheSize = string.Empty;

    [ObservableProperty]
    private bool _cacheEnabled = true;

    public List<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>().ToList();
    public List<AppLanguage> Languages { get; } = Enum.GetValues<AppLanguage>().ToList();
    public List<AudioQuality> Qualities { get; } = Enum.GetValues<AudioQuality>().ToList();
    public List<AudioFormat> Formats { get; } = Enum.GetValues<AudioFormat>().ToList();

    public string CurrentVersion => "1.0.0";

    public SettingsViewModel()
    {
        _settings = App.Settings;
        _theme = App.Theme!;
        _localization = App.Localization;

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            _current = await _settings.LoadAsync();

            SelectedTheme = _current.Theme.Theme;
            SelectedLanguage = _current.Language.Language;
            AccentColor = _current.Theme.AccentColor;
            OutputPath = _current.Download.OutputPath;
            SelectedQuality = _current.Download.Quality;
            SelectedFormat = _current.Download.OutputFormat;
            MaxConcurrent = _current.Download.MaxConcurrentDownloads;
            CreateArtistFolders = _current.Download.CreateArtistFolders;
            CreateAlbumFolders = _current.Download.CreateAlbumFolders;
            MinimizeToTray = _current.UI.MinimizeToTray;
            CloseToTray = _current.UI.CloseToTray;
            StartMinimized = _current.UI.StartMinimized;
            ShowNotifications = _current.UI.ShowNotifications;
            UseMica = _current.Theme.UseMica;
            UseAcrylic = _current.Theme.UseAcrylic;
            WindowTransparency = _current.Theme.WindowTransparency;
            CornerRadius = _current.Theme.CornerRadius;
            FontSize = _current.Theme.FontSize;
            UiScale = _current.Theme.UiScale;
            CompactMode = _current.Theme.CompactMode;
            AnimationsEnabled = _current.Theme.AnimationsEnabled;
            EmbedCoverArt = _current.Download.EmbedCoverArt;
            SaveMetadata = _current.Download.SaveMetadata;

            // Load stats
            var history = await App.Download.GetHistoryAsync();
            HistoryTotal = history.TotalDownloads;
            HistorySuccessful = history.SuccessfulDownloads;
            HistoryFailed = history.FailedDownloads;
            HistoryTotalSize = history.TotalSizeFormatted;

            // Cache size
            var cacheBytes = await App.Cache.GetCacheSizeAsync();
            CacheSize = Helpers.FormatHelper.FormatFileSize(cacheBytes);
            CacheEnabled = _current.Cache.Enabled;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load settings");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_current == null) return;

        try
        {
            _current.Theme.Theme = SelectedTheme;
            _current.Theme.AccentColor = AccentColor;
            _current.Language.Language = SelectedLanguage;
            _current.Download.OutputPath = OutputPath;
            _current.Download.Quality = SelectedQuality;
            _current.Download.OutputFormat = SelectedFormat;
            _current.Download.MaxConcurrentDownloads = MaxConcurrent;
            _current.Download.CreateArtistFolders = CreateArtistFolders;
            _current.Download.CreateAlbumFolders = CreateAlbumFolders;
            _current.UI.MinimizeToTray = MinimizeToTray;
            _current.UI.CloseToTray = CloseToTray;
            _current.UI.StartMinimized = StartMinimized;
            _current.UI.ShowNotifications = ShowNotifications;
            _current.Theme.UseMica = UseMica;
            _current.Theme.UseAcrylic = UseAcrylic;
            _current.Theme.WindowTransparency = WindowTransparency;
            _current.Theme.CornerRadius = CornerRadius;
            _current.Theme.FontSize = FontSize;
            _current.Theme.UiScale = UiScale;
            _current.Theme.CompactMode = CompactMode;
            _current.Theme.AnimationsEnabled = AnimationsEnabled;
            _current.Download.EmbedCoverArt = EmbedCoverArt;
            _current.Download.SaveMetadata = SaveMetadata;
            _current.Cache.Enabled = CacheEnabled;

            await _settings.SaveAsync(_current);
            _theme.ApplyAll(_current.Theme);
            _localization.SetLanguage(SelectedLanguage);

            App.Notifications.ShowSuccess("Configurações salvas!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings");
            App.Notifications.ShowError("Falha ao salvar configurações");
        }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        await _settings.ResetAsync();
        await LoadAsync();
        _theme.ApplyAll(AppSettings.Default.Theme);
        App.Notifications.ShowSuccess("Configurações restauradas!");
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await App.Cache.ClearAllAsync();
        CacheSize = "0 B";
        App.Notifications.ShowSuccess("Cache limpo!");
    }

    // Live preview when theme changes
    partial void OnSelectedThemeChanged(AppTheme value) => _theme.ApplyTheme(value);
    partial void OnAccentColorChanged(string value) => _theme.ApplyAccentColor(value);
    partial void OnUseMicaChanged(bool value) => _theme.ApplyMica(value);
    partial void OnUseAcrylicChanged(bool value) => _theme.ApplyAcrylic(value);
    partial void OnWindowTransparencyChanged(double value) => _theme.ApplyTransparency(value);
    partial void OnCornerRadiusChanged(double value) => _theme.ApplyCornerRadius(value);
    partial void OnFontSizeChanged(double value) => _theme.ApplyFontSize(value);
    partial void OnUiScaleChanged(double value) => _theme.ApplyUiScale(value);
}
