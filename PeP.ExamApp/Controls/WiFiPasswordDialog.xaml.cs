using System.Windows;

namespace PeP.ExamApp.Controls;

public partial class WiFiPasswordDialog : Window
{
    public string Password => PasswordInput.Password;

    public WiFiPasswordDialog(string networkName)
    {
        InitializeComponent();
        NetworkNameText.Text = networkName;
        
        Loaded += (s, e) => PasswordInput.Focus();
        
        // Allow Enter key to submit
        PasswordInput.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OnConnectClick(this, new RoutedEventArgs());
            }
        };
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordInput.Password))
        {
            ErrorText.Text = "Please enter the WiFi password";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (PasswordInput.Password.Length < 8)
        {
            ErrorText.Text = "WiFi password must be at least 8 characters";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        DialogResult = true;
        Close();
    }
}
