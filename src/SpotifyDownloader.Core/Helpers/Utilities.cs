using System.Text.RegularExpressions;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Helpers;

/// <summary>
/// Utility class for parsing Spotify URLs and URIs.
/// </summary>
public static class SpotifyLinkParser
{
    /// <summary>
    /// Parses a Spotify URL or URI into its components.
    /// Supported formats:
    ///   https://open.spotify.com/track/ID
    ///   https://open.spotify.com/album/ID
    ///   https://open.spotify.com/playlist/ID
    ///   https://open.spotify.com/artist/ID
    ///   spotify:track:ID
    ///   spotify:album:ID
    ///   spotify:playlist:ID
    ///   spotify:artist:ID
    /// </summary>
    public static SpotifyLinkInfo? Parse(string url)
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

    /// <summary>
    /// Extracts the Spotify entity ID from a URL or URI.
    /// </summary>
    public static string? ExtractId(string url)
    {
        return Parse(url)?.Id;
    }

    /// <summary>
    /// Determines if the given text is a Spotify URL or URI.
    /// </summary>
    public static bool IsSpotifyLink(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               (text.Contains("spotify.com") || text.StartsWith("spotify:"));
    }
}

/// <summary>
/// Utility class for formatting various data types.
/// </summary>
public static class FormatHelper
{
    /// <summary>
    /// Formats a byte count into a human-readable string.
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1_048_576 => $"{bytes / 1024.0:F1} KB",
            < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
            _ => $"{bytes / 1_073_741_824.0:F2} GB"
        };
    }

    /// <summary>
    /// Formats milliseconds into a human-readable duration.
    /// </summary>
    public static string FormatDuration(int milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"mm\:ss");
    }

    /// <summary>
    /// Formats a speed value in bytes/sec into a human-readable string.
    /// </summary>
    public static string FormatSpeed(double bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            < 1024 => $"{bytesPerSecond:F0} B/s",
            < 1_048_576 => $"{bytesPerSecond / 1024.0:F1} KB/s",
            _ => $"{bytesPerSecond / 1_048_576.0:F1} MB/s"
        };
    }

    /// <summary>
    /// Formats a TimeSpan into a remaining-time string.
    /// </summary>
    public static string FormatRemainingTime(TimeSpan? time)
    {
        if (time == null) return "--:--";
        return time.Value.TotalHours >= 1
            ? time.Value.ToString(@"h\:mm\:ss")
            : time.Value.ToString(@"mm\:ss");
    }

    /// <summary>
    /// Formats a number with K/M suffix.
    /// </summary>
    public static string FormatNumber(int number)
    {
        return number switch
        {
            >= 1_000_000 => $"{number / 1_000_000.0:F1}M",
            >= 1_000 => $"{number / 1_000.0:F1}K",
            _ => number.ToString()
        };
    }

    /// <summary>
    /// Sanitizes a string for use as a file name.
    /// </summary>
    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized.Trim();
    }
}
