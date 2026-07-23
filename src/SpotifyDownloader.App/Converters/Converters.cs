using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.Converters;

/// <summary>
/// Converts a boolean to Visibility. Optional parameter "True" inverts the logic.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is bool b && b;
        var invert = parameter is string s && s.Equals("True", StringComparison.OrdinalIgnoreCase);
        return invert ? (boolValue ? Visibility.Collapsed : Visibility.Visible) : (boolValue ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : false;
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : false;
}

/// <summary>
/// Converts a boolean to a double opacity (1.0 for true, 0.4 for false).
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? 1.0 : 0.4;
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DownloadStatus to a color string for UI display.
/// </summary>
public class DownloadStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Completed => "#1DB954",
                DownloadStatus.Failed => "#E74C3C",
                DownloadStatus.Downloading => "#3498DB",
                DownloadStatus.Converting => "#F39C12",
                DownloadStatus.Paused => "#95A5A6",
                DownloadStatus.Cancelled => "#666666",
                _ => "#95A5A6"
            };
        }
        return "#95A5A6";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts AudioQuality to a display string.
/// </summary>
public class QualityToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AudioQuality quality)
        {
            return quality switch
            {
                AudioQuality.Standard128 => "128 kbps",
                AudioQuality.High192 => "192 kbps",
                AudioQuality.High256 => "256 kbps",
                AudioQuality.VeryHigh320 => "320 kbps",
                _ => "Desconhecido"
            };
        }
        return "Desconhecido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts AudioFormat to a display string.
/// </summary>
public class FormatToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AudioFormat format)
        {
            return format switch
            {
                AudioFormat.Mp3 => "MP3",
                AudioFormat.Flac => "FLAC",
                AudioFormat.Wav => "WAV",
                AudioFormat.M4a => "M4A",
                AudioFormat.Ogg => "OGG",
                _ => "Desconhecido"
            };
        }
        return "Desconhecido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts an integer count to Visibility (Visible if zero).
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is double d) return d == 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a DateTime? to a display string.
/// </summary>
public class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt) return dt.ToString("dd/MM/yyyy HH:mm");
        if (value is DateTime? ndt && ndt.HasValue) return ndt.Value.ToString("dd/MM/yyyy HH:mm");
        return "--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
