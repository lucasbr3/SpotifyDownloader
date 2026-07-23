using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// Manages audio downloads from YouTube with queue, progress, and retry logic.
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, DownloadItem> _activeDownloads = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly List<DownloadItem> _history = new();
    private readonly string _historyPath;
    private int _totalDownloads;

    public DownloadService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _concurrencyLimiter = new SemaphoreSlim(3, 3);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotifyDownloader");
        Directory.CreateDirectory(appData);
        _historyPath = Path.Combine(appData, "history.json");
        _ = LoadHistoryAsync();
    }

    public int TotalDownloads => _totalDownloads;
    public int ActiveDownloadCount => _activeDownloads.Count;

    public event EventHandler<DownloadItem>? DownloadStarted;
    public event EventHandler<DownloadItem>? DownloadProgressChanged;
    public event EventHandler<DownloadItem>? DownloadCompleted;
    public event EventHandler<DownloadItem>? DownloadFailed;
    public event EventHandler<DownloadItem>? DownloadPaused;
    public event EventHandler<DownloadItem>? DownloadResumed;

    public async Task<DownloadItem> DownloadTrackAsync(SpotifyTrack track, AudioQuality quality,
        AudioFormat format, string outputPath, IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var item = new DownloadItem
        {
            Track = track,
            Quality = quality,
            OutputFormat = format,
            OutputPath = outputPath,
            Status = DownloadStatus.Pending,
            AddedAt = DateTime.UtcNow
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeDownloads[item.Id] = item;
        _tokens[item.Id] = cts;

        DownloadStarted?.Invoke(this, item);

        try
        {
            await _concurrencyLimiter.WaitAsync(cts.Token);
            try
            {
                item.Status = DownloadStatus.Downloading;
                item.StartedAt = DateTime.UtcNow;

                var audioUrl = await FindAudioSourceAsync(track, cts.Token);
                if (string.IsNullOrEmpty(audioUrl))
                {
                    item.Status = DownloadStatus.Failed;
                    item.ErrorMessage = "Fonte de áudio não encontrada";
                    DownloadFailed?.Invoke(this, item);
                    return item;
                }

                var outputFile = BuildOutputPath(track, outputPath, format);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                item.Status = DownloadStatus.Converting;
                item.Progress = 50;
                progress?.Report(50);

                var success = await DownloadAudioAsync(audioUrl, outputFile, quality, format,
                    item, progress, cts.Token);

                if (!success)
                {
                    item.Status = DownloadStatus.Failed;
                    if (string.IsNullOrEmpty(item.ErrorMessage))
                        item.ErrorMessage = "Falha no download";
                    DownloadFailed?.Invoke(this, item);
                    return item;
                }

                item.Status = DownloadStatus.Completed;
                item.Progress = 100;
                item.CompletedAt = DateTime.UtcNow;
                item.FileSize = File.Exists(outputFile) ? new FileInfo(outputFile).Length : 0;

                progress?.Report(100);
                DownloadCompleted?.Invoke(this, item);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            item.Status = DownloadStatus.Failed;
            item.ErrorMessage = ex.Message;
            Log.Error(ex, "Download failed for {Track}", track.Title);
            DownloadFailed?.Invoke(this, item);
        }
        finally
        {
            _activeDownloads.TryRemove(item.Id, out _);
            _tokens.TryRemove(item.Id, out _);
            _totalDownloads++;
            SaveHistoryAsync().ConfigureAwait(false);
        }

        return item;
    }

    public async Task<List<DownloadItem>> DownloadBatchAsync(List<SpotifyTrack> tracks,
        AudioQuality quality, AudioFormat format, string outputPath,
        IProgress<BatchDownloadProgress>? progress = null, CancellationToken ct = default)
    {
        var results = new List<DownloadItem>();
        var batch = new BatchDownloadProgress { Total = tracks.Count };

        for (int i = 0; i < tracks.Count; i++)
        {
            if (ct.IsCancellationRequested) break;

            batch.CurrentTrack = tracks[i].Title;
            batch.CurrentArtist = tracks[i].Artist;

            var trackProgress = new Progress<double>(p => { });
            var item = await DownloadTrackAsync(tracks[i], quality, format, outputPath,
                trackProgress, ct);
            results.Add(item);

            batch.Completed = i + 1;
            progress?.Report(batch);
        }

        return results;
    }

    public Task PauseDownloadAsync(string downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var item))
        {
            item.Status = DownloadStatus.Paused;
            DownloadPaused?.Invoke(this, item);
        }
        return Task.CompletedTask;
    }

    public Task ResumeDownloadAsync(string downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var item))
        {
            item.Status = DownloadStatus.Downloading;
            DownloadResumed?.Invoke(this, item);
        }
        return Task.CompletedTask;
    }

    public Task CancelDownloadAsync(string downloadId)
    {
        if (_tokens.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
            _tokens.TryRemove(downloadId, out _);
        }

        if (_activeDownloads.TryGetValue(downloadId, out var item))
        {
            item.Status = DownloadStatus.Cancelled;
            _activeDownloads.TryRemove(downloadId, out _);
        }

        return Task.CompletedTask;
    }

    public async Task<DownloadItem> RetryDownloadAsync(DownloadItem item)
    {
        if (item.RetryCount >= 3)
        {
            item.ErrorMessage = "Número máximo de tentativas excedido";
            return item;
        }

        item.RetryCount++;
        item.Status = DownloadStatus.Pending;
        item.ErrorMessage = string.Empty;

        return await DownloadTrackAsync(item.Track, item.Quality, item.OutputFormat,
            item.OutputPath, null);
    }

    public List<DownloadItem> GetActiveDownloads() =>
        _activeDownloads.Values.OrderByDescending(d => d.AddedAt).ToList();

    public async Task<DownloadHistory> GetHistoryAsync()
    {
        await LoadHistoryAsync();
        return new DownloadHistory
        {
            Items = _history.ToList(),
            TotalDownloads = _totalDownloads,
            SuccessfulDownloads = _history.Count(h => h.Status == DownloadStatus.Completed),
            FailedDownloads = _history.Count(h => h.Status == DownloadStatus.Failed),
            TotalBytesDownloaded = _history.Sum(h => h.FileSize)
        };
    }

    public Task ClearCompletedAsync()
    {
        var completed = _activeDownloads.Values
            .Where(d => d.Status == DownloadStatus.Completed).ToList();
        foreach (var item in completed)
            _activeDownloads.TryRemove(item.Id, out _);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync()
    {
        foreach (var kvp in _tokens)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _activeDownloads.Clear();
        _tokens.Clear();
        return Task.CompletedTask;
    }

    public async Task<List<DownloadItem>> SearchHistoryAsync(string query)
    {
        await LoadHistoryAsync();
        var lower = query.ToLower();
        return _history.Where(h =>
            h.Track.Title.ToLower().Contains(lower) ||
            h.Track.Artist.ToLower().Contains(lower) ||
            h.Track.Album.ToLower().Contains(lower))
            .OrderByDescending(h => h.CompletedAt)
            .ToList();
    }

    public async Task<bool> IsFfmpegAvailableAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetFfmpegPathAsync()
    {
        var paths = new[]
        {
            "ffmpeg",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "ffmpeg", "bin", "ffmpeg.exe")
        };

        foreach (var path in paths)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0) return path;
            }
            catch { }
        }

        return "ffmpeg";
    }

    private async Task<string?> FindAudioSourceAsync(SpotifyTrack track, CancellationToken ct)
    {
        try
        {
            var query = Uri.EscapeDataString($"{track.Artist} {track.Title} audio");
            var url = $"https://www.youtube.com/results?search_query={query}";
            var html = await _httpClient.GetStringAsync(url, ct);

            var match = System.Text.RegularExpressions.Regex.Match(html,
                "/watch\\?v=([a-zA-Z0-9_-]{11})");
            if (match.Success)
                return $"https://www.youtube.com/watch?v={match.Groups[1].Value}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to find audio source for {Track}", track.Title);
        }
        return null;
    }

    private async Task<bool> DownloadAudioAsync(string audioUrl, string outputPath,
        AudioQuality quality, AudioFormat format, DownloadItem item,
        IProgress<double>? progress, CancellationToken ct)
    {
        try
        {
            var ffmpeg = await GetFfmpegPathAsync();
            var bitrate = (int)quality;
            var extension = GetExtension(format);
            var finalPath = Path.ChangeExtension(outputPath, extension);

            var codec = format switch
            {
                AudioFormat.Mp3 => "libmp3lame",
                AudioFormat.Flac => "flac",
                AudioFormat.Wav => "pcm_s16le",
                AudioFormat.M4a => "aac",
                AudioFormat.Ogg => "libvorbis",
                _ => "libmp3lame"
            };

            var args = $"-i \"{audioUrl}\" -codec:a {codec} -b:a {bitrate}k " +
                       $"-map 0:a -id3v2_version 3 -write_id3v1 1 -y \"{finalPath}\"";

            item.OutputPath = finalPath;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0) return false;
            if (!File.Exists(finalPath)) return false;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Download failed for {Url}", audioUrl);
            item.ErrorMessage = ex.Message;
            return false;
        }
    }

    private static string BuildOutputPath(SpotifyTrack track, string basePath, AudioFormat format)
    {
        var extension = GetExtension(format);
        var sanitizedTitle = SanitizeFileName(track.Title);
        var sanitizedArtist = SanitizeFileName(track.Artist);
        var sanitizedAlbum = SanitizeFileName(track.Album);

        var path = basePath;

        if (true) // CreateArtistFolders
            path = Path.Combine(path, sanitizedArtist);

        if (true) // CreateAlbumFolders
            path = Path.Combine(path, sanitizedAlbum);

        var filename = $"{track.TrackNumber:D2} - {sanitizedTitle}.{extension}";
        return Path.Combine(path, filename);
    }

    private static string GetExtension(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => "mp3",
        AudioFormat.Flac => "flac",
        AudioFormat.Wav => "wav",
        AudioFormat.M4a => "m4a",
        AudioFormat.Ogg => "ogg",
        _ => "mp3"
    };

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized.Trim();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = await File.ReadAllTextAsync(_historyPath);
                var history = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DownloadItem>>(json);
                if (history != null)
                {
                    _history.Clear();
                    _history.AddRange(history);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load download history");
        }
    }

    private async Task SaveHistoryAsync()
    {
        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_history, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(_historyPath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save download history");
        }
    }
}
