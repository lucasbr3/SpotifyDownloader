using Microsoft.UI.Xaml.Controls;
using SpotifyDownloader.App.ViewModels;

namespace SpotifyDownloader.App.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; } = new();

    public DownloadsPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        _ = ViewModel.LoadAsync();
    }
}
