using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Result of a search query containing tracks, artists, albums, and playlists.
/// </summary>
public class SearchResults
{
    [JsonProperty("tracks")]
    public List<SpotifyTrack> Tracks { get; set; } = new();

    [JsonProperty("artists")]
    public List<SpotifyArtist> Artists { get; set; } = new();

    [JsonProperty("albums")]
    public List<SpotifyAlbum> Albums { get; set; } = new();

    [JsonProperty("playlists")]
    public List<SpotifyPlaylist> Playlists { get; set; } = new();

    [JsonProperty("total_results")]
    public int TotalResults { get; set; }

    [JsonIgnore]
    public bool HasResults => Tracks.Count > 0 || Artists.Count > 0 || Albums.Count > 0 || Playlists.Count > 0;
}

/// <summary>
/// Information about a new application update.
/// </summary>
public class UpdateInfo
{
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonProperty("changelog")]
    public string Changelog { get; set; } = string.Empty;

    [JsonProperty("release_date")]
    public DateTime ReleaseDate { get; set; }

    [JsonProperty("is_prerelease")]
    public bool IsPreRelease { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("sha256_hash")]
    public string Sha256Hash { get; set; } = string.Empty;

    /// <summary>
    /// Returns the file size in a readable format.
    /// </summary>
    [JsonIgnore]
    public string FileSizeFormatted => FileSize switch
    {
        < 1_048_576 => $"{FileSize / 1024.0:F1} KB",
        < 1_073_741_824 => $"{FileSize / 1_048_576.0:F1} MB",
        _ => $"{FileSize / 1_073_741_824.0:F2} GB"
    };
}

/// <summary>
/// Progress information for batch download operations.
/// </summary>
public class BatchDownloadProgress
{
    [JsonProperty("completed")]
    public int Completed { get; set; }

    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("current_track")]
    public string? CurrentTrack { get; set; }

    [JsonProperty("current_artist")]
    public string? CurrentArtist { get; set; }

    [JsonIgnore]
    public double OverallPercent => Total > 0 ? (double)Completed / Total * 100 : 0;
}

/// <summary>
/// Represents a Spotify URL or URI parsed into components.
/// </summary>
public class SpotifyLinkInfo
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("original_url")]
    public string OriginalUrl { get; set; } = string.Empty;

    [JsonProperty("is_valid")]
    public bool IsValid => !string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Id);

    /// <summary>
    /// Parsed entity type enum.
    /// </summary>
    [JsonIgnore]
    public SpotifyEntityType EntityType => Type.ToLower() switch
    {
        "track" => SpotifyEntityType.Track,
        "album" => SpotifyEntityType.Album,
        "playlist" => SpotifyEntityType.Playlist,
        "artist" => SpotifyEntityType.Artist,
        _ => SpotifyEntityType.Unknown
    };
}

/// <summary>
/// Spotify entity types for link parsing.
/// </summary>
public enum SpotifyEntityType
{
    Unknown,
    Track,
    Album,
    Playlist,
    Artist
}
