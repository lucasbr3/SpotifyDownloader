using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.ViewModels;

/// <summary>
/// ViewModel for search results display.
/// </summary>
public partial class SearchViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private SpotifyTrack? _selectedTrack;

    [ObservableProperty]
    private SpotifyAlbum? _selectedAlbum;

    [ObservableProperty]
    private SpotifyPlaylist? _selectedPlaylist;

    [ObservableProperty]
    private SpotifyArtist? _selectedArtist;

    [ObservableProperty]
    private string _resultStatus = string.Empty;

    public ObservableCollection<SpotifyTrack> Tracks { get; } = new();
    public ObservableCollection<SpotifyAlbum> Albums { get; } = new();
    public ObservableCollection<SpotifyPlaylist> Playlists { get; } = new();
    public ObservableCollection<SpotifyArtist> Artists { get; } = new();

    public SearchViewModel()
    {
        _metadata = App.Metadata;
        _localization = App.Localization;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;

        IsLoading = true;
        HasResults = false;

        try
        {
            if (Helpers.SpotifyLinkParser.IsSpotifyLink(Query))
            {
                var link = Helpers.SpotifyLinkParser.Parse(Query);
                if (link != null && link.IsValid)
                {
                    var content = await _metadata.LoadFromLinkAsync(link.OriginalUrl);
                    LoadContent(content);
                    return;
                }
            }

            var results = await _metadata.SearchAsync(Query, 30);
            LoadResults(results);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Search failed");
            ResultStatus = "Falha na pesquisa";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadResults(SearchResults results)
    {
        Tracks.Clear();
        Albums.Clear();
        Playlists.Clear();
        Artists.Clear();

        foreach (var t in results.Tracks) Tracks.Add(t);
        foreach (var a in results.Albums) Albums.Add(a);
        foreach (var p in results.Playlists) Playlists.Add(p);
        foreach (var a in results.Artists) Artists.Add(a);

        HasResults = results.HasResults;
        ResultStatus = HasResults
            ? $"{results.TotalResults} resultados encontrados"
            : "Nenhum resultado encontrado";
    }

    private void LoadContent(object? content)
    {
        Tracks.Clear();
        Albums.Clear();
        Playlists.Clear();
        Artists.Clear();

        switch (content)
        {
            case SpotifyTrack track:
                Tracks.Add(track);
                HasResults = true;
                ResultStatus = $"Faixa: {track.Title}";
                break;
            case SpotifyAlbum album:
                Albums.Add(album);
                foreach (var t in album.Tracks) Tracks.Add(t);
                HasResults = true;
                ResultStatus = $"Álbum: {album.Name} - {album.Tracks.Count} faixas";
                break;
            case SpotifyPlaylist playlist:
                Playlists.Add(playlist);
                foreach (var t in playlist.Tracks) Tracks.Add(t);
                HasResults = true;
                ResultStatus = $"Playlist: {playlist.Name} - {playlist.Tracks.Count} faixas";
                break;
            case SpotifyArtist artist:
                Artists.Add(artist);
                foreach (var t in artist.TopTracks) Tracks.Add(t);
                HasResults = true;
                ResultStatus = $"Artista: {artist.Name}";
                break;
            default:
                HasResults = false;
                ResultStatus = "Não foi possível carregar este conteúdo";
                break;
        }
    }

    [RelayCommand]
    private async Task DownloadTrackAsync(SpotifyTrack? track)
    {
        if (track == null) return;
        try
        {
            var settings = await App.Settings.LoadAsync();
            var item = await App.Download.DownloadTrackAsync(
                track, settings.Download.Quality, settings.Download.OutputFormat,
                settings.Download.OutputPath);
            App.Notifications.ShowDownloadComplete(track.Title);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download track {Track}", track?.Title);
        }
    }

    [RelayCommand]
    private async Task DownloadAllTracksAsync()
    {
        if (Tracks.Count == 0) return;
        try
        {
            var settings = await App.Settings.LoadAsync();
            await App.Download.DownloadBatchAsync(
                Tracks.ToList(), settings.Download.Quality, settings.Download.OutputFormat,
                settings.Download.OutputPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Batch download failed");
        }
    }

    [RelayCommand]
    private void PlayTrack(SpotifyTrack? track)
    {
        if (track == null) return;
        TrackPlayRequested?.Invoke(this, track);
    }

    /// <summary>
    /// Raised when a track should be played.
    /// </summary>
    public event EventHandler<SpotifyTrack>? TrackPlayRequested;
}
