using Kier.Mobile.Services;
using Kier.Mobile.ViewModels;
using Microsoft.Maui.Controls;

namespace Kier.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly LoginViewModel _viewModel;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = ApiService.Shared;
        _viewModel = new LoginViewModel();
        BindingContext = _viewModel;
        _viewModel.LoadSavedAuth();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_apiService.HasToken && _apiService.HasServerUrl)
        {
            var valid = await _apiService.ValidateTokenAsync();
            if (valid)
            {
                await Shell.Current.GoToAsync("//attendance");
            }
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        var success = await _viewModel.LoginAsync();
        if (success)
        {
            await Shell.Current.GoToAsync("//attendance");
        }

        if (sender is Button button2)
        {
            button2.IsEnabled = true;
        }
    }
}
