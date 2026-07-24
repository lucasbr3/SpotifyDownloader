namespace SpotifyDownloader.Shared;

public class SpotifyAlbum
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public int TotalTracks { get; set; }
    public int ReleaseYear { get; set; }
    public List<SpotifyTrack> Tracks { get; set; } = new();
}
