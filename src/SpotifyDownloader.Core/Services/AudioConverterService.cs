using System.Diagnostics;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// Handles audio file conversion between formats and metadata embedding.
/// </summary>
public class AudioConverterService : IAudioConverterService
{
    private readonly HttpClient _httpClient;

    public AudioConverterService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public string GetExtension(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => "mp3",
        AudioFormat.Flac => "flac",
        AudioFormat.Wav => "wav",
        AudioFormat.M4a => "m4a",
        AudioFormat.Ogg => "ogg",
        _ => "mp3"
    };

    public string GetCodec(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => "libmp3lame",
        AudioFormat.Flac => "flac",
        AudioFormat.Wav => "pcm_s16le",
        AudioFormat.M4a => "aac",
        AudioFormat.Ogg => "libvorbis",
        _ => "libmp3lame"
    };

    public async Task<string?> ConvertAsync(string inputPath, string outputPath,
        AudioFormat targetFormat, AudioQuality quality, CancellationToken ct = default)
    {
        try
        {
            var ffmpeg = await FindFfmpegAsync();
            var bitrate = (int)quality;
            var codec = GetCodec(targetFormat);
            var extension = GetExtension(targetFormat);
            var finalPath = Path.ChangeExtension(outputPath, extension);

            var args = $"-i \"{inputPath}\" -codec:a {codec} -b:a {bitrate}k " +
                       $"-map 0:a -id3v2_version 3 -write_id3v1 1 -y \"{finalPath}\"";

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

            return process.ExitCode == 0 && File.Exists(finalPath) ? finalPath : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Conversion failed for {Input}", inputPath);
            return null;
        }
    }

    public async Task<bool> EmbedMetadataAsync(string filePath, SpotifyTrack track)
    {
        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            tagFile.Tag.Title = track.Title;
            tagFile.Tag.Performers = string.IsNullOrEmpty(track.Artist)
                ? Array.Empty<string>()
                : new[] { track.Artist };
            tagFile.Tag.Album = track.Album;
            tagFile.Tag.Track = (uint)Math.Max(0, track.TrackNumber);
            tagFile.Tag.Disc = (uint)Math.Max(0, track.DiscNumber);
            tagFile.Tag.Year = (uint)Math.Max(0, track.ReleaseYear);
            tagFile.Tag.Genres = track.Genres?.ToArray() ?? Array.Empty<string>();
            tagFile.Tag.Comment = $"Downloaded by Spotify Downloader | {track.SpotifyUri}";

            if (!string.IsNullOrEmpty(track.AlbumCoverUrl))
            {
                try
                {
                    var coverBytes = await _httpClient.GetByteArrayAsync(track.AlbumCoverUrl);
                    var picture = new TagLib.Picture(new TagLib.ByteVector(coverBytes))
                    {
                        Type = TagLib.PictureType.FrontCover,
                        MimeType = "image/jpeg"
                    };
                    tagFile.Tag.Pictures = new TagLib.IPicture[] { picture };
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to embed cover art for {File}", filePath);
                }
            }

            tagFile.Save();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to embed metadata for {File}", filePath);
            return false;
        }
    }

    public async Task<bool> ValidateAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            var ffmpeg = await FindFfmpegAsync();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = $"-v error -i \"{filePath}\" -f null -",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return string.IsNullOrEmpty(error);
        }
        catch
        {
            return false;
        }
    }

    public long EstimateSize(int durationMs, AudioQuality quality, AudioFormat format)
    {
        var durationSec = durationMs / 1000.0;
        var bitrate = (int)quality;

        double overhead = format switch
        {
            AudioFormat.Mp3 => 1.05,
            AudioFormat.Flac => 1.5,
            AudioFormat.Wav => 3.0,
            AudioFormat.M4a => 1.05,
            AudioFormat.Ogg => 1.1,
            _ => 1.1
        };

        return (long)(durationSec * bitrate * 1000 / 8 * overhead);
    }

    private static async Task<string> FindFfmpegAsync()
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
}
