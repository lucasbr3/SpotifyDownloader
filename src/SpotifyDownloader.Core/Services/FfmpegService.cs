using System.Diagnostics;
using Serilog;

namespace SpotifyDownloader.Core.Services;

public static class FfmpegService
{
    private static string? _cachedPath;

    public static async Task<string> GetFfmpegPathAsync()
    {
        if (_cachedPath != null && File.Exists(_cachedPath))
            return _cachedPath;

        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(localPath))
        {
            _cachedPath = localPath;
            return localPath;
        }

        if (await IsFfmpegInPathAsync())
        {
            _cachedPath = "ffmpeg";
            return "ffmpeg";
        }

        await DownloadFfmpegAsync(localPath);
        _cachedPath = localPath;
        return localPath;
    }

    public static async Task<bool> IsAvailableAsync()
    {
        try
        {
            var path = await GetFfmpegPathAsync();
            return !string.IsNullOrEmpty(path);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsFfmpegInPathAsync()
    {
        try
        {
            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            proc.Start();
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static async Task DownloadFfmpegAsync(string destinationPath)
    {
        try
        {
            Log.Information("Downloading FFmpeg...");

            var url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
            var zipPath = Path.Combine(Path.GetTempPath(), $"ffmpeg_{Guid.NewGuid():N}.zip");

            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fs);
            }

            var extractDir = Path.Combine(Path.GetTempPath(), $"ffmpeg_{Guid.NewGuid():N}");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

            var ffmpegExe = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (ffmpegExe != null)
            {
                File.Copy(ffmpegExe, destinationPath, overwrite: true);
                Log.Information("FFmpeg downloaded to {Path}", destinationPath);
            }
            else
            {
                throw new FileNotFoundException("ffmpeg.exe not found in extracted archive");
            }

            try { File.Delete(zipPath); } catch { }
            try { Directory.Delete(extractDir, recursive: true); } catch { }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download FFmpeg");
            throw;
        }
    }
}
