using System.Windows;
using System.Windows.Controls;

namespace PeP.ExamApp.Pages;

public partial class ExamCodePage : Page
{
    private readonly MainWindow _mainWindow;

    public ExamCodePage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        CodeTextBox.Text = _mainWindow.State.ExamCode ?? string.Empty;
        RenderExamInfo(_mainWindow.State.ExamInfo);
        ContinueButton.IsEnabled = _mainWindow.State.ExamInfo != null;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new LoginPage(_mainWindow));
    }

    private async void OnLookupClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        ErrorBorder.Visibility = Visibility.Collapsed;
        StatusText.Text = string.Empty;
        ContinueButton.IsEnabled = false;

        var api = _mainWindow.State.ApiClient;
        if (api == null)
        {
            ErrorText.Text = "Server is not configured.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        var code = CodeTextBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorText.Text = "Exam code is required.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        LookupButton.IsEnabled = false;
        StatusText.Text = "Looking up exam code...";

        try
        {
            var result = await api.GetExamInfoAsync(code);
            if (!result.Success || result.Exam == null)
            {
                RenderExamInfo(null);
                ErrorText.Text = result.Error ?? "Invalid exam code.";
                ErrorBorder.Visibility = Visibility.Visible;
                _mainWindow.State.ExamInfo = null;
                return;
            }

            _mainWindow.State.ExamCode = code;
            _mainWindow.State.ExamInfo = result.Exam;
            RenderExamInfo(result.Exam);
            ContinueButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            RenderExamInfo(null);
            ErrorText.Text = ex.Message;
            ErrorBorder.Visibility = Visibility.Visible;
            _mainWindow.State.ExamInfo = null;
        }
        finally
        {
            LookupButton.IsEnabled = true;
            StatusText.Text = string.Empty;
        }
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        if (_mainWindow.State.ExamInfo == null || string.IsNullOrWhiteSpace(_mainWindow.State.ExamCode))
        {
            ErrorText.Text = "Please look up a valid exam code first.";
            ErrorBorder.Visibility = Visibility.Visible;
            return;
        }

        _mainWindow.Navigate(new TeacherAuthPage(_mainWindow));
    }

    private void RenderExamInfo(ExamAppApiClient.ExamAppExamInfoDto? exam)
    {
        if (exam != null)
        {
            ExamDetailsCard.Visibility = Visibility.Visible;
            ExamTitleText.Text = exam.ExamTitle ?? "-";
            CourseText.Text = exam.CourseName ?? "-";
            TeacherText.Text = exam.TeacherName ?? "-";
            DurationText.Text = $"{exam.DurationMinutes} minutes";
        }
        else
        {
            ExamDetailsCard.Visibility = Visibility.Collapsed;
            ExamTitleText.Text = "-";
            CourseText.Text = "-";
            TeacherText.Text = "-";
            DurationText.Text = "-";
        }
    }
}

