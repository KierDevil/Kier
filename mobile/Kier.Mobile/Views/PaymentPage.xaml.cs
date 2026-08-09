using Kier.Mobile.Models;
using Kier.Mobile.Services;

namespace Kier.Mobile.Views;

public partial class PaymentPage : ContentPage
{
    private readonly ApiService _apiService;

    public PaymentPage()
    {
        InitializeComponent();
        _apiService = ApiService.Shared;
    }

    private async void OnSubmitPaymentClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        if (!int.TryParse(StudentIdEntry.Text?.Trim(), out var studentId) || studentId <= 0)
        {
            PaymentStatusLabel.Text = "Valid student ID is required.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        if (!decimal.TryParse(AmountPaidEntry.Text?.Trim(), out var amountPaid) || amountPaid <= 0m)
        {
            PaymentStatusLabel.Text = "Enter a valid amount paid.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        var request = new CreateCollectionRequest(
            studentId,
            amountPaid,
            DateTime.UtcNow,
            CollectorNameEntry.Text?.Trim(),
            ReceiptNumberEntry.Text?.Trim(),
            CategoryEntry.Text?.Trim());

        var result = await _apiService.CreateCollectionAsync(request);
        if (result is null)
        {
            PaymentStatusLabel.Text = "Payment submission failed. Check server or login.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        PaymentStatusLabel.Text = $"Payment recorded: {result.AmountPaid:0.00} for student {result.StudentId}.";
        StudentIdEntry.Text = string.Empty;
        AmountPaidEntry.Text = string.Empty;
        CollectorNameEntry.Text = string.Empty;
        ReceiptNumberEntry.Text = string.Empty;
        CategoryEntry.Text = string.Empty;

        if (sender is Button button3)
        {
            button3.IsEnabled = true;
        }
    }
}
