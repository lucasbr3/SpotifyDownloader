using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlayerService _player;
    private readonly ILocalizationService _localization;
    private readonly Random _rand = new();
    private Timer? _spectrumTimer;

    [ObservableProperty]
    private SpotifyTrack? _currentTrack;

    [ObservableProperty]
    private string _trackTitle = string.Empty;

    [ObservableProperty]
    private string _trackArtist = string.Empty;

    [ObservableProperty]
    private string _trackAlbum = string.Empty;

    [ObservableProperty]
    private string _albumCoverUrl = string.Empty;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isShuffled;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private double _durationSeconds;

    [ObservableProperty]
    private string _positionFormatted = "0:00";

    [ObservableProperty]
    private string _durationFormatted = "0:00";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _isMiniPlayerVisible;

    [ObservableProperty]
    private bool _hasTrack;

    [ObservableProperty]
    private bool _isEqualizerOpen;

    [ObservableProperty]
    private bool _isLyricsOpen;

    [ObservableProperty]
    private string _currentLyrics = string.Empty;

    public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768";
    public double RepeatModeOpacity => RepeatMode != RepeatMode.None ? 1.0 : 0.4;

    public ObservableCollection<SpotifyTrack> Queue { get; } = new();
    public ObservableCollection<EqualizerBand> EqualizerBands { get; } = new();
    public ObservableCollection<double> SpectrumBars { get; } = new();

    public PlayerViewModel(IPlayerService player)
    {
        _player = player;
        _localization = App.Localization;

        _player.TrackChanged += OnTrackChanged;
        _player.PositionChanged += OnPositionChanged;
        _player.PlayStateChanged += OnPlayStateChanged;
        _player.VolumeChanged += OnVolumeChanged;
        _player.ShuffleChanged += (_, s) => IsShuffled = s;
        _player.RepeatChanged += (_, r) => RepeatMode = r;

        InitEqualizer();
        InitSpectrum();
    }

    private void InitEqualizer()
    {
        EqualizerBands.Add(new EqualizerBand("32Hz", 32));
        EqualizerBands.Add(new EqualizerBand("64Hz", 64));
        EqualizerBands.Add(new EqualizerBand("125Hz", 125));
        EqualizerBands.Add(new EqualizerBand("250Hz", 250));
        EqualizerBands.Add(new EqualizerBand("500Hz", 500));
        EqualizerBands.Add(new EqualizerBand("1kHz", 1000));
        EqualizerBands.Add(new EqualizerBand("2kHz", 2000));
        EqualizerBands.Add(new EqualizerBand("4kHz", 4000));
        EqualizerBands.Add(new EqualizerBand("8kHz", 8000));
        EqualizerBands.Add(new EqualizerBand("16kHz", 16000));
    }

    private void InitSpectrum()
    {
        for (int i = 0; i < 32; i++)
            SpectrumBars.Add(0);
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_player.IsPlaying)
            _player.Pause();
        else
            _player.Resume();
    }

    [RelayCommand]
    private void Next() => _player.Next();

    [RelayCommand]
    private void Previous() => _player.Previous();

    [RelayCommand]
    private void Stop() => _player.Stop();

    [RelayCommand]
    private void ToggleShuffle() => _player.ToggleShuffle();

    [RelayCommand]
    private void ToggleRepeat() => _player.ToggleRepeat();

    [RelayCommand]
    private void ToggleMiniPlayer()
    {
        IsMiniPlayerVisible = !IsMiniPlayerVisible;
        if (IsMiniPlayerVisible)
        {
            App.MainWindow?.ShowMiniPlayer();
        }
        else
        {
            App.MainWindow?.HideMiniPlayer();
        }
    }

    [RelayCommand]
    private void ToggleEqualizer()
    {
        IsEqualizerOpen = !IsEqualizerOpen;
        if (IsEqualizerOpen)
            IsLyricsOpen = false;
    }

    [RelayCommand]
    private async Task ToggleLyricsAsync()
    {
        IsLyricsOpen = !IsLyricsOpen;
        if (IsLyricsOpen && CurrentTrack != null)
        {
            IsEqualizerOpen = false;
            CurrentLyrics = "Carregando letras...";
            CurrentLyrics = await App.Lyrics.GetLyricsAsync(CurrentTrack.Artist, CurrentTrack.Title);
            if (string.IsNullOrEmpty(CurrentLyrics))
                CurrentLyrics = "Letra não encontrada para esta música.";
        }
    }

    [RelayCommand]
    private void OpenQueue()
    {
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayPauseIcon));
        UpdateSpectrumTimer();
    }

    partial void OnRepeatModeChanged(RepeatMode value)
    {
        OnPropertyChanged(nameof(RepeatModeOpacity));
    }

    partial void OnVolumeChanged(double value)
    {
        _player.SetVolume(value);
    }

    partial void OnPositionSecondsChanged(double value)
    {
        if (Math.Abs(value - _player.Position.TotalSeconds) > 1)
            _player.Seek(TimeSpan.FromSeconds(value));
    }

    private void UpdateSpectrumTimer()
    {
        _spectrumTimer?.Dispose();
        if (IsPlaying)
        {
            _spectrumTimer = new Timer(_ =>
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < SpectrumBars.Count; i++)
                    {
                        var value = _rand.NextDouble() * (1.0 - i / 32.0);
                        SpectrumBars[i] = value;
                    }
                    OnPropertyChanged(nameof(SpectrumBars));
                });
            }, null, 0, 80);
        }
    }

    private void OnTrackChanged(object? sender, SpotifyTrack track)
    {
        CurrentTrack = track;
        TrackTitle = track.Title;
        TrackArtist = track.Artist;
        TrackAlbum = track.Album;
        AlbumCoverUrl = track.BestCoverUrl;
        DurationSeconds = track.DurationMs / 1000.0;
        DurationFormatted = track.DurationFormatted;
        HasTrack = true;
        IsPlaying = true;
    }

    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        PositionSeconds = position.TotalSeconds;
        PositionFormatted = position.ToString(@"m\:ss");
        ProgressPercent = DurationSeconds > 0
            ? position.TotalSeconds / DurationSeconds * 100 : 0;
    }

    private void OnPlayStateChanged(object? sender, bool playing)
    {
        IsPlaying = playing;
    }

    private void OnVolumeChanged(object? sender, double volume)
    {
        Volume = volume;
    }
}
