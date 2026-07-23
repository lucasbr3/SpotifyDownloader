using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly IDownloadService _download;
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;
    private readonly INotificationService _notifications;
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty]
    private string _pageTitle = "Spotify Downloader";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSearchActive;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _spotifyLink = string.Empty;

    [ObservableProperty]
    private bool _isSuggestionsOpen;

    [ObservableProperty]
    private object? _selectedSuggestion;

    public string LikedCountText => $"{LikedTracks.Count} músicas curtidas";
    public string SearchPlaceholder => "Pesquise músicas, artistas ou cole um link do Spotify...";

    public ObservableCollection<SpotifyTrack> LikedTracks { get; } = new();
    public ObservableCollection<SpotifyPlaylist> Playlists { get; } = new();
    public ObservableCollection<SpotifyAlbum> Albums { get; } = new();
    public ObservableCollection<SpotifyTrack> SearchResults { get; } = new();
    public ObservableCollection<object> Suggestions { get; } = new();

    public MainViewModel()
    {
        _metadata = App.Metadata;
        _download = App.Download;
        _settings = App.Settings;
        _localization = App.Localization;
        _notifications = App.Notifications;

        _localization.LanguageChanged += (_, lang) =>
        {
            PageTitle = _localization["AppName"];
            OnPropertyChanged(nameof(PageTitle));
        };

        LikedTracks.CollectionChanged += (_, _) =>
            OnPropertyChanged(nameof(LikedCountText));
    }

    partial void OnSearchQueryChanged(string value)
    {
        DebounceSearch();
    }

    private void DebounceSearch()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (!string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        _ = SearchAsync(SearchQuery, isSuggestion: true);
                    }
                    else
                    {
                        Suggestions.Clear();
                        IsSuggestionsOpen = false;
                        SearchResults.Clear();
                        IsSearchActive = false;
                        StatusMessage = string.Empty;
                    }
                });
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }

    [RelayCommand]
    private async Task SearchOrPasteAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) && string.IsNullOrWhiteSpace(SpotifyLink))
            return;

        _debounceCts?.Cancel();

        var query = !string.IsNullOrWhiteSpace(SpotifyLink) ? SpotifyLink : SearchQuery;
        await SearchAsync(query, isSuggestion: false);
    }

    private async Task SearchAsync(string query, bool isSuggestion)
    {
        IsLoading = true;
        IsSearchActive = true;

        try
        {
            if (_metadata.ParseSpotifyLink(query) != null)
            {
                IsSuggestionsOpen = false;
                var content = await _metadata.LoadFromLinkAsync(query);
                StatusMessage = content != null
                    ? $"Carregado: {(content as SpotifyTrack)?.Title ?? "Conteúdo"}"
                    : "Não foi possível carregar este link";
                return;
            }

            var results = await _metadata.SearchAsync(query, 20);

            if (isSuggestion)
            {
                Suggestions.Clear();
                foreach (var track in results.Tracks.Take(5))
                    Suggestions.Add(track);
                IsSuggestionsOpen = Suggestions.Count > 0;
            }
            else
            {
                IsSuggestionsOpen = false;
                SearchResults.Clear();
                foreach (var track in results.Tracks)
                    SearchResults.Add(track);
                StatusMessage = $"{results.Tracks.Count} resultados encontrados";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Search failed");
            StatusMessage = "Falha na pesquisa";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedSuggestionChanged(object? value)
    {
        if (value is SpotifyTrack track)
        {
            SearchQuery = track.Title;
            IsSuggestionsOpen = false;
            _debounceCts?.Cancel();
            _ = SearchAsync(track.Title, isSuggestion: false);
        }
    }

    [RelayCommand]
    private async Task DownloadTrackAsync(SpotifyTrack? track)
    {
        if (track == null) return;
        try
        {
            var settings = await _settings.LoadAsync();
            await _download.DownloadTrackAsync(
                track, settings.Download.Quality, settings.Download.OutputFormat,
                settings.Download.OutputPath,
                new Progress<double>(p => { }));
            _notifications.ShowDownloadComplete(track.Title);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Download failed for {Track}", track?.Title);
        }
    }

    [RelayCommand]
    private async Task DownloadAllTracksAsync(IList<SpotifyTrack>? tracks)
    {
        if (tracks == null || tracks.Count == 0) return;
        try
        {
            var settings = await _settings.LoadAsync();
            await _download.DownloadBatchAsync(
                tracks.ToList(), settings.Download.Quality, settings.Download.OutputFormat,
                settings.Download.OutputPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Batch download failed");
        }
    }

    [RelayCommand]
    private void NavigateToPage(string page)
    {
        PageNavigation?.Invoke(this, page);
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        NavigateToPage("settings");
    }

    public event EventHandler<string>? PageNavigation;
}
