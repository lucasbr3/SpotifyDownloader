namespace SpotifyDownloader.Shared;

public class SpotifyPlaylist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int TrackCount { get; set; }
    public List<SpotifyTrack> Tracks { get; set; } = new();
}
