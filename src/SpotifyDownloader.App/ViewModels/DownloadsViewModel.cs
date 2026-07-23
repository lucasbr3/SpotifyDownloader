using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SpotifyDownloader.Core.Helpers;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.ViewModels;

/// <summary>
/// ViewModel for managing the download queue and download history.
/// </summary>
public partial class DownloadsViewModel : ObservableObject
{
    private readonly IDownloadService _download;
    private readonly ILocalizationService _localization;
    private readonly INotificationService _notifications;

    [ObservableProperty]
    private bool _hasActiveDownloads;

    [ObservableProperty]
    private bool _hasHistory;

    [ObservableProperty]
    private int _activeCount;

    [ObservableProperty]
    private int _historyTotal;

    [ObservableProperty]
    private int _historySuccessful;

    [ObservableProperty]
    private int _historyFailed;

    [ObservableProperty]
    private string _historyTotalSize = "0 B";

    [ObservableProperty]
    private string _historySearchQuery = string.Empty;

    [ObservableProperty]
    private DownloadItem? _selectedDownload;

    public ObservableCollection<DownloadItem> ActiveDownloads { get; } = new();
    public ObservableCollection<DownloadItem> HistoryItems { get; } = new();

    public DownloadsViewModel()
    {
        _download = App.Download;
        _localization = App.Localization;
        _notifications = App.Notifications;

        _download.DownloadStarted += OnDownloadStarted;
        _download.DownloadProgressChanged += OnDownloadProgressChanged;
        _download.DownloadCompleted += OnDownloadCompleted;
        _download.DownloadFailed += OnDownloadFailed;
        _download.DownloadPaused += OnDownloadPaused;
        _download.DownloadResumed += OnDownloadResumed;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var active = _download.GetActiveDownloads();
        ActiveDownloads.Clear();
        foreach (var item in active)
            ActiveDownloads.Add(item);

        var history = await _download.GetHistoryAsync();
        HistoryItems.Clear();
        foreach (var item in history.Items.OrderByDescending(h => h.CompletedAt))
            HistoryItems.Add(item);

        HistoryTotal = history.TotalDownloads;
        HistorySuccessful = history.SuccessfulDownloads;
        HistoryFailed = history.FailedDownloads;
        HistoryTotalSize = history.TotalSizeFormatted;

        UpdateState();
    }

    [RelayCommand]
    private Task PauseAsync(DownloadItem? item)
    {
        if (item == null) return Task.CompletedTask;
        return _download.PauseDownloadAsync(item.Id);
    }

    [RelayCommand]
    private Task ResumeAsync(DownloadItem? item)
    {
        if (item == null) return Task.CompletedTask;
        return _download.ResumeDownloadAsync(item.Id);
    }

    [RelayCommand]
    private Task CancelAsync(DownloadItem? item)
    {
        if (item == null) return Task.CompletedTask;
        return _download.CancelDownloadAsync(item.Id);
    }

    [RelayCommand]
    private async Task RetryAsync(DownloadItem? item)
    {
        if (item == null) return;
        var newItem = await _download.RetryDownloadAsync(item);
        if (newItem != null)
        {
            var index = ActiveDownloads.IndexOf(item);
            if (index >= 0)
                ActiveDownloads[index] = newItem;
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        await _download.ClearCompletedAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await _download.ClearAllAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SearchHistoryAsync()
    {
        if (string.IsNullOrWhiteSpace(HistorySearchQuery))
        {
            await LoadAsync();
            return;
        }

        var results = await _download.SearchHistoryAsync(HistorySearchQuery);
        HistoryItems.Clear();
        foreach (var item in results)
            HistoryItems.Add(item);

        HistoryTotal = results.Count;
        HistorySuccessful = results.Count(i => i.Status == DownloadStatus.Completed);
        HistoryFailed = results.Count(i => i.Status == DownloadStatus.Failed);
        HistoryTotalSize = FormatHelper.FormatFileSize(results.Sum(i => i.FileSize));
    }

    private void OnDownloadStarted(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            if (!ActiveDownloads.Any(d => d.Id == item.Id))
                ActiveDownloads.Insert(0, item);
            UpdateState();
        });
    }

    private void OnDownloadProgressChanged(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            var existing = ActiveDownloads.FirstOrDefault(d => d.Id == item.Id);
            if (existing != null)
            {
                var index = ActiveDownloads.IndexOf(existing);
                if (index >= 0)
                {
                    ActiveDownloads[index] = item;
                    OnPropertyChanged(nameof(item.ProgressPercent));
                    OnPropertyChanged(nameof(item.SpeedFormatted));
                    OnPropertyChanged(nameof(item.RemainingTimeFormatted));
                }
            }
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            await LoadAsync();
            _notifications.ShowDownloadComplete(item.Track.Title);
        });
    }

    private void OnDownloadFailed(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            await LoadAsync();
            if (!string.IsNullOrEmpty(item.ErrorMessage))
                _notifications.ShowDownloadError(item.Track.Title, item.ErrorMessage);
        });
    }

    private void OnDownloadPaused(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            OnPropertyChanged(nameof(item.StatusText));
        });
    }

    private void OnDownloadResumed(object? sender, DownloadItem item)
    {
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            OnPropertyChanged(nameof(item.StatusText));
        });
    }

    private void UpdateState()
    {
        HasActiveDownloads = ActiveDownloads.Count > 0;
        HasHistory = HistoryItems.Count > 0;
        ActiveCount = ActiveDownloads.Count;
    }
}
