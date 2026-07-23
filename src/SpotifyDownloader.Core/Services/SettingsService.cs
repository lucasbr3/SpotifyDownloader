using Newtonsoft.Json;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.Core.Services;

/// <summary>
/// Manages application settings persistence in JSON format.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AppSettings? _cached;

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotifyDownloader");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public async Task<AppSettings> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_cached != null)
                return _cached;

            if (!File.Exists(_settingsPath))
            {
                _cached = AppSettings.Default;
                await SaveInternalAsync(_cached);
                return _cached;
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            _cached = JsonConvert.DeserializeObject<AppSettings>(json)
                      ?? AppSettings.Default;
            return _cached;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load settings, using defaults");
            _cached = AppSettings.Default;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await _lock.WaitAsync();
        try
        {
            _cached = settings;
            await SaveInternalAsync(settings);
            SettingsChanged?.Invoke(this, settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetAsync()
    {
        var defaults = AppSettings.Default;
        await SaveAsync(defaults);
    }

    private async Task SaveInternalAsync(AppSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public event EventHandler<AppSettings>? SettingsChanged;
}
