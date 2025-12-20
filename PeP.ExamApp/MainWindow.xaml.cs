using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace PeP.ExamApp;

public partial class MainWindow : Window
{
    public ExamAppState State { get; } = new();
    private readonly ExamModeManager _examModeManager;
    private int _securityViolationCount;

    public MainWindow()
    {
        InitializeComponent();
        _examModeManager = new ExamModeManager(this);
        _examModeManager.SecurityViolationDetected += OnSecurityViolationDetected;
        
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Ensure true fullscreen covering entire screen including taskbar
        SetFullscreen();
        
        Navigate(new Pages.ConnectPage(this));
    }

    private void SetFullscreen()
    {
        // Get screen dimensions
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        
        // Position at 0,0 and cover entire screen
        Left = 0;
        Top = 0;
        Width = screenWidth;
        Height = screenHeight;
        
        // Ensure window is topmost
        Topmost = true;
    }

    public void Navigate(Page page)
    {
        MainFrame.Navigate(page);
    }

    public void EnterExamMode()
    {
        _examModeManager.Enter();
    }

    public void ExitExamMode()
    {
        _examModeManager.Exit();
    }

    public bool IsExamModeActive => _examModeManager.IsActive;

    /// <summary>
    /// Temporarily suspends keyboard hooks to allow dialog input.
    /// </summary>
    public void SuspendHooks()
    {
        _examModeManager.SuspendHooks();
    }

    /// <summary>
    /// Resumes keyboard hooks after dialog is closed.
    /// </summary>
    public void ResumeHooks()
    {
        _examModeManager.ResumeHooks();
    }

    /// <summary>
    /// Force exits the application, bypassing exam mode checks.
    /// </summary>
    public void ForceExit()
    {
        _examModeManager.Exit();
        _examModeManager.Dispose();
        Application.Current.Shutdown();
    }

    private void OnSecurityViolationDetected(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _securityViolationCount++;
            ShowSecurityViolationOverlay();

            // Log the violation (in production, this would be sent to the server)
            System.Diagnostics.Debug.WriteLine($"Security violation #{_securityViolationCount} detected at {DateTime.UtcNow}");
        });
    }

    private void ShowSecurityViolationOverlay()
    {
        SecurityOverlay.Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        SecurityOverlay.BeginAnimation(OpacityProperty, fadeIn);

        var hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        hideTimer.Tick += (_, _) =>
        {
            hideTimer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (_, _) => SecurityOverlay.Visibility = Visibility.Collapsed;
            SecurityOverlay.BeginAnimation(OpacityProperty, fadeOut);
        };
        hideTimer.Start();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_examModeManager.IsActive)
        {
            return;
        }

        // Additional in-app blocking as a fallback (primary blocking is in the low-level hook)
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.F4)
        {
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            switch (e.Key)
            {
                case Key.C:
                case Key.V:
                case Key.X:
                case Key.A:
                case Key.P:
                case Key.S:
                case Key.N:
                case Key.O:
                case Key.F:
                case Key.L:
                case Key.T:
                case Key.W:
                case Key.R:
                case Key.Tab:
                    e.Handled = true;
                    return;
            }
        }

        // Block Windows key combinations
        if (e.Key == Key.LWin || e.Key == Key.RWin)
        {
            e.Handled = true;
            return;
        }

        // Block Tab with Alt (already in hook, but double protection)
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Tab)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.PrintScreen || e.Key == Key.Snapshot)
        {
            e.Handled = true;
        }

        // Block Escape
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_examModeManager.IsActive)
        {
            e.Cancel = true;
            MessageBox.Show(
                "The exam is currently in progress.\n\nPlease submit your exam through the exam interface before closing the application.",
                "Exam in Progress",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            _examModeManager.Dispose();
        }
    }

    public int GetSecurityViolationCount() => _securityViolationCount;
}
