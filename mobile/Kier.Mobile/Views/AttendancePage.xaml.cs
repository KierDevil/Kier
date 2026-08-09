using Kier.Mobile.Models;
using Kier.Mobile.Services;
using Microsoft.Maui.Storage;

namespace Kier.Mobile.Views;

public partial class AttendancePage : ContentPage
{
    private readonly ApiService _apiService;
    private IReadOnlyList<AttendanceEventDto> _events = Array.Empty<AttendanceEventDto>();

    public AttendancePage()
    {
        InitializeComponent();
        _apiService = ApiService.Shared;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        ResultLabel.Text = "Refreshing events...";
        EventPicker.ItemsSource = null;

        var events = await _apiService.GetEventsAsync();
        if (events is null)
        {
            ResultLabel.Text = "Unable to load events. Make sure the backend is reachable.";
            return;
        }

        _events = events;
        EventPicker.ItemsSource = _events.ToList();
        EventPicker.SelectedIndex = _events.Count > 0 ? 0 : -1;
        ResultLabel.Text = _events.Count > 0 ? $"Loaded {_events.Count} event(s)." : "No events available. Enter a new event title.";
    }

    private async void OnRefreshEventsClicked(object sender, EventArgs e)
    {
        await LoadEventsAsync();
    }

    private async void OnScanQrClicked(object sender, EventArgs e)
    {
        var scanPage = new QRScanPage();
        scanPage.ScanCompleted += ScanPage_ScanCompleted;
        await Navigation.PushModalAsync(scanPage);
    }

    private void ScanPage_ScanCompleted(object? sender, string scanText)
    {
        if (string.IsNullOrWhiteSpace(scanText))
        {
            ResultLabel.Text = "No QR code data was detected.";
            return;
        }

        var normalized = scanText.Trim();
        StudentNoEntry.Text = normalized;
        ResultLabel.Text = "Ready to record attendance for scanned ID/RFID.";
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        ResultLabel.Text = "Recording attendance...";
        var studentNo = StudentNoEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(studentNo))
        {
            ResultLabel.Text = "Enter a student number or RFID.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        var eventTitle = EventTitleEntry.Text?.Trim();
        var selectedEvent = EventPicker.SelectedItem as AttendanceEventDto;
        if (selectedEvent is not null)
        {
            eventTitle = selectedEvent.Title;
        }

        if (string.IsNullOrWhiteSpace(eventTitle))
        {
            ResultLabel.Text = "Select or enter an event title.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        var request = new ScanAttendanceRequest(
            studentNo,
            studentNo,
            eventTitle,
            "Present",
            null,
            null,
            null,
            null,
            null,
            LocationEntry.Text?.Trim() ?? string.Empty,
            RemarksEntry.Text?.Trim() ?? string.Empty);

        var response = await _apiService.ScanAttendanceAsync(request);
        if (response is null)
        {
            ResultLabel.Text = "Attendance scan failed. Check network or login.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        ResultLabel.Text = $"Recorded {response.StudentName} ({response.StudentNo}) at {response.RecordedAt:G}.";
        if (sender is Button button3)
        {
            button3.IsEnabled = true;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("auth-token");
        await Shell.Current.GoToAsync("//login");
    }
}
