using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents a Spotify playlist with metadata and tracks.
/// </summary>
public class SpotifyPlaylist
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("owner")]
    public string Owner { get; set; } = string.Empty;

    [JsonProperty("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonProperty("cover_url")]
    public string CoverUrl { get; set; } = string.Empty;

    [JsonProperty("cover_url_hd")]
    public string CoverUrlHd { get; set; } = string.Empty;

    [JsonProperty("track_count")]
    public int TrackCount { get; set; }

    [JsonProperty("spotify_uri")]
    public string SpotifyUri { get; set; } = string.Empty;

    [JsonProperty("is_public")]
    public bool IsPublic { get; set; }

    [JsonProperty("is_collaborative")]
    public bool IsCollaborative { get; set; }

    [JsonProperty("is_owner")]
    public bool IsOwner { get; set; }

    [JsonProperty("snapshot_id")]
    public string SnapshotId { get; set; } = string.Empty;

    [JsonProperty("tracks")]
    public List<SpotifyTrack> Tracks { get; set; } = new();

    /// <summary>
    /// Returns the playlist description without HTML tags.
    /// </summary>
    [JsonIgnore]
    public string DescriptionPlain =>
        System.Text.RegularExpressions.Regex.Replace(Description, "<.*?>", string.Empty);

    /// <summary>
    /// Returns the total duration formatted.
    /// </summary>
    [JsonIgnore]
    public string TotalDurationFormatted
    {
        get
        {
            var totalMs = Tracks.Sum(t => t.DurationMs);
            var ts = TimeSpan.FromMilliseconds(totalMs);
            return ts.Hours > 0
                ? $"{ts.Hours}h {ts.Minutes}min"
                : $"{ts.Minutes}min {ts.Seconds}s";
        }
    }

    /// <summary>
    /// Returns the best available cover URL.
    /// </summary>
    [JsonIgnore]
    public string BestCoverUrl =>
        !string.IsNullOrEmpty(CoverUrlHd) ? CoverUrlHd : CoverUrl;
}
