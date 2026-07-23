using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App;

public sealed partial class MiniPlayerWindow : Window
{
    private readonly IPlayerService _player;
    private Timer? _updateTimer;
    private readonly Border[] _spectrumBars;

    public MiniPlayerWindow()
    {
        InitializeComponent();
        _player = App.Player;
        Title = "Mini Player";

        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
            AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
        var centerX = (displayArea.WorkArea.Width - 360) / 2;
        var centerY = displayArea.WorkArea.Height - 200;
        AppWindow.Move(new PointInt32((int)centerX, (int)centerY));
        AppWindow.Resize(new SizeInt32(360, 580));
        AppWindow.MinSize = new SizeInt32(320, 500);

        _player.PlayStateChanged += OnPlayStateChanged;
        Closed += OnClosed;

        _spectrumBars = new Border[32];
        for (int i = 0; i < 32; i++)
        {
            _spectrumBars[i] = new Border();
            SpectrumGrid.Children.Add(_spectrumBars[i]);
        }

        _updateTimer = new Timer(_ =>
        {
            _ = DispatcherQueue.TryEnqueue(UpdateUI);
        }, null, 0, 200);

        UpdateUI();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _player.PlayStateChanged -= OnPlayStateChanged;
        _updateTimer?.Dispose();
        _updateTimer = null;
    }

    private void UpdateUI()
    {
        var track = _player.CurrentTrack;
        if (track != null)
        {
            TrackTitleText.Text = track.Title;
            TrackArtistText.Text = track.Artist;
            CoverImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(track.BestCoverUrl));
            LargeCoverImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(track.BestCoverUrl));
            DurationText.Text = track.DurationFormatted;
        }

        var pos = _player.Position;
        PositionText.Text = pos.ToString(@"m\:ss");
        var totalSec = _player.Duration.TotalSeconds;
        ProgressSlider.Value = totalSec > 0 ? pos.TotalSeconds / totalSec * 100 : 0;

        var playIcon = _player.IsPlaying ? "\uE769" : "\uE768";
        PlayPauseBtn.Content = playIcon;
        PlayPauseLarge.Content = playIcon;

        var rand = new Random();
        for (int i = 0; i < _spectrumBars.Length; i++)
        {
            var height = _player.IsPlaying ? rand.Next(4, 40) * (1.0 - i / 40.0) : 2;
            _spectrumBars[i].Height = height;
        }
    }

    private void OnPlayStateChanged(object? sender, bool playing)
    {
        _ = DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (_player.IsPlaying) _player.Pause(); else _player.Resume();
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e) => _player.Previous();
    private void OnNextClick(object sender, RoutedEventArgs e) => _player.Next();

    private void OnProgressChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var totalSec = _player.Duration.TotalSeconds;
        if (totalSec > 0)
            _player.Seek(TimeSpan.FromSeconds(e.NewValue / 100.0 * totalSec));
    }

    private void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        App.MainWindow?.HideMiniPlayer();
    }
}
