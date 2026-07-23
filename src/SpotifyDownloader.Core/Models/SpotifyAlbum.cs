using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents a Spotify album with full metadata.
/// </summary>
public class SpotifyAlbum
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonProperty("artist_id")]
    public string ArtistId { get; set; } = string.Empty;

    [JsonProperty("cover_url")]
    public string CoverUrl { get; set; } = string.Empty;

    [JsonProperty("cover_url_hd")]
    public string CoverUrlHd { get; set; } = string.Empty;

    [JsonProperty("release_year")]
    public int ReleaseYear { get; set; }

    [JsonProperty("release_date")]
    public string ReleaseDate { get; set; } = string.Empty;

    [JsonProperty("album_type")]
    public string AlbumType { get; set; } = string.Empty;

    [JsonProperty("total_tracks")]
    public int TotalTracks { get; set; }

    [JsonProperty("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    [JsonProperty("copyright")]
    public string Copyright { get; set; } = string.Empty;

    [JsonProperty("spotify_uri")]
    public string SpotifyUri { get; set; } = string.Empty;

    [JsonProperty("popularity")]
    public int Popularity { get; set; }

    [JsonProperty("tracks")]
    public List<SpotifyTrack> Tracks { get; set; } = new();

    /// <summary>
    /// Returns the album type label in Portuguese.
    /// </summary>
    [JsonIgnore]
    public string AlbumTypeFormatted => AlbumType switch
    {
        "album" => "Álbum",
        "single" => "Single",
        "compilation" => "Compilação",
        _ => AlbumType
    };

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
