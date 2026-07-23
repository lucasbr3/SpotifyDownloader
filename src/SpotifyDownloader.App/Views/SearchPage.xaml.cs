using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.Views;

public sealed partial class SearchPage : Page
{
    public ViewModels.MainViewModel ViewModel => App.GetService<ViewModels.MainViewModel>()!;

    public SearchPage()
    {
        InitializeComponent();
    }

    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SpotifyTrack track)
        {
            ViewModel.SearchQuery = track.Title;
        }
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            ViewModel.SearchQuery = args.QueryText;
            _ = ViewModel.SearchOrPasteCommand.ExecuteAsync(null);
        }
    }

    private void OnTrackClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SpotifyTrack track)
        {
            _ = ViewModel.DownloadTrackCommand.ExecuteAsync(track);
        }
    }
}
