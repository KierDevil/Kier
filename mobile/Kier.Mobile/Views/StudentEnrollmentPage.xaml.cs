using Kier.Mobile.Models;
using Kier.Mobile.Services;

namespace Kier.Mobile.Views;

public partial class StudentEnrollmentPage : ContentPage
{
    private readonly ApiService _apiService;

    public StudentEnrollmentPage()
    {
        InitializeComponent();
        _apiService = ApiService.Shared;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStudentsAsync();
    }

    private async Task LoadStudentsAsync()
    {
        StatusLabel.Text = "Loading students...";
        StudentsCollection.ItemsSource = null;

        var students = await _apiService.GetStudentsAsync();
        if (students is null)
        {
            StatusLabel.Text = "Unable to load active students.";
            return;
        }

        StudentsCollection.ItemsSource = students;
        StatusLabel.Text = $"Loaded {students.Length} student(s).";
    }

    private async void OnCreateStudentClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        var studentNo = StudentNoEntry.Text?.Trim() ?? string.Empty;
        var firstName = FirstNameEntry.Text?.Trim() ?? string.Empty;
        var lastName = LastNameEntry.Text?.Trim() ?? string.Empty;
        var course = CourseEntry.Text?.Trim() ?? string.Empty;
        var yearLevel = YearLevelEntry.Text?.Trim() ?? string.Empty;
        var contact = ContactEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim();
        var rfidUid = RfidEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(studentNo) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            StatusLabel.Text = "Student number, first name, and last name are required.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        var request = new CreateStudentRequest(
            studentNo,
            firstName,
            lastName,
            course,
            yearLevel,
            contact,
            email,
            rfidUid);

        var created = await _apiService.CreateStudentAsync(request);
        if (created is null)
        {
            StatusLabel.Text = "Unable to enroll student. Check server or login.";
            if (sender is Button button2) button2.IsEnabled = true;
            return;
        }

        StatusLabel.Text = $"Enrolled {created.Name} ({created.StudentNo}).";
        await LoadStudentsAsync();

        StudentNoEntry.Text = string.Empty;
        FirstNameEntry.Text = string.Empty;
        LastNameEntry.Text = string.Empty;
        CourseEntry.Text = string.Empty;
        YearLevelEntry.Text = string.Empty;
        ContactEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
        RfidEntry.Text = string.Empty;

        if (sender is Button button3)
        {
            button3.IsEnabled = true;
        }
    }
}
