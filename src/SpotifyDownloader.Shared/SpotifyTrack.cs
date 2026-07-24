namespace SpotifyDownloader.Shared;

public class SpotifyTrack
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string ArtistId { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string AlbumId { get; set; } = string.Empty;
    public string AlbumCoverUrl { get; set; } = string.Empty;
    public string AlbumCoverUrlHd { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public int TrackNumber { get; set; }
    public int DiscNumber { get; set; }
    public int ReleaseYear { get; set; }
    public string ReleaseDate { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public string PreviewUrl { get; set; } = string.Empty;
    public string SpotifyUri { get; set; } = string.Empty;
    public bool IsExplicit { get; set; }
    public int Popularity { get; set; }
    public bool IsPlayable { get; set; } = true;
    public string DurationFormatted => TimeSpan.FromMilliseconds(DurationMs).ToString(@"mm\:ss");
    public string ArtistAlbum => $"{Artist} • {Album}";
    public string ExplicitLabel => IsExplicit ? "E" : "";
    public string BestCoverUrl => !string.IsNullOrEmpty(AlbumCoverUrlHd) ? AlbumCoverUrlHd : AlbumCoverUrl;
}
