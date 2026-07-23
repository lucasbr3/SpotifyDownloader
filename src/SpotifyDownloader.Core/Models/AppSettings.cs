using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Root settings class containing all application settings sections.
/// </summary>
public class AppSettings
{
    [JsonProperty("theme")]
    public ThemeSettings Theme { get; set; } = new();

    [JsonProperty("download")]
    public DownloadSettings Download { get; set; } = new();

    [JsonProperty("player")]
    public PlayerSettings Player { get; set; } = new();

    [JsonProperty("ui")]
    public UISettings UI { get; set; } = new();

    [JsonProperty("language")]
    public LanguageSettings Language { get; set; } = new();

    [JsonProperty("update")]
    public UpdateSettings Update { get; set; } = new();

    [JsonProperty("spotify")]
    public SpotifySettings Spotify { get; set; } = new();

    [JsonProperty("cache")]
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Factory method returning default settings.
    /// </summary>
    public static AppSettings Default => new();
}

/// <summary>
/// Theme-related settings.
/// </summary>
public class ThemeSettings
{
    [JsonProperty("theme")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AppTheme Theme { get; set; } = AppTheme.Dark;

    [JsonProperty("accent_color")]
    public string AccentColor { get; set; } = "#1DB954";

    [JsonProperty("accent_color_hover")]
    public string AccentColorHover { get; set; } = "#1ED760";

    [JsonProperty("window_transparency")]
    public double WindowTransparency { get; set; } = 1.0;

    [JsonProperty("use_mica")]
    public bool UseMica { get; set; } = true;

    [JsonProperty("use_acrylic")]
    public bool UseAcrylic { get; set; }

    [JsonProperty("corner_radius")]
    public double CornerRadius { get; set; } = 8;

    [JsonProperty("font_size")]
    public double FontSize { get; set; } = 14;

    [JsonProperty("ui_scale")]
    public double UiScale { get; set; } = 1.0;

    [JsonProperty("compact_mode")]
    public bool CompactMode { get; set; }

    [JsonProperty("show_album_art")]
    public bool ShowAlbumArt { get; set; } = true;

    [JsonProperty("animations_enabled")]
    public bool AnimationsEnabled { get; set; } = true;
}

/// <summary>
/// Available application themes.
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    Amoled,
    Blue,
    Green,
    Purple,
    Red,
    Orange,
    Custom
}

/// <summary>
/// Download-related settings.
/// </summary>
public class DownloadSettings
{
    [JsonProperty("output_path")]
    public string OutputPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        "Spotify Downloads");

    [JsonProperty("output_format")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AudioFormat OutputFormat { get; set; } = AudioFormat.Mp3;

    [JsonProperty("quality")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AudioQuality Quality { get; set; } = AudioQuality.VeryHigh320;

    [JsonProperty("max_concurrent")]
    public int MaxConcurrentDownloads { get; set; } = 3;

    [JsonProperty("create_artist_folders")]
    public bool CreateArtistFolders { get; set; } = true;

    [JsonProperty("create_album_folders")]
    public bool CreateAlbumFolders { get; set; } = true;

    [JsonProperty("filename_template")]
    public string FilenameTemplate { get; set; } = "{trackNumber} - {title}";

    [JsonProperty("auto_download_new_liked")]
    public bool AutoDownloadNewLiked { get; set; }

    [JsonProperty("notify_on_complete")]
    public bool NotifyOnComplete { get; set; } = true;

    [JsonProperty("max_retries")]
    public int MaxRetries { get; set; } = 3;

    [JsonProperty("save_metadata")]
    public bool SaveMetadata { get; set; } = true;

    [JsonProperty("embed_cover_art")]
    public bool EmbedCoverArt { get; set; } = true;
}

/// <summary>
/// In-app player settings.
/// </summary>
public class PlayerSettings
{
    [JsonProperty("default_volume")]
    public double DefaultVolume { get; set; } = 0.8;

    [JsonProperty("remember_position")]
    public bool RememberPosition { get; set; } = true;

    [JsonProperty("crossfade_enabled")]
    public bool CrossfadeEnabled { get; set; }

    [JsonProperty("crossfade_duration")]
    public int CrossfadeDuration { get; set; } = 5;

    [JsonProperty("gapless_playback")]
    public bool GaplessPlayback { get; set; } = true;

    [JsonProperty("mini_player_on_top")]
    public bool MiniPlayerOnTop { get; set; } = true;

    [JsonProperty("mini_player_compact")]
    public bool MiniPlayerCompact { get; set; }
}

/// <summary>
/// UI and behavior settings.
/// </summary>
public class UISettings
{
    [JsonProperty("show_recently_played")]
    public bool ShowRecentlyPlayed { get; set; } = true;

    [JsonProperty("minimize_to_tray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonProperty("close_to_tray")]
    public bool CloseToTray { get; set; }

    [JsonProperty("start_minimized")]
    public bool StartMinimized { get; set; }

    [JsonProperty("show_notifications")]
    public bool ShowNotifications { get; set; } = true;

    [JsonProperty("show_track_notifications")]
    public bool ShowTrackNotifications { get; set; } = true;

    [JsonProperty("confirm_before_download")]
    public bool ConfirmBeforeDownload { get; set; } = true;
}

/// <summary>
/// Language settings.
/// </summary>
public class LanguageSettings
{
    [JsonProperty("language")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AppLanguage Language { get; set; } = AppLanguage.Portuguese;
}

/// <summary>
/// Available application languages.
/// </summary>
public enum AppLanguage
{
    Portuguese,
    English,
    Spanish
}

/// <summary>
/// Auto-update settings.
/// </summary>
public class UpdateSettings
{
    [JsonProperty("check_automatically")]
    public bool CheckAutomatically { get; set; } = true;

    [JsonProperty("include_prereleases")]
    public bool IncludePreReleases { get; set; }

    [JsonProperty("last_checked_version")]
    public string? LastCheckedVersion { get; set; }

    [JsonProperty("last_check_date")]
    public DateTime? LastCheckDate { get; set; }

    [JsonProperty("download_updates_automatically")]
    public bool DownloadUpdatesAutomatically { get; set; }
}

/// <summary>
/// Spotify-related settings (no authentication required).
/// </summary>
public class SpotifySettings
{
    [JsonProperty("user_id")]
    public string? UserId { get; set; }

    [JsonProperty("display_name")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Cache behavior settings.
/// </summary>
public class CacheSettings
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("max_cache_size_mb")]
    public int MaxCacheSizeMb { get; set; } = 500;

    [JsonProperty("cache_duration_hours")]
    public int CacheDurationHours { get; set; } = 24;

    [JsonProperty("cache_album_covers")]
    public bool CacheAlbumCovers { get; set; } = true;

    [JsonProperty("cache_search_results")]
    public bool CacheSearchResults { get; set; } = true;
}
