using Microsoft.UI.Xaml.Controls;
using SpotifyDownloader.App.ViewModels;

namespace SpotifyDownloader.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
    }
}
