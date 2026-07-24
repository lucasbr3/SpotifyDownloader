namespace SpotifyDownloader.Shared;

public class SpotifyArtist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Followers { get; set; }
    public List<string> Genres { get; set; } = new();
}
