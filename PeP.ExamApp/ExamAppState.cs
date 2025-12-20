using System.Net;
using System.Net.Http;

namespace PeP.ExamApp;

public class ExamAppState
{
    public Uri? ServerBaseUri { get; private set; }
    public CookieContainer CookieContainer { get; private set; } = new();
    public HttpClient? HttpClient { get; private set; }
    public ExamAppApiClient? ApiClient { get; private set; }

    public string? StudentEmail { get; set; }
    public string? ExamCode { get; set; }
    public ExamAppApiClient.ExamAppExamInfoDto? ExamInfo { get; set; }
    public string? AuthorizationToken { get; set; }
    public DateTime? AuthorizationExpiresAtUtc { get; set; }
    public string? TeacherPassword { get; set; }

    public int? AttemptId { get; set; }
    public string? LaunchToken { get; set; }
    public DateTime? LaunchExpiresAtUtc { get; set; }

    public void ConfigureServer(string serverUrl)
    {
        if (!Uri.TryCreate(serverUrl.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid server URL.", nameof(serverUrl));
        }

        ServerBaseUri = uri;
        CookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = CookieContainer,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(30)
        };

        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PePExamApp/0.1");
        ApiClient = new ExamAppApiClient(HttpClient);
    }
}
