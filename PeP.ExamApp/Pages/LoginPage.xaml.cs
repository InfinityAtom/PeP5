using System.Windows;
using System.Windows.Controls;

namespace PeP.ExamApp.Pages;

public partial class LoginPage : Page
{
    private readonly MainWindow _mainWindow;

    public LoginPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        EmailTextBox.Text = _mainWindow.State.StudentEmail ?? string.Empty;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new ConnectPage(_mainWindow));
    }

    private async void OnSignInClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        ErrorBorder.Visibility = Visibility.Collapsed;
        StatusText.Text = string.Empty;

        var api = _mainWindow.State.ApiClient;
        if (api == null)
        {
            ErrorText.Text = "Server is not configured.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        var email = EmailTextBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorText.Text = "Email and password are required.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        SignInButton.IsEnabled = false;
        StatusText.Text = "Signing in...";

        try
        {
            var result = await api.LoginAsync(email, password);
            if (!result.Success)
            {
                ErrorText.Text = result.Error ?? "Sign in failed.";
                ErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            _mainWindow.State.StudentEmail = email;
            _mainWindow.Navigate(new ExamCodePage(_mainWindow));
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
        finally
        {
            SignInButton.IsEnabled = true;
            StatusText.Text = string.Empty;
        }
    }
}

