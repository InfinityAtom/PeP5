using System.Configuration;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using PeP.ExamApp.Services;

namespace PeP.ExamApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private UpdateService? _updateService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Add global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        if (!SecurityChecks.IsRunningAsAdmin())
        {
            TryRelaunchAsAdmin();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Check for updates in the background
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            // Initialize update service with your server URL
            _updateService = new UpdateService("https://localhost:7170");
            
            // Clean up old update files
            _updateService.CleanupOldUpdates();

            // Check for updates
            var updateInfo = await _updateService.CheckForUpdateAsync();
            
            if (updateInfo != null)
            {
                await _updateService.PromptAndInstallUpdateAsync(updateInfo);
            }
        }
        catch (Exception ex)
        {
            // Don't block app startup if update check fails
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Debug.WriteLine($"Unhandled exception: {ex?.Message}\n{ex?.StackTrace}");
        
        // Show error but try to keep app running
        MessageBox.Show(
            $"An unexpected error occurred: {ex?.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"Dispatcher exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
        
        // Mark as handled to prevent crash
        e.Handled = true;
        
        MessageBox.Show(
            $"An error occurred: {e.Exception.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"Unobserved task exception: {e.Exception.Message}\n{e.Exception.StackTrace}");
        e.SetObserved(); // Prevent crash
    }

    private static void TryRelaunchAsAdmin()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                MessageBox.Show(
                    "PeP Exam App must be run as administrator to start an exam.",
                    "Administrator required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Win32Exception)
        {
            // User cancelled UAC prompt.
            MessageBox.Show(
                "PeP Exam App must be run as administrator to start an exam.",
                "Administrator required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to relaunch as administrator: {ex.Message}",
                "Administrator required",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
