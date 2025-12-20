using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PeP.ExamApp.Pages;

public partial class ExamRunnerPage : Page
{
    private readonly MainWindow _mainWindow;
    private bool _started;
    private bool _examModeEntered;
    private bool _watermarkHooked;
    private string _watermarkText = string.Empty;

    public ExamRunnerPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;

        OverlayErrorText.Text = string.Empty;
        OverlayErrorBorder.Visibility = Visibility.Collapsed;

        var state = _mainWindow.State;
        var api = state.ApiClient;

        // Initialize taskbar with teacher password for secure exit
        ExamTaskbar.Initialize(_mainWindow, state.ExamInfo?.ExamTitle, state.TeacherPassword);

        if (state.ServerBaseUri == null || api == null)
        {
            ShowError("Server is not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(state.AuthorizationToken))
        {
            ShowError("Missing authorization token. Please restart the launch flow.");
            return;
        }

        try
        {
            var start = await api.StartAsync(state.AuthorizationToken);
            if (!start.Success || start.AttemptId == null || string.IsNullOrWhiteSpace(start.LaunchToken))
            {
                ShowError(start.Error ?? "Failed to start the exam.");
                return;
            }

            state.AttemptId = start.AttemptId;
            state.LaunchToken = start.LaunchToken;
            state.LaunchExpiresAtUtc = start.ExpiresAtUtc;

            if (!_examModeEntered)
            {
                _examModeEntered = true;
                _mainWindow.EnterExamMode();
            }

            await ExamWebView.EnsureCoreWebView2Async();

            // Tag user-agent so the server can distinguish the app
            var existingUa = ExamWebView.CoreWebView2.Settings.UserAgent;
            ExamWebView.CoreWebView2.Settings.UserAgent = $"PePExamApp/1.0 SecureBrowser {existingUa}";

            HardenWebView2(state.ServerBaseUri, ExamWebView.CoreWebView2);

            CopyCookiesToWebView(state.ServerBaseUri, state.CookieContainer, ExamWebView.CoreWebView2.CookieManager);

            // Listen for navigation to detect exam submission
            ExamWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            var token = Uri.EscapeDataString(start.LaunchToken);
            var url = new Uri(state.ServerBaseUri, $"/student/take-exam?attemptId={start.AttemptId}&launchToken={token}");
            ExamWebView.CoreWebView2.Navigate(url.ToString());

            SetupWatermark(state);

            Overlay.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Check if we navigated to results page (exam submitted)
        var currentUrl = ExamWebView.CoreWebView2.Source;
        if (currentUrl.Contains("/results") || currentUrl.Contains("/exam-completed"))
        {
            // Exam was submitted through the web interface - launch browser and close
            LaunchBrowserWithResults();
        }
    }

    private void OnTaskbarExitRequested(object? sender, EventArgs e)
    {
        // Teacher password was validated - submit exam and exit
        SubmitExamAndExit();
    }

    private void OnInternetToggled(object? sender, bool enabled)
    {
        // This is informational only - actual network control would require admin network commands
        // Could implement actual control with netsh commands if needed
    }

    private async void SubmitExamAndExit()
    {
        try
        {
            var state = _mainWindow.State;
            var api = state.ApiClient;

            if (api != null && state.AttemptId.HasValue)
            {
                // Submit the exam first
                await api.SubmitExamAsync(state.AttemptId.Value);
            }

            // Mark exam mode as exited
            _examModeEntered = false;

            // Launch browser with results and force exit
            LaunchBrowserWithResults();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error submitting exam: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchBrowserWithResults()
    {
        try
        {
            var state = _mainWindow.State;
            
            // Build results URL
            var resultsUrl = state.ServerBaseUri != null && state.AttemptId.HasValue
                ? new Uri(state.ServerBaseUri, $"/student/results/{state.AttemptId}").ToString()
                : state.ServerBaseUri?.ToString() ?? "about:blank";

            // Exit exam mode first
            if (_examModeEntered)
            {
                _examModeEntered = false;
            }

            // Launch default browser with results URL
            Process.Start(new ProcessStartInfo
            {
                FileName = resultsUrl,
                UseShellExecute = true
            });

            // Force exit the application (bypasses closing check)
            _mainWindow.ForceExit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error launching browser: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowError(string message)
    {
        OverlayErrorText.Text = message;
        OverlayErrorBorder.Visibility = Visibility.Visible;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_examModeEntered)
        {
            _examModeEntered = false;
            _mainWindow.ExitExamMode();
        }
    }

    private static void HardenWebView2(Uri serverBaseUri, CoreWebView2 core)
    {
        var settings = core.Settings;
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = false;
        settings.IsBuiltInErrorPageEnabled = false;
        settings.IsSwipeNavigationEnabled = false;

        core.PermissionRequested -= OnPermissionRequested;
        core.PermissionRequested += OnPermissionRequested;

        core.NewWindowRequested -= OnNewWindowRequested;
        core.NewWindowRequested += OnNewWindowRequested;

        core.DownloadStarting -= OnDownloadStarting;
        core.DownloadStarting += OnDownloadStarting;

        core.NavigationStarting -= OnNavigationStarting;
        core.NavigationStarting += OnNavigationStarting;

        void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!IsAllowedNavigation(serverBaseUri, e.Uri))
            {
                e.Cancel = true;
            }
        }

        static void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Block popups / external windows
            e.Handled = true;
        }

        static void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Block downloads during the exam
            e.Cancel = true;
        }

        static void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            if (e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead ||
                e.PermissionKind == CoreWebView2PermissionKind.Camera ||
                e.PermissionKind == CoreWebView2PermissionKind.Microphone ||
                e.PermissionKind == CoreWebView2PermissionKind.Geolocation ||
                e.PermissionKind == CoreWebView2PermissionKind.Notifications)
            {
                e.State = CoreWebView2PermissionState.Deny;
                e.Handled = true;
            }
        }

        static bool IsAllowedNavigation(Uri serverBaseUri, string? targetUri)
        {
            if (string.IsNullOrWhiteSpace(targetUri)) return false;
            if (!Uri.TryCreate(targetUri, UriKind.Absolute, out var uri)) return false;

            // Allow non-http(s) internal URIs (about:blank, etc)
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(uri.Scheme, serverBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(uri.Authority, serverBaseUri.Authority, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SetupWatermark(ExamAppState state)
    {
        var email = state.StudentEmail ?? "Student";
        var attempt = state.AttemptId?.ToString() ?? "-";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        _watermarkText = $"{email} • Attempt #{attempt} • {Environment.MachineName} • {timestamp}";

        if (!_watermarkHooked)
        {
            _watermarkHooked = true;
            WatermarkLayer.SizeChanged += (_, _) => RenderWatermark();
        }

        RenderWatermark();
    }

    private void RenderWatermark()
    {
        WatermarkLayer.Children.Clear();

        if (string.IsNullOrWhiteSpace(_watermarkText))
        {
            return;
        }

        var width = WatermarkLayer.ActualWidth;
        var height = WatermarkLayer.ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        const double xStep = 450;
        const double yStep = 200;
        var brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        for (double y = -yStep; y < height + yStep; y += yStep)
        {
            for (double x = -xStep; x < width + xStep; x += xStep)
            {
                var textBlock = new TextBlock
                {
                    Text = _watermarkText,
                    Foreground = brush,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    RenderTransform = new RotateTransform(-20)
                };

                Canvas.SetLeft(textBlock, x);
                Canvas.SetTop(textBlock, y);
                WatermarkLayer.Children.Add(textBlock);
            }
        }
    }

    private static void CopyCookiesToWebView(Uri serverBaseUri, CookieContainer cookieContainer, CoreWebView2CookieManager cookieManager)
    {
        foreach (Cookie cookie in cookieContainer.GetCookies(serverBaseUri))
        {
            var domain = string.IsNullOrWhiteSpace(cookie.Domain) ? serverBaseUri.Host : cookie.Domain.TrimStart('.');
            var webViewCookie = cookieManager.CreateCookie(cookie.Name, cookie.Value, domain, cookie.Path);

            webViewCookie.IsHttpOnly = cookie.HttpOnly;
            webViewCookie.IsSecure = cookie.Secure;

            if (cookie.Expires != DateTime.MinValue)
            {
                webViewCookie.Expires = cookie.Expires;
            }

            cookieManager.AddOrUpdateCookie(webViewCookie);
        }
    }
}
