using Serilog;
using SpotifyDownloader.Core.Interfaces;

namespace SpotifyDownloader.Core.Services;

public class LyricsService : ILyricsService
{
    private readonly HttpClient _http;
    private readonly ICacheService _cache;

    public LyricsService(ICacheService cache)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
        _http.Timeout = TimeSpan.FromSeconds(8);
        _cache = cache;
    }

    public async Task<string> GetLyricsAsync(string artist, string title)
    {
        var cacheKey = $"lyrics_{artist}_{title}".ToLowerInvariant();
        var cached = await _cache.GetAsync<string>(cacheKey);
        if (cached != null)
            return cached;

        try
        {
            var searchQuery = Uri.EscapeDataString($"{artist} {title} lyrics");
            var searchUrl = $"https://www.google.com/search?q={searchQuery}";
            var searchHtml = await _http.GetStringAsync(searchUrl);

            var lyrics = ExtractFromGoogleResult(searchHtml);
            if (string.IsNullOrEmpty(lyrics))
                lyrics = await TryGeniusAsync(artist, title);

            if (!string.IsNullOrEmpty(lyrics))
            {
                await _cache.SetAsync(cacheKey, lyrics, TimeSpan.FromDays(30));
                return lyrics;
            }

            return "Letra não encontrada.";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch lyrics for {Artist} - {Title}", artist, title);
            return "Erro ao carregar letra.";
        }
    }

    public async Task<bool> HasLyricsAsync(string artist, string title)
    {
        var lyrics = await GetLyricsAsync(artist, title);
        return !string.IsNullOrEmpty(lyrics) &&
               !lyrics.Contains("não encontrada") &&
               !lyrics.Contains("Erro ao");
    }

    private static string ExtractFromGoogleResult(string html)
    {
        var startMarker = "<div class=\"lyrics\">";
        var start = html.IndexOf(startMarker);
        if (start < 0)
        {
            var altMarker = "<span class=\"hwc\">";
            start = html.IndexOf(altMarker);
            if (start >= 0)
            {
                var endPos = html.IndexOf("</span>", start);
                if (endPos > start)
                    return html.Substring(start + altMarker.Length, endPos - start - altMarker.Length);
            }
            return string.Empty;
        }

        start += startMarker.Length;
        var endTag = "</div>";
        var end = html.IndexOf(endTag, start);
        if (end > start)
        {
            var raw = html.Substring(start, end - start);
            return System.Net.WebUtility.HtmlDecode(raw)
                .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
                .Replace("<div>", "").Replace("</div>", "");
        }

        return string.Empty;
    }

    private async Task<string> TryGeniusAsync(string artist, string title)
    {
        try
        {
            var searchQuery = Uri.EscapeDataString($"{artist} {title}");
            var searchUrl = $"https://genius.com/api/search/song?q={searchQuery}";
            var json = await _http.GetStringAsync(searchUrl);

            var resultStart = json.IndexOf("\"url\":\"");
            if (resultStart < 0) return string.Empty;

            resultStart += "\"url\":\"".Length;
            var resultEnd = json.IndexOf("\"", resultStart);
            if (resultEnd <= resultStart) return string.Empty;

            var songUrl = json.Substring(resultStart, resultEnd - resultStart)
                .Replace("\\/", "/");
            if (string.IsNullOrEmpty(songUrl)) return string.Empty;

            var pageHtml = await _http.GetStringAsync(songUrl);
            return ExtractLyricsFromGenius(pageHtml);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Genius lookup failed for {Artist} - {Title}", artist, title);
            return string.Empty;
        }
    }

    private static string ExtractLyricsFromGenius(string html)
    {
        var startMarker = "<div data-lyrics-container=\"true\">";
        var start = html.IndexOf(startMarker);
        if (start < 0) return string.Empty;

        start += startMarker.Length;
        var depth = 1;
        var end = start;

        for (int i = start; i < html.Length - 6; i++)
        {
            if (html[i] == '<' && html.Substring(i).StartsWith("<div"))
                depth++;
            else if (html[i] == '<' && html.Substring(i).StartsWith("</div"))
            {
                depth--;
                if (depth == 0)
                {
                    end = i;
                    break;
                }
            }
        }

        if (end <= start) return string.Empty;

        var raw = html.Substring(start, end - start);
        return System.Net.WebUtility.HtmlDecode(raw)
            .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
            .Replace("</p>", "\n").Replace("<p>", "")
            .Replace("</a>", "").Split('<')[0];
    }
}
