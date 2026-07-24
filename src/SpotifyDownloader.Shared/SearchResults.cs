namespace SpotifyDownloader.Shared;

public class SearchResults
{
    public List<SpotifyTrack> Tracks { get; set; } = new();
    public List<SpotifyAlbum> Albums { get; set; } = new();
    public List<SpotifyPlaylist> Playlists { get; set; } = new();
    public List<SpotifyArtist> Artists { get; set; } = new();
    public int TotalCount => Tracks.Count + Albums.Count + Playlists.Count + Artists.Count;
    public bool HasResults => TotalCount > 0;
}
