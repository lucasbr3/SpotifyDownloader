using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

public class UpdateService : IUpdateService
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/anomalyco/SpotifyDownloader/releases/latest";
    private readonly HttpClient _http;

    public UpdateService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SpotifyDownloader/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public string CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(ReleaseApiUrl);
            var release = JsonConvert.DeserializeAnonymousType(json, new
            {
                tag_name = "",
                body = "",
                published_at = "",
                prerelease = false,
                assets = Array.Empty<dynamic>()
            });

            if (release == null) return null;

            var latestVersion = release.tag_name.TrimStart('v');
            if (CompareVersions(latestVersion, CurrentVersion) <= 0)
                return null;

            var downloadUrl = string.Empty;
            var fileSize = 0L;

            foreach (var asset in release.assets)
            {
                string? name = asset.name;
                if (name != null && name.EndsWith(".msix"))
                {
                    downloadUrl = asset.browser_download_url;
                    fileSize = (long)(asset.size ?? 0);
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
                return null;

            return new UpdateInfo
            {
                Version = latestVersion,
                DownloadUrl = downloadUrl,
                Changelog = release.body ?? "",
                ReleaseDate = DateTime.TryParse(release.published_at, out var dt) ? dt : DateTime.UtcNow,
                IsPreRelease = release.prerelease,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for updates");
            return null;
        }
    }

    public async Task<bool> DownloadUpdateAsync(string downloadUrl, IProgress<double>? progress = null)
    {
        try
        {
            var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var updateDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SpotifyDownloader", "updates");
            Directory.CreateDirectory(updateDir);
            var updatePath = Path.Combine(updateDir, "update.msix");

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(updatePath);
            var buffer = new byte[81920];
            long readBytes = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                readBytes += bytesRead;
                progress?.Report(totalBytes > 0 ? (double)readBytes / totalBytes * 100 : 0);
            }

            Log.Information("Update downloaded to {Path}", updatePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download update");
            return false;
        }
    }

    public async Task<bool> InstallUpdateAsync(string updatePath)
    {
        try
        {
            if (!File.Exists(updatePath))
            {
                Log.Error("Update file not found: {Path}", updatePath);
                return false;
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Add-AppxPackage -Path '{updatePath}'\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            System.Diagnostics.Process.Start(psi);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to install update");
            return false;
        }
    }

    private static int CompareVersions(string a, string b)
    {
        var re = new Regex(@"\d+");
        var partsA = re.Matches(a).Select(m => int.Parse(m.Value)).ToArray();
        var partsB = re.Matches(b).Select(m => int.Parse(m.Value)).ToArray();

        for (int i = 0; i < Math.Max(partsA.Length, partsB.Length); i++)
        {
            var va = i < partsA.Length ? partsA[i] : 0;
            var vb = i < partsB.Length ? partsB[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }
}
