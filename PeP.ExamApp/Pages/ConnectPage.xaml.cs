using System.Windows;
using System.Windows.Controls;

namespace PeP.ExamApp.Pages;

public partial class ConnectPage : Page
{
    private readonly MainWindow _mainWindow;

    public ConnectPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        ServerUrlTextBox.Text = _mainWindow.State.ServerBaseUri?.ToString() ?? "https://localhost:5001";
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        ErrorBorder.Visibility = Visibility.Collapsed;

        try
        {
            _mainWindow.State.ConfigureServer(ServerUrlTextBox.Text);
            _mainWindow.Navigate(new LoginPage(_mainWindow));
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

