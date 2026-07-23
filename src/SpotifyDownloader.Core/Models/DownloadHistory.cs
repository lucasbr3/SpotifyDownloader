using Newtonsoft.Json;

namespace SpotifyDownloader.Core.Models;

/// <summary>
/// Represents the download history with statistics.
/// </summary>
public class DownloadHistory
{
    [JsonProperty("items")]
    public List<DownloadItem> Items { get; set; } = new();

    [JsonProperty("last_cleaned")]
    public DateTime LastCleaned { get; set; } = DateTime.UtcNow;

    [JsonProperty("total_downloads")]
    public int TotalDownloads { get; set; }

    [JsonProperty("successful_downloads")]
    public int SuccessfulDownloads { get; set; }

    [JsonProperty("failed_downloads")]
    public int FailedDownloads { get; set; }

    [JsonProperty("total_bytes_downloaded")]
    public long TotalBytesDownloaded { get; set; }

    [JsonProperty("total_time_spent_ms")]
    public long TotalTimeSpentMs { get; set; }

    [JsonIgnore]
    public string TotalSizeFormatted => TotalBytesDownloaded switch
    {
        < 1_048_576 => $"{TotalBytesDownloaded / 1024.0:F1} KB",
        < 1_073_741_824 => $"{TotalBytesDownloaded / 1_048_576.0:F1} MB",
        _ => $"{TotalBytesDownloaded / 1_073_741_824.0:F2} GB"
    };
}
