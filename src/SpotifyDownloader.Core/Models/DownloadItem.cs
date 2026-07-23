using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Status of a download item in the queue.
/// </summary>
public enum DownloadStatus
{
    Pending,
    Downloading,
    Converting,
    Paused,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Audio quality presets in kbps.
/// </summary>
public enum AudioQuality
{
    Standard128 = 128,
    High192 = 192,
    High256 = 256,
    VeryHigh320 = 320
}

/// <summary>
/// Supported audio output formats.
/// </summary>
public enum AudioFormat
{
    Mp3,
    Flac,
    Wav,
    M4a,
    Ogg
}

/// <summary>
/// Represents a single download item in the queue.
/// </summary>
public class DownloadItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("track")]
    public SpotifyTrack Track { get; set; } = new();

    [JsonProperty("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    [JsonProperty("progress")]
    public double Progress { get; set; }

    [JsonProperty("download_speed")]
    public double DownloadSpeed { get; set; }

    [JsonProperty("remaining_time")]
    public TimeSpan? RemainingTime { get; set; }

    [JsonProperty("output_path")]
    public string OutputPath { get; set; } = string.Empty;

    [JsonProperty("output_format")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AudioFormat OutputFormat { get; set; } = AudioFormat.Mp3;

    [JsonProperty("error_message")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonProperty("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonProperty("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("quality")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AudioQuality Quality { get; set; } = AudioQuality.VeryHigh320;

    [JsonProperty("retry_count")]
    public int RetryCount { get; set; }

    [JsonProperty("is_playlist_item")]
    public bool IsPlaylistItem { get; set; }

    [JsonProperty("playlist_name")]
    public string? PlaylistName { get; set; }

    [JsonProperty("album_name")]
    public string? AlbumName { get; set; }

    [JsonIgnore]
    public string StatusText => Status switch
    {
        DownloadStatus.Pending => "Aguardando",
        DownloadStatus.Downloading => "Baixando",
        DownloadStatus.Converting => "Convertendo",
        DownloadStatus.Paused => "Pausado",
        DownloadStatus.Completed => "Concluído",
        DownloadStatus.Failed => "Erro",
        DownloadStatus.Cancelled => "Cancelado",
        _ => "Desconhecido"
    };

    [JsonIgnore]
    public string ProgressPercent => $"{Progress:F1}%";

    [JsonIgnore]
    public string FileSizeFormatted => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1_048_576 => $"{FileSize / 1024.0:F1} KB",
        < 1_073_741_824 => $"{FileSize / 1_048_576.0:F1} MB",
        _ => $"{FileSize / 1_073_741_824.0:F2} GB"
    };

    [JsonIgnore]
    public string SpeedFormatted => DownloadSpeed switch
    {
        < 1024 => $"{DownloadSpeed:F0} B/s",
        < 1_048_576 => $"{DownloadSpeed / 1024.0:F1} KB/s",
        _ => $"{DownloadSpeed / 1_048_576.0:F1} MB/s"
    };

    [JsonIgnore]
    public string RemainingTimeFormatted =>
        RemainingTime.HasValue
            ? RemainingTime.Value.TotalHours >= 1
                ? RemainingTime.Value.ToString(@"h\:mm\:ss")
                : RemainingTime.Value.ToString(@"mm\:ss")
            : "--:--";

    [JsonIgnore]
    public bool ShowSpeed => Status == DownloadStatus.Downloading || Status == DownloadStatus.Converting;

    [JsonIgnore]
    public bool IsDownloading => Status == DownloadStatus.Downloading || Status == DownloadStatus.Converting;

    [JsonIgnore]
    public bool IsPaused => Status == DownloadStatus.Paused;

    [JsonIgnore]
    public bool ShowCancel => Status == DownloadStatus.Downloading || Status == DownloadStatus.Converting || Status == DownloadStatus.Paused || Status == DownloadStatus.Pending;

    [JsonIgnore]
    public bool ShowRetry => Status == DownloadStatus.Failed;
}
