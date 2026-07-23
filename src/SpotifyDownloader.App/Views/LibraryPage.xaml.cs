using Microsoft.UI.Xaml.Controls;

namespace SpotifyDownloader.App.Views;

public sealed partial class LibraryPage : Page
{
    public App.ViewModels.MainViewModel ViewModel => App.GetService<ViewModels.MainViewModel>()!;

    public LibraryPage()
    {
        InitializeComponent();
    }
}
