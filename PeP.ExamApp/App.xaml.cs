using System.Configuration;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace PeP.ExamApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (!SecurityChecks.IsRunningAsAdmin())
        {
            TryRelaunchAsAdmin();
            Shutdown();
            return;
        }

        base.OnStartup(e);
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
