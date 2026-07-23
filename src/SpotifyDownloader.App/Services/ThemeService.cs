using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Serilog;
using SpotifyDownloader.Core.Interfaces;
using SpotifyDownloader.Core.Models;

namespace SpotifyDownloader.App.Services;

/// <summary>
/// Manages runtime theme switching including Mica, Acrylic, and accent colors.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly Window _window;
    private ResourceDictionary? _currentTheme;
    private ResourceDictionary? _currentAccent;
    private bool _micaApplied;
    private bool _acrylicApplied;

    public ThemeService(Window window)
    {
        _window = window;
    }

    public bool IsMicaSupported
    {
        get
        {
            var osVersion = Environment.OSVersion.Version;
            return osVersion.Major >= 10 && osVersion.Build >= 22000;
        }
    }

    public bool IsAcrylicSupported => true;

    public void ApplyTheme(AppTheme theme)
    {
        var merged = Application.Current.Resources.MergedDictionaries;

        var uri = theme switch
        {
            AppTheme.Light => new Uri("ms-appx:///Themes/LightTheme.xaml"),
            AppTheme.Amoled => new Uri("ms-appx:///Themes/AmoledTheme.xaml"),
            AppTheme.Blue => new Uri("ms-appx:///Themes/BlueTheme.xaml"),
            AppTheme.Green => new Uri("ms-appx:///Themes/GreenTheme.xaml"),
            AppTheme.Purple => new Uri("ms-appx:///Themes/PurpleTheme.xaml"),
            AppTheme.Red => new Uri("ms-appx:///Themes/RedTheme.xaml"),
            AppTheme.Orange => new Uri("ms-appx:///Themes/OrangeTheme.xaml"),
            AppTheme.Custom => new Uri("ms-appx:///Themes/CustomTheme.xaml"),
            _ => new Uri("ms-appx:///Themes/DarkTheme.xaml")
        };

        if (_currentTheme != null)
            merged.Remove(_currentTheme);

        _currentTheme = new ResourceDictionary { Source = uri };
        merged.Add(_currentTheme);

        var rootTheme = theme == AppTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
        if (_window.Content is FrameworkElement element)
            element.RequestedTheme = rootTheme;

        Log.Information("Theme applied: {Theme}", theme);
    }

    public void ApplyAccentColor(string colorHex)
    {
        try
        {
            var color = ParseColor(colorHex);
            var merged = Application.Current.Resources.MergedDictionaries;

            if (_currentAccent != null)
                merged.Remove(_currentAccent);

            _currentAccent = new ResourceDictionary
            {
                ["SystemAccentColor"] = color,
                ["SystemAccentColorDark1"] = Darken(color, 0.1),
                ["SystemAccentColorDark2"] = Darken(color, 0.2),
                ["SystemAccentColorLight1"] = Lighten(color, 0.1),
                ["SystemAccentColorLight2"] = Lighten(color, 0.2),
                ["AccentButtonBackground"] = new SolidColorBrush(color)
            };

            merged.Add(_currentAccent);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply accent color {Color}", colorHex);
        }
    }

    public void ApplyTransparency(double opacity)
    {
        if (_window.Content is FrameworkElement element)
            element.Opacity = opacity;
    }

    public void ApplyCornerRadius(double radius)
    {
        Application.Current.Resources["ControlCornerRadius"] = new CornerRadius(radius);
    }

    public void ApplyFontSize(double size)
    {
        Application.Current.Resources["ControlContentThemeFontSize"] = size;
    }

    public void ApplyUiScale(double scale)
    {
        Application.Current.Resources["UIScale"] = scale;
    }

    public void ApplyMica(bool enable)
    {
        if (!IsMicaSupported) return;

        try
        {
            var hwnd = GetWindowHandle();
            var attribute = 38; // DWMWA_MICA_EFFECT
            var value = enable ? 1 : 0;
            DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
            _micaApplied = enable;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply Mica effect");
        }
    }

    public void ApplyAcrylic(bool enable)
    {
        if (!IsAcrylicSupported) return;

        try
        {
            var hwnd = GetWindowHandle();

            if (enable && !_acrylicApplied)
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    GradientColor = 0x99FFFFFF
                };

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = Marshal.SizeOf(accent)
                };

                data.Data = Marshal.AllocHGlobal(data.SizeOfData);
                Marshal.StructureToPtr(accent, data.Data, false);
                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.Data);
                _acrylicApplied = true;
            }
            else if (!enable && _acrylicApplied)
            {
                var accent = new AccentPolicy { AccentState = AccentState.ACCENT_DISABLED };
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = Marshal.SizeOf(accent)
                };
                data.Data = Marshal.AllocHGlobal(data.SizeOfData);
                Marshal.StructureToPtr(accent, data.Data, false);
                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.Data);
                _acrylicApplied = false;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply Acrylic effect");
        }
    }

    public void ApplyAll(ThemeSettings settings)
    {
        ApplyTheme(settings.Theme);
        ApplyAccentColor(settings.AccentColor);
        ApplyTransparency(settings.WindowTransparency);
        ApplyCornerRadius(settings.CornerRadius);
        ApplyFontSize(settings.FontSize);
        ApplyUiScale(settings.UiScale);
        ApplyMica(settings.UseMica);
        ApplyAcrylic(settings.UseAcrylic);
    }

    private IntPtr GetWindowHandle()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        return hwnd;
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        return Windows.UI.Color.FromArgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16),
            Convert.ToByte(hex[6..8], 16));
    }

    private static Windows.UI.Color Darken(Windows.UI.Color color, double factor)
    {
        return Windows.UI.Color.FromArgb(color.A,
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor)));
    }

    private static Windows.UI.Color Lighten(Windows.UI.Color color, double factor)
    {
        return Windows.UI.Color.FromArgb(color.A,
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor));
    }

    // Native methods
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    private enum AccentState { ACCENT_DISABLED = 0, ACCENT_ENABLE_BLURBEHIND = 3, ACCENT_ENABLE_ACRYLICBLURBEHIND = 4 }
    private enum WindowCompositionAttribute { WCA_ACCENT_POLICY = 19 }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }
}
