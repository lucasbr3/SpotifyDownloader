using Microsoft.UI.Xaml.Controls;

namespace SpotifyDownloader.App.Views;

public sealed partial class HistoryPage : Page
{
    public App.ViewModels.DownloadsViewModel ViewModel => App.GetService<ViewModels.DownloadsViewModel>()!;

    public HistoryPage()
    {
        InitializeComponent();
    }

    private async void OnHistorySearchChanged(object sender, TextChangedEventArgs e)
    {
        await ViewModel.SearchHistoryAsync();
    }
}
