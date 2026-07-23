using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Interfaces;

/// <summary>
/// Service for searching music metadata from public sources (no authentication required).
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Searches for tracks, artists, albums, and playlists by query.
    /// </summary>
    Task<SearchResults> SearchAsync(string query, int limit = 20);

    /// <summary>
    /// Gets detailed metadata for a single track by search or URL.
    /// </summary>
    Task<SpotifyTrack?> GetTrackAsync(string queryOrUrl);

    /// <summary>
    /// Loads content from a Spotify URL (track, album, playlist, or artist).
    /// Extracts metadata from public pages without authentication.
    /// </summary>
    Task<object?> LoadFromLinkAsync(string url);

    /// <summary>
    /// Parses a Spotify URL or URI into its components.
    /// </summary>
    SpotifyLinkInfo? ParseSpotifyLink(string url);

    /// <summary>
    /// Gets the audio source URL for a track (e.g. YouTube).
    /// </summary>
    Task<string?> GetAudioSourceUrlAsync(string artist, string title);

    /// <summary>
    /// Extracts track metadata from a YouTube video/page.
    /// </summary>
    Task<SpotifyTrack?> ExtractMetadataFromYouTubeAsync(string videoUrl);
}

/// <summary>
/// Service for downloading and converting audio tracks.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Downloads a single track.
    /// </summary>
    Task<DownloadItem> DownloadTrackAsync(SpotifyTrack track, AudioQuality quality, AudioFormat format,
        string outputPath, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads multiple tracks in batch.
    /// </summary>
    Task<List<DownloadItem>> DownloadBatchAsync(List<SpotifyTrack> tracks, AudioQuality quality,
        AudioFormat format, string outputPath, IProgress<BatchDownloadProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Pauses an active download.
    /// </summary>
    Task PauseDownloadAsync(string downloadId);

    /// <summary>
    /// Resumes a paused download.
    /// </summary>
    Task ResumeDownloadAsync(string downloadId);

    /// <summary>
    /// Cancels an active or pending download.
    /// </summary>
    Task CancelDownloadAsync(string downloadId);

    /// <summary>
    /// Retries a failed download.
    /// </summary>
    Task<DownloadItem> RetryDownloadAsync(DownloadItem item);

    /// <summary>
    /// Gets the current download queue.
    /// </summary>
    List<DownloadItem> GetActiveDownloads();

    /// <summary>
    /// Gets the download history.
    /// </summary>
    Task<DownloadHistory> GetHistoryAsync();

    /// <summary>
    /// Clears completed downloads from the queue.
    /// </summary>
    Task ClearCompletedAsync();

    /// <summary>
    /// Clears all downloads from the queue.
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Searches the download history.
    /// </summary>
    Task<List<DownloadItem>> SearchHistoryAsync(string query);

    /// <summary>
    /// Checks if FFmpeg is available on the system.
    /// </summary>
    Task<bool> IsFfmpegAvailableAsync();

    /// <summary>
    /// Gets the FFmpeg executable path.
    /// </summary>
    Task<string> GetFfmpegPathAsync();

    /// <summary>
    /// Total download count.
    /// </summary>
    int TotalDownloads { get; }

    /// <summary>
    /// Active download count.
    /// </summary>
    int ActiveDownloadCount { get; }

    event EventHandler<DownloadItem>? DownloadStarted;
    event EventHandler<DownloadItem>? DownloadProgressChanged;
    event EventHandler<DownloadItem>? DownloadCompleted;
    event EventHandler<DownloadItem>? DownloadFailed;
    event EventHandler<DownloadItem>? DownloadPaused;
    event EventHandler<DownloadItem>? DownloadResumed;
}

/// <summary>
/// Service for converting audio files between formats.
/// </summary>
public interface IAudioConverterService
{
    /// <summary>
    /// Converts an audio file to the specified format and quality.
    /// </summary>
    Task<string?> ConvertAsync(string inputPath, string outputPath, AudioFormat targetFormat,
        AudioQuality quality, CancellationToken ct = default);

    /// <summary>
    /// Embeds metadata (including cover art) into an audio file.
    /// </summary>
    Task<bool> EmbedMetadataAsync(string filePath, SpotifyTrack track);

    /// <summary>
    /// Gets the file extension for a given format.
    /// </summary>
    string GetExtension(AudioFormat format);

    /// <summary>
    /// Gets the FFmpeg codec name for a given format.
    /// </summary>
    string GetCodec(AudioFormat format);

    /// <summary>
    /// Validates that the converted file is not corrupted.
    /// </summary>
    Task<bool> ValidateAsync(string filePath);

    /// <summary>
    /// Gets the estimated output size in bytes.
    /// </summary>
    long EstimateSize(int durationMs, AudioQuality quality, AudioFormat format);
}

/// <summary>
/// Service for persisting and loading application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    string SettingsPath { get; }

    event EventHandler<AppSettings>? SettingsChanged;
}

/// <summary>
/// Service for managing application themes at runtime.
/// </summary>
public interface IThemeService
{
    void ApplyTheme(AppTheme theme);
    void ApplyAccentColor(string colorHex);
    void ApplyTransparency(double opacity);
    void ApplyCornerRadius(double radius);
    void ApplyFontSize(double size);
    void ApplyUiScale(double scale);
    void ToggleMica(bool enable);
    void ToggleAcrylic(bool enable);
    void ApplyMica(bool enable);
    void ApplyAcrylic(bool enable);
    void ApplyAll(ThemeSettings settings);
    bool IsMicaSupported { get; }
    bool IsAcrylicSupported { get; }
}

/// <summary>
/// Service for localization and multi-language support.
/// </summary>
public interface ILocalizationService
{
    string GetString(string key);
    string this[string key] { get; }
    string Format(string key, params object[] args);
    void SetLanguage(AppLanguage language);
    AppLanguage CurrentLanguage { get; }
    event EventHandler<AppLanguage>? LanguageChanged;
}

/// <summary>
/// Service for showing desktop notifications.
/// </summary>
public interface INotificationService
{
    void Show(string title, string message, NotificationType type = NotificationType.Info);
    void ShowDownloadComplete(string trackName);
    void ShowDownloadError(string trackName, string error);
    void ShowError(string message);
    void ShowSuccess(string message);
    void Clear();
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Service for checking and installing application updates.
/// </summary>
public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task<bool> DownloadUpdateAsync(string downloadUrl, IProgress<double>? progress = null);
    Task<bool> InstallUpdateAsync(string updatePath);
    string CurrentVersion { get; }
}

/// <summary>
/// Service for caching data to improve performance.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task ClearExpiredAsync();
    Task ClearAllAsync();
    Task<long> GetCacheSizeAsync();
    bool IsEnabled { get; }
}

/// <summary>
/// Service for managing in-app audio playback.
/// </summary>
public interface IPlayerService
{
    Task PlayAsync(SpotifyTrack track);
    Task PlayQueueAsync(List<SpotifyTrack> tracks, int startIndex = 0);
    void Pause();
    void Resume();
    void Stop();
    void Next();
    void Previous();
    void Seek(TimeSpan position);
    void SetVolume(double volume);
    void ToggleShuffle();
    void ToggleRepeat();
    List<SpotifyTrack> Queue { get; set; }
    SpotifyTrack? CurrentTrack { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Volume { get; }
    bool IsPlaying { get; }
    bool IsShuffled { get; }
    RepeatMode RepeatMode { get; }
    int CurrentIndex { get; set; }
    event EventHandler<SpotifyTrack>? TrackChanged;
    event EventHandler<TimeSpan>? PositionChanged;
    event EventHandler<bool>? PlayStateChanged;
    event EventHandler<double>? VolumeChanged;
    event EventHandler<bool>? ShuffleChanged;
    event EventHandler<RepeatMode>? RepeatChanged;
    event EventHandler? QueueEnded;
}

/// <summary>
/// Service for fetching song lyrics from public sources.
/// </summary>
public interface ILyricsService
{
    Task<string> GetLyricsAsync(string artist, string title);
    Task<bool> HasLyricsAsync(string artist, string title);
}

public enum RepeatMode
{
    None,
    One,
    All
}
