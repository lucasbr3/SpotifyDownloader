using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.Services;

/// <summary>
/// Manages in-app notifications and Windows toast notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly Queue<NotificationEntry> _queue = new();

    public void Show(string title, string message, NotificationType type = NotificationType.Info)
    {
        _queue.Enqueue(new NotificationEntry(title, message, type));
        Log.Information("Notification: [{Type}] {Title} - {Message}", type, title, message);
    }

    public void ShowDownloadComplete(string trackName)
    {
        Show("Download Concluído", $"{trackName} foi baixado com sucesso!", NotificationType.Success);
    }

    public void ShowDownloadError(string trackName, string error)
    {
        Show("Falha no Download", $"{trackName}: {error}", NotificationType.Error);
    }

    public void ShowError(string message)
    {
        Show("Erro", message, NotificationType.Error);
    }

    public void ShowSuccess(string message)
    {
        Show("Sucesso", message, NotificationType.Success);
    }

    public void Clear()
    {
        _queue.Clear();
    }

    /// <summary>
    /// Dequeues the next notification if available.
    /// </summary>
    public NotificationEntry? Dequeue()
    {
        return _queue.TryDequeue(out var entry) ? entry : null;
    }

    public record NotificationEntry(string Title, string Message, NotificationType Type);
}
