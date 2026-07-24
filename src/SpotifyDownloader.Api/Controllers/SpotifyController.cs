using Microsoft.AspNetCore.Mvc;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;
using SpotifyDownloader.Shared;

namespace SpotifyDownloader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpotifyController : ControllerBase
{
    private readonly IMetadataService _metadata;
    private readonly IDownloadService _download;
    private readonly ISettingsService _settings;

    public SpotifyController(IMetadataService metadata, IDownloadService download, ISettingsService settings)
    {
        _metadata = metadata;
        _download = download;
        _settings = settings;
    }

    [HttpGet("search")]
    public async Task<ActionResult<Shared.SearchResults>> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query is required");

        var results = await _metadata.SearchAsync(q, limit);
        return new Shared.SearchResults
        {
            Tracks = results.Tracks.Select(Map).ToList(),
            Albums = results.Albums.Select(a => new Shared.SpotifyAlbum
            {
                Id = a.Id, Name = a.Name, Artist = a.Artist,
                CoverUrl = a.CoverUrl, TotalTracks = a.TotalTracks,
                ReleaseYear = a.ReleaseYear
            }).ToList(),
            Playlists = results.Playlists.Select(p => new Shared.SpotifyPlaylist
            {
                Id = p.Id, Name = p.Name, Description = p.Description,
                CoverUrl = p.CoverUrl, Owner = p.Owner, TrackCount = p.TrackCount
            }).ToList()
        };
    }

    [HttpGet("track")]
    public async Task<ActionResult<Shared.SpotifyTrack?>> GetTrack([FromQuery] string url)
    {
        var track = await _metadata.GetTrackAsync(url);
        return track != null ? Ok(Map(track)) : NotFound();
    }

    [HttpPost("download")]
    public async Task<ActionResult> Download([FromBody] DownloadRequest request)
    {
        var track = new Core.Models.SpotifyTrack
        {
            Id = request.TrackId,
            Title = request.Title,
            Artist = request.Artist,
            Album = request.Album,
            AlbumCoverUrl = request.CoverUrl,
            DurationMs = request.DurationMs
        };

        var settings = await _settings.LoadAsync();
        var item = await _download.DownloadTrackAsync(track, settings.Download.Quality,
            settings.Download.OutputFormat, settings.Download.OutputPath);

        if (item.Status == Core.Models.DownloadStatus.Completed && System.IO.File.Exists(item.OutputPath))
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(item.OutputPath);
            var fileName = Path.GetFileName(item.OutputPath);
            return File(bytes, "audio/mpeg", fileName);
        }

        return BadRequest(new { error = item.ErrorMessage });
    }

    [HttpGet("ffmpeg-status")]
    public async Task<ActionResult> CheckFfmpeg()
    {
        var available = await _download.IsFfmpegAvailableAsync();
        var path = await _download.GetFfmpegPathAsync();
        return Ok(new { available, path });
    }

    private static Shared.SpotifyTrack Map(Core.Models.SpotifyTrack t) => new()
    {
        Id = t.Id, Title = t.Title, Artist = t.Artist, ArtistId = t.ArtistId,
        Album = t.Album, AlbumId = t.AlbumId, AlbumCoverUrl = t.AlbumCoverUrl,
        AlbumCoverUrlHd = t.AlbumCoverUrlHd, DurationMs = t.DurationMs,
        TrackNumber = t.TrackNumber, DiscNumber = t.DiscNumber,
        ReleaseYear = t.ReleaseYear, ReleaseDate = t.ReleaseDate,
        Genres = t.Genres, PreviewUrl = t.PreviewUrl, SpotifyUri = t.SpotifyUri,
        IsExplicit = t.IsExplicit, Popularity = t.Popularity, IsPlayable = t.IsPlayable
    };
}

public class DownloadRequest
{
    public string TrackId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public int DurationMs { get; set; }
}
