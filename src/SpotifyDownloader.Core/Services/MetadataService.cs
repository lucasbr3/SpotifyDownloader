using System.Text.RegularExpressions;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// Searches music metadata from public sources without authentication.
/// Uses YouTube search for audio sources and extracts Spotify metadata from public pages.
/// </summary>
public class MetadataService : IMetadataService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cache;

    public MetadataService(ICacheService cache)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" } }
        };
        _cache = cache;
    }

    public async Task<SearchResults> SearchAsync(string query, int limit = 20)
    {
        var results = new SearchResults();

        try
        {
            var encoded = Uri.EscapeDataString(query);
            var url = $"https://www.youtube.com/results?search_query={encoded}";
            var html = await _httpClient.GetStringAsync(url);

            var videoIds = ExtractVideoIds(html, limit);
            var tracks = new List<SpotifyTrack>();

            foreach (var videoId in videoIds)
            {
                var track = new SpotifyTrack
                {
                    Id = videoId,
                    Title = ExtractTitleFromVideoId(videoId) ?? $"Video {videoId}",
                    Artist = "YouTube",
                    Album = "YouTube Audio",
                    AlbumCoverUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg",
                    AlbumCoverUrlHd = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg",
                    DurationMs = 0,
                    SpotifyUri = $"https://www.youtube.com/watch?v={videoId}",
                    IsPlayable = true
                };
                tracks.Add(track);
            }

            results.Tracks = tracks;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Search failed for {Query}", query);
        }

        return results;
    }

    public async Task<SpotifyTrack?> GetTrackAsync(string queryOrUrl)
    {
        try
        {
            var sourceUrl = await GetAudioSourceUrlAsync("", queryOrUrl);
            if (string.IsNullOrEmpty(sourceUrl)) return null;

            var videoId = ExtractVideoIdFromUrl(sourceUrl);
            if (string.IsNullOrEmpty(videoId)) return null;

            return new SpotifyTrack
            {
                Id = videoId,
                Title = queryOrUrl,
                Artist = "YouTube",
                Album = "YouTube Audio",
                AlbumCoverUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg",
                AlbumCoverUrlHd = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg",
                SpotifyUri = sourceUrl,
                IsPlayable = true
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get track from {Query}", queryOrUrl);
            return null;
        }
    }

    public async Task<object?> LoadFromLinkAsync(string url)
    {
        var link = ParseSpotifyLink(url);
        if (link == null || !link.IsValid) return null;

        try
        {
            var cacheKey = $"spotify_public:{link.Type}:{link.Id}";
            var cached = await _cache.GetAsync<SpotifyTrack>(cacheKey);
            if (cached != null) return cached;

            var metadataUrl = link.Type switch
            {
                "track" => $"https://open.spotify.com/track/{link.Id}",
                "album" => $"https://open.spotify.com/album/{link.Id}",
                "playlist" => $"https://open.spotify.com/playlist/{link.Id}",
                "artist" => $"https://open.spotify.com/artist/{link.Id}",
                _ => null
            };

            if (metadataUrl == null) return null;

            var html = await _httpClient.GetStringAsync(metadataUrl);

            var title = ExtractMetaProperty(html, "og:title") ?? link.Id;
            var description = ExtractMetaProperty(html, "og:description") ?? "";
            var image = ExtractMetaProperty(html, "og:image") ?? "";

            var track = new SpotifyTrack
            {
                Id = link.Id,
                Title = title,
                Artist = description.Split('-').FirstOrDefault()?.Trim() ?? "Unknown",
                Album = "Spotify",
                AlbumCoverUrl = image,
                AlbumCoverUrlHd = image,
                SpotifyUri = url,
                IsPlayable = true
            };

            await _cache.SetAsync(cacheKey, track, TimeSpan.FromHours(24));
            return track;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load Spotify link: {Url}", url);
            return null;
        }
    }

    public SpotifyLinkInfo? ParseSpotifyLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var patterns = new (string Pattern, string Type)[]
        {
            (@"open\.spotify\.com/track/([a-zA-Z0-9]+)", "track"),
            (@"open\.spotify\.com/album/([a-zA-Z0-9]+)", "album"),
            (@"open\.spotify\.com/playlist/([a-zA-Z0-9]+)", "playlist"),
            (@"open\.spotify\.com/artist/([a-zA-Z0-9]+)", "artist"),
            (@"spotify:track:([a-zA-Z0-9]+)", "track"),
            (@"spotify:album:([a-zA-Z0-9]+)", "album"),
            (@"spotify:playlist:([a-zA-Z0-9]+)", "playlist"),
            (@"spotify:artist:([a-zA-Z0-9]+)", "artist")
        };

        foreach (var (pattern, type) in patterns)
        {
            var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return new SpotifyLinkInfo
                {
                    Type = type,
                    Id = match.Groups[1].Value,
                    OriginalUrl = url
                };
            }
        }

        return null;
    }

    public async Task<string?> GetAudioSourceUrlAsync(string artist, string title)
    {
        try
        {
            var query = string.IsNullOrEmpty(artist)
                ? Uri.EscapeDataString(title)
                : Uri.EscapeDataString($"{artist} {title} audio");

            var url = $"https://www.youtube.com/results?search_query={query}";
            var html = await _httpClient.GetStringAsync(url);

            var match = Regex.Match(html, "/watch\\?v=([a-zA-Z0-9_-]{11})");
            if (match.Success)
                return $"https://www.youtube.com/watch?v={match.Groups[1].Value}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to find audio source for {Artist} - {Title}", artist, title);
        }
        return null;
    }

    public Task<SpotifyTrack?> ExtractMetadataFromYouTubeAsync(string videoUrl)
    {
        try
        {
            var videoId = ExtractVideoIdFromUrl(videoUrl);
            if (string.IsNullOrEmpty(videoId)) return Task.FromResult<SpotifyTrack?>(null);

            return Task.FromResult<SpotifyTrack?>(new SpotifyTrack
            {
                Id = videoId,
                Title = $"YouTube Video {videoId}",
                Artist = "YouTube",
                Album = "YouTube Audio",
                AlbumCoverUrl = $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg",
                AlbumCoverUrlHd = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg",
                SpotifyUri = videoUrl,
                IsPlayable = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to extract YouTube metadata");
            return Task.FromResult<SpotifyTrack?>(null);
        }
    }

    private static List<string> ExtractVideoIds(string html, int maxResults)
    {
        var ids = new List<string>();
        var matches = Regex.Matches(html, "/watch\\?v=([a-zA-Z0-9_-]{11})");
        foreach (Match match in matches)
        {
            var id = match.Groups[1].Value;
            if (!ids.Contains(id))
            {
                ids.Add(id);
                if (ids.Count >= maxResults) break;
            }
        }
        return ids;
    }

    private static string? ExtractVideoIdFromUrl(string url)
    {
        var match = Regex.Match(url, "(?:youtube\\.com/watch\\?v=|youtu\\.be/)([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractTitleFromVideoId(string videoId)
    {
        return null;
    }

    private static string? ExtractMetaProperty(string html, string property)
    {
        var match = Regex.Match(html,
            $"<meta[^>]+property=\"(?:{property}|{property.Replace("og:", "")})\"[^>]+content=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html,
                $"<meta[^>]+content=\"([^\"]+)\"[^>]+property=\"(?:{property}|{property.Replace("og:", "")})\"", RegexOptions.IgnoreCase);
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }
}
