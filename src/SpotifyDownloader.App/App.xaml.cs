using Microsoft.UI.Xaml;
using Serilog;
using SpotifyDownloader.App.Services;
using SpotifyDownloader.App.ViewModels;
using SpotifyDownloader.App.Views;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Services;

namespace SpotifyDownloader.App;

public partial class App : WinUIApplication
{
    private Window? _window;

    private static ICacheService? _cache;
    private static IMetadataService? _metadata;
    private static ISettingsService? _settings;
    private static ILocalizationService? _localization;
    private static IThemeService? _theme;
    private static IDownloadService? _download;
    private static IAudioConverterService? _converter;
    private static INotificationService? _notifications;
    private static IPlayerService? _player;
    private static ILyricsService? _lyrics;
    private static IUpdateService? _updater;
    private static DownloadsViewModel? _downloadsViewModel;
    private static PlayerViewModel? _playerViewModel;

    public static ICacheService Cache => _cache ??= new CacheService();
    public static IMetadataService Metadata => _metadata ??= new MetadataService(Cache);
    public static ISettingsService Settings => _settings ??= new SettingsService();
    public static ILocalizationService Localization => _localization ??= new LocalizationService();
    public static IThemeService? Theme => _theme;
    public static IDownloadService Download => _download ??= new DownloadService();
    public static IAudioConverterService Converter => _converter ??= new AudioConverterService();
    public static INotificationService Notifications => _notifications ??= new NotificationService();
    public static IPlayerService Player => _player ??= new PlayerService();
    public static ILyricsService Lyrics => _lyrics ??= new LyricsService(Cache);
    public static IUpdateService Updater => _updater ??= new UpdateService();

    public static MainWindow? MainWindow => (Current as App)?._window as MainWindow;

    public static T? GetService<T>() where T : class
    {
        if (typeof(T) == typeof(MainViewModel))
            return ((Current as App)?._window?.Content as MainPage)?.ViewModel as T;
        if (typeof(T) == typeof(PlayerViewModel))
            return _playerViewModel as T;
        if (typeof(T) == typeof(DownloadsViewModel))
            return (_downloadsViewModel ??= new DownloadsViewModel()) as T;
        return null;
    }

    public App()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotifyDownloader", "logs");
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logPath, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("Application starting (no Spotify auth required)");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _theme = new ThemeService(_window);

        var playerService = new PlayerService();
        _player = playerService;
        _playerViewModel = new PlayerViewModel(playerService);

        var mainViewModel = new MainViewModel();
        _window.Content = new MainPage(mainViewModel, _playerViewModel);
        _window.Activate();

        Log.Information("Application launched");
    }
}
