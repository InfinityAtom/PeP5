using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PeP.ExamApp.Controls;

public partial class ExamTaskbar : UserControl
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _networkTimer;
    private MainWindow? _mainWindow;
    private string? _teacherPassword;

    public event EventHandler? ExitRequested;

    public ExamTaskbar()
    {
        InitializeComponent();

        // Clock timer - update every second
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;

        // Network check timer - check every 5 seconds
        _networkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _networkTimer.Tick += OnNetworkTick;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        
        // Handle popup closed event (e.g., clicking outside)
        NetworkPopup.Closed += OnNetworkPopupClosed;
    }

    private void OnNetworkPopupClosed(object? sender, EventArgs e)
    {
        // Resume hooks when popup closes by any means
        _mainWindow?.ResumeHooks();
    }

    public void Initialize(MainWindow mainWindow, string? examTitle, string? teacherPassword)
    {
        _mainWindow = mainWindow;
        _teacherPassword = teacherPassword;
        
        if (!string.IsNullOrWhiteSpace(examTitle))
        {
            ExamTitleText.Text = examTitle;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateClock();
        UpdateDate();
        CheckNetworkStatus();
        
        _clockTimer.Start();
        _networkTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _clockTimer.Stop();
        _networkTimer.Stop();
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        UpdateClock();
        UpdateViolationCount();
    }

    private void OnNetworkTick(object? sender, EventArgs e)
    {
        CheckNetworkStatus();
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private void UpdateDate()
    {
        DateText.Text = DateTime.Now.ToString("MMM dd, yyyy");
    }

    private void UpdateViolationCount()
    {
        if (_mainWindow != null)
        {
            var count = _mainWindow.GetSecurityViolationCount();
            if (count > 0)
            {
                ViolationBorder.Visibility = Visibility.Visible;
                ViolationCountText.Text = count == 1 ? "1 violation" : $"{count} violations";
            }
            else
            {
                ViolationBorder.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void CheckNetworkStatus()
    {
        try
        {
            bool isConnected = NetworkInterface.GetIsNetworkAvailable();
            
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    InternetIndicator.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    InternetStatusText.Text = "Online";
                }
                else
                {
                    InternetIndicator.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    InternetStatusText.Text = "Offline";
                }
            });
        }
        catch
        {
            // Ignore network check errors
        }
    }

    private void OnInternetToggleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            // Check if _mainWindow is available
            if (_mainWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: MainWindow not initialized in ExamTaskbar");
                // Try to get it from visual tree
                var window = Window.GetWindow(this);
                if (window is MainWindow mw)
                {
                    _mainWindow = mw;
                }
            }
            
            // Suspend hooks when opening network panel to allow interaction
            if (!NetworkPopup.IsOpen)
            {
                _mainWindow?.SuspendHooks();
                NetworkPopup.IsOpen = true;
            }
            else
            {
                NetworkPopup.IsOpen = false;
                _mainWindow?.ResumeHooks();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling network popup: {ex.Message}");
            // Ensure we try to resume hooks on error
            try { _mainWindow?.ResumeHooks(); } catch { }
        }
    }

    private void OnNetworkPanelClose(object? sender, EventArgs e)
    {
        try
        {
            NetworkPopup.IsOpen = false;
            _mainWindow?.ResumeHooks();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing network panel: {ex.Message}");
        }
    }

    private void OnNetworkChanged(object? sender, string networkName)
    {
        // Refresh network status when network changes
        CheckNetworkStatus();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        if (_mainWindow == null) return;

        // Suspend keyboard hooks to allow typing in password dialog
        _mainWindow.SuspendHooks();
        
        try
        {
            // Show password dialog
            var dialog = new ExitPasswordDialog(_teacherPassword);
            dialog.Owner = Window.GetWindow(this);
            
            if (dialog.ShowDialog() == true)
            {
                // Password correct - request exit
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            // Resume hooks if dialog was cancelled
            _mainWindow.ResumeHooks();
        }
    }
}

/// <summary>
/// Dialog for entering teacher password to exit the exam safely.
/// </summary>
public class ExitPasswordDialog : Window
{
    private readonly string? _correctPassword;
    private readonly System.Windows.Controls.PasswordBox _passwordBox;
    private readonly TextBlock _errorText;

    public ExitPasswordDialog(string? teacherPassword)
    {
        _correctPassword = teacherPassword;
        
        Title = "Exit Secure Exam";
        Width = 400;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));

        var grid = new Grid { Margin = new Thickness(24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Icon and title
        var iconText = new TextBlock
        {
            Text = "🔐",
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(iconText, 0);
        grid.Children.Add(iconText);

        var titleText = new TextBlock
        {
            Text = "Teacher Password Required",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(titleText, 1);
        grid.Children.Add(titleText);

        var subtitleText = new TextBlock
        {
            Text = "Enter the teacher password to submit and exit the exam safely.",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(subtitleText, 2);
        grid.Children.Add(subtitleText);

        // Password input
        _passwordBox = new System.Windows.Controls.PasswordBox
        {
            FontSize = 14,
            Padding = new Thickness(12, 10, 12, 10),
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Foreground = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8)
        };
        _passwordBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ValidateAndClose();
        };
        Grid.SetRow(_passwordBox, 3);
        grid.Children.Add(_passwordBox);

        // Error text
        _errorText = new TextBlock
        {
            Text = "",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            Margin = new Thickness(0, 0, 0, 8),
            Visibility = Visibility.Collapsed
        };
        Grid.SetRow(_errorText, 4);
        grid.Children.Add(_errorText);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 10, 20, 10),
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            Foreground = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        cancelButton.Click += (s, e) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        var submitButton = new Button
        {
            Content = "Submit & Exit",
            Padding = new Thickness(20, 10, 20, 10),
            Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            Foreground = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        submitButton.Click += (s, e) => ValidateAndClose();
        buttonPanel.Children.Add(submitButton);

        Grid.SetRow(buttonPanel, 5);
        grid.Children.Add(buttonPanel);

        Content = grid;
        
        Loaded += (s, e) => _passwordBox.Focus();
    }

    private void ValidateAndClose()
    {
        if (string.IsNullOrWhiteSpace(_correctPassword))
        {
            // No password set - allow exit
            DialogResult = true;
            return;
        }

        if (_passwordBox.Password == _correctPassword)
        {
            DialogResult = true;
        }
        else
        {
            _errorText.Text = "Incorrect password. Please try again.";
            _errorText.Visibility = Visibility.Visible;
            _passwordBox.Clear();
            _passwordBox.Focus();
        }
    }
}
