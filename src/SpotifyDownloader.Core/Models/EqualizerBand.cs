using CommunityToolkit.Mvvm.ComponentModel;

namespace SpotifyDownloader.Core.Models;

public partial class EqualizerBand : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _gain;

    public EqualizerBand(string label, double frequency, double gain = 0)
    {
        _label = label;
        _frequency = frequency;
        _gain = gain;
    }
}
