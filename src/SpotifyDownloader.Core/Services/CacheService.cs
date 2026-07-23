using System.Collections.Concurrent;
using Newtonsoft.Json;
using Serilog;
using SpotifyDownloader.Core.Interfaces;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// Provides in-memory caching with optional disk persistence for API responses.
/// </summary>
public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _memory = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _cacheDir;
    private readonly int _defaultCacheHours;
    private bool _isEnabled;

    public CacheService()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotifyDownloader", "cache");
        Directory.CreateDirectory(_cacheDir);
        _defaultCacheHours = 24;
        _isEnabled = true;
    }

    public bool IsEnabled => _isEnabled;

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_isEnabled) return null;

        if (_memory.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
                return entry.Data as T;

            _memory.TryRemove(key, out _);
        }

        var diskEntry = await ReadFromDiskAsync(key);
        if (diskEntry != null && diskEntry.ExpiresAt > DateTime.UtcNow)
        {
            _memory[key] = diskEntry;
            return diskEntry.Data as T;
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class
    {
        if (!_isEnabled) return;

        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(_defaultCacheHours));
        var entry = new CacheEntry
        {
            Key = key,
            Data = data,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _memory[key] = entry;
        await WriteToDiskAsync(key, entry);
    }

    public Task RemoveAsync(string key)
    {
        _memory.TryRemove(key, out _);
        var filePath = GetDiskPath(key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public async Task ClearExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = _memory.Where(kvp => kvp.Value.ExpiresAt <= now).ToList();

        foreach (var kvp in expired)
            _memory.TryRemove(kvp.Key, out _);

        await _lock.WaitAsync();
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.GetFiles(_cacheDir, "*.cache"))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var entry = JsonConvert.DeserializeObject<CacheEntry>(json);
                        if (entry != null && entry.ExpiresAt <= now)
                            File.Delete(file);
                    }
                    catch { }
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        _memory.Clear();

        await _lock.WaitAsync();
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (var file in Directory.GetFiles(_cacheDir, "*.cache"))
                    File.Delete(file);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> GetCacheSizeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!Directory.Exists(_cacheDir)) return 0;
            return Directory.GetFiles(_cacheDir, "*.cache")
                .Sum(f => new FileInfo(f).Length);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<CacheEntry?> ReadFromDiskAsync(string key)
    {
        var filePath = GetDiskPath(key);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var entry = JsonConvert.DeserializeObject<CacheEntry>(json);

            if (entry != null && entry.ExpiresAt > DateTime.UtcNow)
            {
                if (entry.Size > 0)
                    entry.Data = JsonConvert.DeserializeObject(File.ReadAllText(filePath.Replace(".cache", ".data")),
                        typeof(object));
                return entry;
            }

            File.Delete(filePath);
        }
        catch
        {
            try { File.Delete(filePath); } catch { }
        }

        return null;
    }

    private async Task WriteToDiskAsync(string key, CacheEntry entry)
    {
        var filePath = GetDiskPath(key);

        try
        {
            var json = JsonConvert.SerializeObject(new
            {
                entry.Key,
                entry.ExpiresAt,
                entry.CreatedAt,
                Size = 0
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to write cache entry {Key}", key);
        }
    }

    private string GetDiskPath(string key)
    {
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(key)));
        return Path.Combine(_cacheDir, $"{hash}.cache");
    }

    private class CacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public object? Data { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
    }
}
