using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Graphics;

namespace SpotifyDownloader.App;

public sealed partial class MainWindow : Window
{
    private MiniPlayerWindow? _miniPlayer;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Spotify Downloader";
        ExtendsContentIntoTitleBar = true;

        var displayArea = DisplayArea.GetFromWindowId(
            AppWindow.Id, DisplayAreaFallback.Nearest);

        var width = (int)(displayArea.WorkArea.Width * 0.65);
        var height = (int)(displayArea.WorkArea.Height * 0.8);
        width = Math.Max(1000, Math.Min(width, displayArea.WorkArea.Width));
        height = Math.Max(700, Math.Min(height, displayArea.WorkArea.Height));

        AppWindow.Resize(new SizeInt32(width, height));

        var centerX = (displayArea.WorkArea.Width - width) / 2;
        var centerY = (displayArea.WorkArea.Height - height) / 2;
        AppWindow.Move(new PointInt32(centerX, centerY));

        AppWindow.MinSize = new SizeInt32(900, 600);

        _ = InitializeAsync();
    }

    public void ShowMiniPlayer()
    {
        if (_miniPlayer == null)
        {
            _miniPlayer = new MiniPlayerWindow();
            _miniPlayer.Closed += (_, _) => _miniPlayer = null;
        }
        _miniPlayer.Activate();
    }

    public void HideMiniPlayer()
    {
        _miniPlayer?.Close();
        _miniPlayer = null;
        Activate();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var settings = await App.Settings.LoadAsync();
            App.Theme?.ApplyAll(settings.Theme);
            App.Localization.SetLanguage(settings.Language.Language);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize window settings");
        }
    }
}
