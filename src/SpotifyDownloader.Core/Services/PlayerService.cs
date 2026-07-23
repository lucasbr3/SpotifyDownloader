using System.Collections.ObjectModel;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// In-app audio player service using Windows Media Player infrastructure.
/// </summary>
public class PlayerService : IPlayerService
{
    private List<SpotifyTrack> _queue = new();
    private int _currentIndex = -1;
    private bool _isPlaying;
    private bool _isShuffled;
    private RepeatMode _repeatMode = RepeatMode.None;
    private double _volume = 0.8;
    private SpotifyTrack? _currentTrack;
    private TimeSpan _position;
    private TimeSpan _duration;
    private Timer? _positionTimer;
    public PlayerService()
    {
        _positionTimer = new Timer(_ =>
        {
            if (_isPlaying)
            {
                _position = _position.Add(TimeSpan.FromSeconds(1));
                PositionChanged?.Invoke(this, _position);
            }
        }, null, 1000, 1000);
    }

    public List<SpotifyTrack> Queue
    {
        get => _queue;
        set
        {
            _queue = value;
            _currentIndex = 0;
        }
    }

    public SpotifyTrack? CurrentTrack => _currentTrack;
    public TimeSpan Position => _position;
    public TimeSpan Duration => _duration;
    public double Volume => _volume;
    public bool IsPlaying => _isPlaying;
    public bool IsShuffled => _isShuffled;
    public RepeatMode RepeatMode => _repeatMode;
    public int CurrentIndex
    {
        get => _currentIndex;
        set => _currentIndex = Math.Clamp(value, -1, _queue.Count - 1);
    }

    public event EventHandler<SpotifyTrack>? TrackChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<bool>? PlayStateChanged;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<bool>? ShuffleChanged;
    public event EventHandler<RepeatMode>? RepeatChanged;
    public event EventHandler? QueueEnded;

    public Task PlayAsync(SpotifyTrack track)
    {
        _currentTrack = track;
        _position = TimeSpan.Zero;
        _duration = TimeSpan.FromMilliseconds(track.DurationMs);
        _isPlaying = true;

        TrackChanged?.Invoke(this, track);
        PlayStateChanged?.Invoke(this, true);
        Log.Information("Playing: {Track} by {Artist}", track.Title, track.Artist);
        return Task.CompletedTask;
    }

    public Task PlayQueueAsync(List<SpotifyTrack> tracks, int startIndex = 0)
    {
        _queue = tracks;
        _currentIndex = startIndex;

        if (_queue.Count > 0 && _currentIndex >= 0 && _currentIndex < _queue.Count)
            return PlayAsync(_queue[_currentIndex]);

        return Task.CompletedTask;
    }

    public void Pause()
    {
        _isPlaying = false;
        PlayStateChanged?.Invoke(this, false);
    }

    public void Resume()
    {
        _isPlaying = true;
        PlayStateChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        _isPlaying = false;
        _position = TimeSpan.Zero;
        PlayStateChanged?.Invoke(this, false);
        PositionChanged?.Invoke(this, _position);
    }

    public void Next()
    {
        if (_queue.Count == 0) return;

        if (_repeatMode == RepeatMode.One)
        {
            PlayAsync(_currentTrack!).ConfigureAwait(false);
            return;
        }

        _currentIndex++;

        if (_currentIndex >= _queue.Count)
        {
            if (_repeatMode == RepeatMode.All)
            {
                _currentIndex = 0;
            }
            else
            {
                _isPlaying = false;
                QueueEnded?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        PlayAsync(_queue[_currentIndex]).ConfigureAwait(false);
    }

    public void Previous()
    {
        if (_queue.Count == 0) return;

        if (_position.TotalSeconds > 3)
        {
            _position = TimeSpan.Zero;
            PositionChanged?.Invoke(this, _position);
            return;
        }

        _currentIndex--;

        if (_currentIndex < 0)
        {
            _currentIndex = _repeatMode == RepeatMode.All ? _queue.Count - 1 : 0;
        }

        PlayAsync(_queue[_currentIndex]).ConfigureAwait(false);
    }

    public void Seek(TimeSpan position)
    {
        _position = TimeSpan.FromSeconds(
            Math.Clamp(position.TotalSeconds, 0, _duration.TotalSeconds));
        PositionChanged?.Invoke(this, _position);
    }

    public void SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0, 1);
        VolumeChanged?.Invoke(this, _volume);
    }

    public void ToggleShuffle()
    {
        _isShuffled = !_isShuffled;
        ShuffleChanged?.Invoke(this, _isShuffled);
    }

    public void ToggleRepeat()
    {
        _repeatMode = _repeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.None,
            _ => RepeatMode.None
        };
        RepeatChanged?.Invoke(this, _repeatMode);
    }
}
