using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Linq;
using ZXing.Net.Maui;

namespace Kier.Mobile.Views;

public partial class QRScanPage : ContentPage
{
    public event EventHandler<string>? ScanCompleted;

    public QRScanPage()
    {
        InitializeComponent();
    }

    private void OnScanButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        ScanResultLabel.Text = "Starting camera...";
        CameraBarcodeView.IsDetecting = true;
        ScanButton.IsEnabled = false;
    }

    private async void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e?.Results?.FirstOrDefault();
        if (result != null && !string.IsNullOrWhiteSpace(result.Value))
        {
            var text = result.Value;
            ScanResultLabel.Text = $"Scanned: {text}";
            CameraBarcodeView.IsDetecting = false;
            ScanCompleted?.Invoke(this, text.Trim());
            await Navigation.PopModalAsync();
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        CameraBarcodeView.IsDetecting = false;
        await Navigation.PopModalAsync();
    }
}
