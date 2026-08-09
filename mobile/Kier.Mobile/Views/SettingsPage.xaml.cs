using Kier.Mobile.Services;
using Microsoft.Maui.Storage;

namespace Kier.Mobile.Views;

public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;
    private const string ServerUrlKey = "server-url";

    public SettingsPage()
    {
        InitializeComponent();
        _apiService = ApiService.Shared;
        ServerUrlEntry.Text = Preferences.Get(ServerUrlKey, string.Empty);
    }

    private void OnSaveUrlClicked(object sender, EventArgs e)
    {
        var url = ServerUrlEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            SettingsStatusLabel.Text = "Enter a backend URL.";
            return;
        }

        _apiService.SetBaseUrl(url);
        Preferences.Set(ServerUrlKey, url);
        SettingsStatusLabel.Text = "Backend URL saved.";
    }
}
