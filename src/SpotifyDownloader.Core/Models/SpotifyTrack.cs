using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents a complete Spotify track with all metadata.
/// </summary>
public class SpotifyTrack
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty("artist_id")]
    public string ArtistId { get; set; } = string.Empty;

    [JsonProperty("album")]
    public string Album { get; set; } = string.Empty;

    [JsonProperty("album_id")]
    public string AlbumId { get; set; } = string.Empty;

    [JsonProperty("album_cover_url")]
    public string AlbumCoverUrl { get; set; } = string.Empty;

    [JsonProperty("album_cover_url_hd")]
    public string AlbumCoverUrlHd { get; set; } = string.Empty;

    [JsonProperty("duration_ms")]
    public int DurationMs { get; set; }

    [JsonProperty("track_number")]
    public int TrackNumber { get; set; }

    [JsonProperty("disc_number")]
    public int DiscNumber { get; set; }

    [JsonProperty("release_year")]
    public int ReleaseYear { get; set; }

    [JsonProperty("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonProperty("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonProperty("preview_url")]
    public string PreviewUrl { get; set; } = string.Empty;

    [JsonProperty("spotify_uri")]
    public string SpotifyUri { get; set; } = string.Empty;

    [JsonProperty("is_explicit")]
    public bool IsExplicit { get; set; }

    [JsonProperty("popularity")]
    public int Popularity { get; set; }

    [JsonProperty("is_playable")]
    public bool IsPlayable { get; set; } = true;

    /// <summary>
    /// Returns duration in mm:ss format.
    /// </summary>
    [JsonIgnore]
    public string DurationFormatted =>
        TimeSpan.FromMilliseconds(DurationMs).ToString(@"mm\:ss");

    /// <summary>
    /// Returns combined artist and album string.
    /// </summary>
    [JsonIgnore]
    public string ArtistAlbum => $"{Artist} • {Album}";

    /// <summary>
    /// Returns explicit label if track is explicit.
    /// </summary>
    [JsonIgnore]
    public string ExplicitLabel => IsExplicit ? "E" : "";

    /// <summary>
    /// Returns the HD cover URL or falls back to the standard one.
    /// </summary>
    [JsonIgnore]
    public string BestCoverUrl =>
        !string.IsNullOrEmpty(AlbumCoverUrlHd) ? AlbumCoverUrlHd : AlbumCoverUrl;
}
