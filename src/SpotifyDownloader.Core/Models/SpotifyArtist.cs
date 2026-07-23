using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents a Spotify artist with profile information.
/// </summary>
public class SpotifyArtist
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonProperty("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonProperty("followers")]
    public int Followers { get; set; }

    [JsonProperty("popularity")]
    public int Popularity { get; set; }

    [JsonProperty("spotify_uri")]
    public string SpotifyUri { get; set; } = string.Empty;

    [JsonProperty("albums")]
    public List<SpotifyAlbum> Albums { get; set; } = new();

    [JsonProperty("top_tracks")]
    public List<SpotifyTrack> TopTracks { get; set; } = new();

    /// <summary>
    /// Returns the follower count in a readable format.
    /// </summary>
    [JsonIgnore]
    public string FollowersFormatted =>
        Followers >= 1000000
            ? $"{Followers / 1000000.0:F1}M seguidores"
            : Followers >= 1000
                ? $"{Followers / 1000.0:F1}K seguidores"
                : $"{Followers} seguidores";
}
