using System.Windows;
using System.Windows.Controls;

namespace PeP.ExamApp.Pages;

public partial class TutorialPage : Page
{
    private readonly MainWindow _mainWindow;

    public TutorialPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new TeacherAuthPage(_mainWindow));
    }

    private void OnAcknowledgedChanged(object sender, RoutedEventArgs e)
    {
        ContinueButton.IsEnabled = AcknowledgeCheckBox.IsChecked == true;
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        _mainWindow.Navigate(new AntiCheatPage(_mainWindow));
    }
}

