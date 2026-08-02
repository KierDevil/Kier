namespace Kier.Mobile;

public partial class MainPage : ContentPage
{
    private const string SavedServerUrlKey = "server-url";

    public MainPage()
    {
        InitializeComponent();

        ServerUrlEntry.Text = Preferences.Get(SavedServerUrlKey, "http://192.168.10.205:5173");
        OpenCurrentServer();
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        OpenCurrentServer();
    }

    private void OpenCurrentServer()
    {
        var serverUrl = (ServerUrlEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return;
        }

        if (!serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            serverUrl = $"http://{serverUrl}";
        }

        Preferences.Set(SavedServerUrlKey, serverUrl);
        ServerUrlEntry.Text = serverUrl;
        AppWebView.Source = serverUrl;
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        IsBusy = true;
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        IsBusy = false;
    }
}
