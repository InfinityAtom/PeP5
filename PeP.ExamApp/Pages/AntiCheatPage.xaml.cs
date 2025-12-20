using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PeP.ExamApp.Pages;

public partial class AntiCheatPage : Page
{
    private readonly MainWindow _mainWindow;
    private SecurityChecks.SecurityReport? _lastReport;

    // Colors for status
    private static readonly SolidColorBrush SuccessColor = new(Color.FromRgb(22, 163, 74));   // Green
    private static readonly SolidColorBrush FailureColor = new(Color.FromRgb(220, 38, 38));   // Red
    private static readonly SolidColorBrush WarningColor = new(Color.FromRgb(217, 119, 6));   // Amber
    private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(240, 253, 244));
    private static readonly SolidColorBrush FailureBg = new(Color.FromRgb(254, 242, 242));
    private static readonly SolidColorBrush SuccessBorder = new(Color.FromRgb(134, 239, 172));
    private static readonly SolidColorBrush FailureBorder = new(Color.FromRgb(254, 202, 202));

    public AntiCheatPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        Loaded += (_, _) => RunChecks();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new TutorialPage(_mainWindow));
    }

    private void OnRecheckClick(object sender, RoutedEventArgs e)
    {
        RunChecks();
    }

    private void OnCloseAppsClick(object sender, RoutedEventArgs e)
    {
        CloseAppsButton.IsEnabled = false;
        CloseAppsButton.Content = "Closing...";

        try
        {
            var (killed, failed) = SecurityChecks.KillBlacklistedProcesses();
            
            // Wait a moment for processes to fully terminate
            System.Threading.Thread.Sleep(500);
            
            // Re-run checks
            RunChecks();

            if (failed.Count > 0)
            {
                MessageBox.Show(
                    $"Successfully closed: {string.Join(", ", killed)}\n\nCould not close (may require manual closure): {string.Join(", ", failed)}",
                    "Some Apps Could Not Be Closed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else if (killed.Count > 0)
            {
                // Just re-check, no message needed
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error closing applications: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CloseAppsButton.IsEnabled = true;
            CloseAppsButton.Content = "Close All";
        }
    }

    private void OnLaunchClick(object sender, RoutedEventArgs e)
    {
        RunChecks();
        if (LaunchButton.IsEnabled != true || _lastReport == null || !_lastReport.CanLaunchExam)
        {
            return;
        }

        _mainWindow.Navigate(new ExamRunnerPage(_mainWindow));
    }

    private void RunChecks()
    {
        _lastReport = SecurityChecks.RunFullSecurityCheck();
        UpdateUI(_lastReport);
    }

    private void UpdateUI(SecurityChecks.SecurityReport report)
    {
        // Update individual check items
        UpdateCheckItem(AdminCheckBorder, AdminIcon, AdminStatus, 
            report.IsAdmin, "PASSED", "FAILED");

        UpdateCheckItem(MonitorCheckBorder, MonitorIcon, MonitorStatus,
            report.MonitorCount == 1, "PASSED", $"{report.MonitorCount} displays");

        UpdateCheckItem(RemoteCheckBorder, RemoteIcon, RemoteStatus,
            !report.IsRemoteSession, "PASSED", "DETECTED");

        UpdateCheckItem(VmCheckBorder, VmIcon, VmStatus,
            !report.IsVM, "PASSED", report.VmType ?? "DETECTED");

        UpdateCheckItem(DebuggerCheckBorder, DebuggerIcon, DebuggerStatus,
            !report.IsDebuggerAttached, "PASSED", "DETECTED");

        var hasBlacklisted = report.BlacklistedProcesses.Count > 0;
        UpdateCheckItem(ProcessCheckBorder, ProcessIcon, ProcessStatus,
            !hasBlacklisted, "PASSED", $"{report.BlacklistedProcesses.Count} found");
        
        // Show/hide close all button
        CloseAppsButton.Visibility = hasBlacklisted ? Visibility.Visible : Visibility.Collapsed;
        
        if (hasBlacklisted)
        {
            ProcessDescription.Text = $"Found: {string.Join(", ", report.BlacklistedProcesses.Take(5))}";
            if (report.BlacklistedProcesses.Count > 5)
                ProcessDescription.Text += $" and {report.BlacklistedProcesses.Count - 5} more...";
        }
        else
        {
            ProcessDescription.Text = "No screen recording, remote access, or cheat tools detected";
        }

        UpdateCheckItem(RecordingCheckBorder, RecordingIcon, RecordingStatus,
            !report.IsScreenRecording, "PASSED", "ACTIVE");

        // Update summary
        if (report.CanLaunchExam)
        {
            StatusSummaryBorder.Background = SuccessBg;
            StatusSummaryBorder.BorderBrush = SuccessBorder;
            StatusSummaryBorder.BorderThickness = new Thickness(1);
            StatusIcon.Text = "✅";
            StatusTitle.Text = "All Security Checks Passed";
            StatusTitle.Foreground = SuccessColor;
            StatusDescription.Text = "Your system is ready for the secure exam environment.";
            StatusDescription.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
            LaunchButton.IsEnabled = true;
        }
        else
        {
            StatusSummaryBorder.Background = FailureBg;
            StatusSummaryBorder.BorderBrush = FailureBorder;
            StatusSummaryBorder.BorderThickness = new Thickness(1);
            StatusIcon.Text = "❌";
            StatusTitle.Text = "Security Checks Failed";
            StatusTitle.Foreground = FailureColor;
            
            var reasons = report.GetBlockingReasons();
            StatusDescription.Text = reasons.Count > 0 
                ? reasons[0] 
                : "Please resolve the issues highlighted below.";
            StatusDescription.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            LaunchButton.IsEnabled = false;
        }
    }

    private void UpdateCheckItem(Border border, TextBlock icon, TextBlock status, 
        bool passed, string passedText, string failedText)
    {
        if (passed)
        {
            border.Background = SuccessBg;
            border.BorderBrush = SuccessBorder;
            icon.Text = "✓";
            icon.Foreground = SuccessColor;
            status.Text = passedText;
            status.Foreground = SuccessColor;
        }
        else
        {
            border.Background = FailureBg;
            border.BorderBrush = FailureBorder;
            icon.Text = "✗";
            icon.Foreground = FailureColor;
            status.Text = failedText;
            status.Foreground = FailureColor;
        }
    }
}
