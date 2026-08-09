using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Kier.Mobile.Models;

namespace Kier.Mobile.Services;

public class ApiService
{
    public static ApiService Shared { get; } = new();

    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string BaseUrl { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;

    public bool HasServerUrl => !string.IsNullOrWhiteSpace(BaseUrl);
    public bool HasToken => !string.IsNullOrWhiteSpace(Token);

    public void SetBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            BaseUrl = string.Empty;
            return;
        }

        BaseUrl = baseUrl.Trim();
        if (!BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            BaseUrl = "http://" + BaseUrl;
        }

        if (!BaseUrl.EndsWith("/"))
        {
            BaseUrl += "/";
        }
    }

    public void SetToken(string token)
    {
        Token = token?.Trim() ?? string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", Token);
    }

    private Uri BuildUri(string path)
    {
        if (!HasServerUrl)
        {
            throw new InvalidOperationException("Server URL has not been configured.");
        }

        return new Uri(new Uri(BaseUrl), path.TrimStart('/'));
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(BuildUri("api/auth/login"), requestContent);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
    }

    public async Task<bool> ValidateTokenAsync()
    {
        if (!HasToken)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(BuildUri("api/auth/me"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AttendanceRecordDto?> ScanAttendanceAsync(ScanAttendanceRequest request)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(BuildUri("api/attendance/scan"), requestContent);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AttendanceRecordDto>(content, _jsonOptions);
    }

    public async Task<AttendanceEventDto[]?> GetEventsAsync()
    {
        var response = await _httpClient.GetAsync(BuildUri("api/attendance/events"));
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AttendanceEventDto[]>(content, _jsonOptions);
    }

    public async Task<StudentDto[]?> GetStudentsAsync()
    {
        var response = await _httpClient.GetAsync(BuildUri("api/students"));
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StudentDto[]>(content, _jsonOptions);
    }

    public async Task<StudentDto?> CreateStudentAsync(CreateStudentRequest request)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(BuildUri("api/students"), requestContent);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StudentDto>(content, _jsonOptions);
    }

    public async Task<CollectionDto?> CreateCollectionAsync(CreateCollectionRequest request)
    {
        var requestContent = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(BuildUri("api/collections"), requestContent);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CollectionDto>(content, _jsonOptions);
    }
}

