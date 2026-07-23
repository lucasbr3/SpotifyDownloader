using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SpotifyDownloader.App.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Versão {App.Updater.CurrentVersion}";
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        UpdateBtn.IsEnabled = false;
        UpdateStatus.Text = "Verificando...";
        UpdateStatus.Visibility = Visibility.Visible;

        var update = await App.Updater.CheckForUpdatesAsync();
        if (update == null)
        {
            UpdateStatus.Text = "Você já está na versão mais recente.";
            UpdateBtn.IsEnabled = true;
            return;
        }

        var dialog = new ContentDialog
        {
            Title = $"Nova versão disponível: {update.Version}",
            Content = $"O que há de novo:\n{update.Changelog}",
            PrimaryButtonText = "Atualizar",
            CloseButtonText = "Agora não",
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            UpdateStatus.Text = "Baixando atualização...";
            var success = await App.Updater.DownloadUpdateAsync(update.DownloadUrl,
                new Progress<double>(p => UpdateStatus.Text = $"Baixando... {p:F0}%"));

            if (success)
            {
                UpdateStatus.Text = "Instalando...";
                await App.Updater.InstallUpdateAsync(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SpotifyDownloader", "updates", "update.msix"));
            }
            else
            {
                UpdateStatus.Text = "Falha ao baixar atualização.";
            }
        }

        UpdateBtn.IsEnabled = true;
    }
}
