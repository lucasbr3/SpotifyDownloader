using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents the authenticated Spotify user's profile.
/// </summary>
public class UserProfile
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    [JsonProperty("product")]
    public string Product { get; set; } = string.Empty;

    [JsonProperty("country")]
    public string Country { get; set; } = string.Empty;

    [JsonProperty("followers")]
    public int Followers { get; set; }

    /// <summary>
    /// Returns the Spotify subscription tier label.
    /// </summary>
    [JsonIgnore]
    public string ProductFormatted => Product switch
    {
        "premium" => "Premium",
        "free" => "Free",
        "open" => "Open",
        _ => Product
    };
}
