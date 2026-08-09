using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Kier.Mobile.Services;
using Kier.Mobile.Models;
using Microsoft.Maui.Storage;

namespace Kier.Mobile.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly ApiService _apiService;

    public LoginViewModel()
        : this(ApiService.Shared)
    {
    }

    public LoginViewModel(ApiService apiService)
    {
        _apiService = apiService;
        ServerUrl = Preferences.Get("server-url", string.Empty);
        Username = Preferences.Get("username", string.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _serverUrl = string.Empty;
    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy { get; private set; }

    public async Task<bool> LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            StatusMessage = "Server URL is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Username and password are required.";
            return false;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Logging in...";
            _apiService.SetBaseUrl(ServerUrl);
            var result = await _apiService.LoginAsync(new LoginRequest(Username.Trim(), Password));
            if (result is null)
            {
                StatusMessage = "Login failed. Check credentials and server.";
                return false;
            }

            _apiService.SetToken(result.Token);
            Preferences.Set("server-url", ServerUrl);
            Preferences.Set("username", Username);
            Preferences.Set("auth-token", result.Token);
            StatusMessage = "Login successful.";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = "Login request failed: " + ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LoadSavedAuth()
    {
        ServerUrl = Preferences.Get("server-url", string.Empty);
        Username = Preferences.Get("username", string.Empty);
        var token = Preferences.Get("auth-token", string.Empty);
        if (!string.IsNullOrWhiteSpace(token))
        {
            _apiService.SetBaseUrl(ServerUrl);
            _apiService.SetToken(token);
        }
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
