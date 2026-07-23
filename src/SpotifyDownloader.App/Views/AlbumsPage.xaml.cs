using Microsoft.UI.Xaml.Controls;
using SpotifyDownloader.App.ViewModels;

namespace SpotifyDownloader.App.Views;

public sealed partial class AlbumsPage : Page
{
    public MainViewModel ViewModel => App.GetService<ViewModels.MainViewModel>()!;

    public AlbumsPage()
    {
        InitializeComponent();
    }
}
