using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpotifyDownloader.App.ViewModels;

namespace SpotifyDownloader.App.Views;

/// <summary>
/// Main shell page with navigation, search bar, and player.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    public PlayerViewModel PlayerViewModel { get; }

    public MainPage(MainViewModel mainViewModel, PlayerViewModel playerViewModel)
    {
        ViewModel = mainViewModel;
        PlayerViewModel = playerViewModel;
        InitializeComponent();

        ViewModel.PageNavigation += OnPageNavigation;
        NavigateToPage("home");
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
            NavigateToPage(tag);
    }

    private void OnPageNavigation(object? sender, string page)
    {
        NavigateToPage(page);
    }

    private void NavigateToPage(string page)
    {
        switch (page.ToLower())
        {
            case "home":
            case "library":
                ContentFrame.Navigate(typeof(LibraryPage));
                break;
            case "search":
            case "search_results":
                ContentFrame.Navigate(typeof(SearchPage));
                break;
            case "playlists":
                ContentFrame.Navigate(typeof(PlaylistsPage));
                break;
            case "albums":
            case "album_detail":
                ContentFrame.Navigate(typeof(AlbumsPage));
                break;
            case "liked":
                ContentFrame.Navigate(typeof(LikedSongsPage));
                break;
            case "downloads":
                ContentFrame.Navigate(typeof(DownloadsPage));
                break;
            case "history":
                ContentFrame.Navigate(typeof(HistoryPage));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
            case "about":
                ContentFrame.Navigate(typeof(AboutPage));
                break;
        }
    }
}
