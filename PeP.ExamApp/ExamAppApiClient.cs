using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace PeP.ExamApp;

public class ExamAppApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public ExamAppApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        var form = new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = password,
            ["rememberMe"] = "false"
        };

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync("/Account/ProcessLogin", content);

        var location = response.Headers.Location?.ToString() ?? string.Empty;

        if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.RedirectKeepVerb)
        {
            if (location.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Invalid credentials.");
            }

            return (true, null);
        }

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        return (false, $"Login failed ({(int)response.StatusCode}).");
    }

    public async Task<(bool Success, string? Error, ExamAppExamInfoDto? Exam)> GetExamInfoAsync(string code)
    {
        var response = await _httpClient.GetFromJsonAsync<ExamCodeInfoResponse>($"/api/exam-app/code/{Uri.EscapeDataString(code)}", JsonOptions);
        if (response == null)
        {
            return (false, "No response from server.", null);
        }

        return (response.Success, response.Error, response.Exam);
    }

    public async Task<(bool Success, string? Error, string? AuthorizationToken, DateTime? ExpiresAtUtc, ExamAppExamInfoDto? Exam)> AuthorizeAsync(string code, string teacherPassword)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/exam-app/authorize", new AuthorizeRequest(code, teacherPassword), JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<AuthorizeResponse>(JsonOptions);

        if (payload == null)
        {
            return (false, "No response from server.", null, null, null);
        }

        return (payload.Success, payload.Error, payload.AuthorizationToken, payload.ExpiresAtUtc, payload.Exam);
    }

    public async Task<(bool Success, string? Error, int? AttemptId, string? LaunchToken, DateTime? ExpiresAtUtc)> StartAsync(string authorizationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/exam-app/start", new StartRequest(authorizationToken), JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<StartResponse>(JsonOptions);

        if (payload == null)
        {
            return (false, "No response from server.", null, null, null);
        }

        return (payload.Success, payload.Error, payload.AttemptId, payload.LaunchToken, payload.ExpiresAtUtc);
    }

    public async Task<(bool Success, string? Error)> SubmitExamAsync(int attemptId)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/exam-app/submit", new SubmitRequest(attemptId), JsonOptions);
            var payload = await response.Content.ReadFromJsonAsync<SubmitResponse>(JsonOptions);

            if (payload == null)
            {
                return (false, "No response from server.");
            }

            return (payload.Success, payload.Error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public record ExamCodeInfoResponse(bool Success, string? Error, ExamAppExamInfoDto? Exam);

    public record ExamAppExamInfoDto(
        int ExamId, 
        int ExamCodeId, 
        string ExamTitle, 
        string? CourseName, 
        int DurationMinutes, 
        string TeacherName,
        string? TeacherPassword = null);

    public record AuthorizeRequest(string Code, string TeacherPassword);

    public record AuthorizeResponse(bool Success, string? Error, string? AuthorizationToken, DateTime? ExpiresAtUtc, ExamAppExamInfoDto? Exam);

    public record StartRequest(string AuthorizationToken);

    public record StartResponse(bool Success, string? Error, int? AttemptId, string? LaunchToken, DateTime? ExpiresAtUtc);

    public record SubmitRequest(int AttemptId);

    public record SubmitResponse(bool Success, string? Error);
}
