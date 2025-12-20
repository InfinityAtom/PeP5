using System.Windows;
using System.Windows.Controls;

namespace PeP.ExamApp.Pages;

public partial class TeacherAuthPage : Page
{
    private readonly MainWindow _mainWindow;

    public TeacherAuthPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        var exam = _mainWindow.State.ExamInfo;
        ExamText.Text = exam?.ExamTitle ?? "-";
        TeacherText.Text = exam?.TeacherName ?? "-";
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new ExamCodePage(_mainWindow));
    }

    private async void OnAuthorizeClick(object sender, RoutedEventArgs e)
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

        if (string.IsNullOrWhiteSpace(_mainWindow.State.ExamCode))
        {
            ErrorText.Text = "Exam code is missing. Go back and enter the code again.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        var teacherPassword = TeacherPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(teacherPassword))
        {
            ErrorText.Text = "Teacher password is required.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        AuthorizeButton.IsEnabled = false;
        StatusText.Text = "Authorizing...";

        try
        {
            var result = await api.AuthorizeAsync(_mainWindow.State.ExamCode, teacherPassword);
            if (!result.Success || string.IsNullOrWhiteSpace(result.AuthorizationToken))
            {
                ErrorText.Text = result.Error ?? "Authorization failed.";
                ErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            _mainWindow.State.AuthorizationToken = result.AuthorizationToken;
            _mainWindow.State.AuthorizationExpiresAtUtc = result.ExpiresAtUtc;
            _mainWindow.State.ExamInfo = result.Exam ?? _mainWindow.State.ExamInfo;
            _mainWindow.State.TeacherPassword = teacherPassword; // Store for exit validation

            _mainWindow.Navigate(new TutorialPage(_mainWindow));
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
        finally
        {
            AuthorizeButton.IsEnabled = true;
            StatusText.Text = string.Empty;
            TeacherPasswordBox.Password = string.Empty;
        }
    }
}

