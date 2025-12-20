using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PeP.ExamApp.Controls;

public partial class NetworkPanel : UserControl
{
    private readonly List<WiFiNetwork> _networks = new();
    private string? _currentNetworkName;

    public event EventHandler? CloseRequested;
    public event EventHandler<string>? NetworkChanged;

    public NetworkPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshNetworksAsync();
        UpdateCurrentConnection();
        UpdateEthernetStatus();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshNetworksAsync();
    }

    private async void OnNetworkSelected(object sender, SelectionChangedEventArgs e)
    {
        if (NetworkList.SelectedItem is WiFiNetwork selectedNetwork)
        {
            if (selectedNetwork.Name == _currentNetworkName)
            {
                // Already connected
                return;
            }

            await ConnectToNetworkAsync(selectedNetwork);
        }
    }

    private void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        DisconnectCurrentNetwork();
    }

    private async Task RefreshNetworksAsync()
    {
        LoadingPanel.Visibility = Visibility.Visible;
        NetworkList.Visibility = Visibility.Collapsed;
        NoNetworksPanel.Visibility = Visibility.Collapsed;
        RefreshButton.IsEnabled = false;

        try
        {
            _networks.Clear();
            
            // Use netsh to get available networks
            var networks = await GetAvailableNetworksAsync();
            
            foreach (var network in networks)
            {
                _networks.Add(network);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                NetworkList.ItemsSource = null;
                NetworkList.ItemsSource = _networks;
                
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                if (_networks.Count > 0)
                {
                    NetworkList.Visibility = Visibility.Visible;
                    NoNetworksPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NetworkList.Visibility = Visibility.Collapsed;
                    NoNetworksPanel.Visibility = Visibility.Visible;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing networks: {ex.Message}");
            LoadingPanel.Visibility = Visibility.Collapsed;
            NoNetworksPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task<List<WiFiNetwork>> GetAvailableNetworksAsync()
    {
        var networks = new List<WiFiNetwork>();

        try
        {
            // Run netsh command to list available WiFi networks
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks mode=bssid",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return networks;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse the output
            var lines = output.Split('\n');
            WiFiNetwork? currentNetwork = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("SSID") && !trimmedLine.StartsWith("SSID ") && trimmedLine.Contains(":"))
                {
                    // New network entry - "SSID 1 : NetworkName"
                    var ssidMatch = Regex.Match(trimmedLine, @"SSID\s*\d*\s*:\s*(.+)");
                    if (ssidMatch.Success)
                    {
                        var ssid = ssidMatch.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(ssid) && !networks.Any(n => n.Name == ssid))
                        {
                            currentNetwork = new WiFiNetwork { Name = ssid };
                            networks.Add(currentNetwork);
                        }
                    }
                }
                else if (currentNetwork != null)
                {
                    if (trimmedLine.StartsWith("Network type", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Tipo de red", StringComparison.OrdinalIgnoreCase))
                    {
                        // Network type
                    }
                    else if (trimmedLine.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase) ||
                             trimmedLine.StartsWith("Autenticación", StringComparison.OrdinalIgnoreCase) ||
                             trimmedLine.StartsWith("Autenticaci", StringComparison.OrdinalIgnoreCase))
                    {
                        var authMatch = Regex.Match(trimmedLine, @":\s*(.+)");
                        if (authMatch.Success)
                        {
                            var auth = authMatch.Groups[1].Value.Trim();
                            currentNetwork.SecurityType = auth;
                            currentNetwork.IsSecured = !auth.Equals("Open", StringComparison.OrdinalIgnoreCase) &&
                                                       !auth.Equals("Abierta", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    else if (trimmedLine.StartsWith("Signal", StringComparison.OrdinalIgnoreCase) ||
                             trimmedLine.StartsWith("Señal", StringComparison.OrdinalIgnoreCase))
                    {
                        var signalMatch = Regex.Match(trimmedLine, @"(\d+)%");
                        if (signalMatch.Success && int.TryParse(signalMatch.Groups[1].Value, out int signal))
                        {
                            currentNetwork.SignalStrengthPercent = signal;
                            currentNetwork.SignalStrength = $"{signal}%";
                            currentNetwork.SignalIcon = GetSignalIcon(signal);
                        }
                    }
                }
            }

            // Sort by signal strength
            networks.Sort((a, b) => b.SignalStrengthPercent.CompareTo(a.SignalStrengthPercent));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting networks: {ex.Message}");
        }

        return networks;
    }

    private static string GetSignalIcon(int signalStrength)
    {
        return signalStrength switch
        {
            >= 80 => "📶",
            >= 60 => "📶",
            >= 40 => "📶",
            >= 20 => "📶",
            _ => "📶"
        };
    }

    private async Task ConnectToNetworkAsync(WiFiNetwork network)
    {
        if (network.IsSecured)
        {
            // Show password dialog
            var passwordDialog = new WiFiPasswordDialog(network.Name);
            
            // Get the parent window
            var parentWindow = Window.GetWindow(this);
            if (parentWindow is MainWindow mainWindow)
            {
                mainWindow.SuspendHooks();
            }

            try
            {
                passwordDialog.Owner = parentWindow;
                var result = passwordDialog.ShowDialog();
                
                if (result == true && !string.IsNullOrEmpty(passwordDialog.Password))
                {
                    await ConnectWithPasswordAsync(network.Name, passwordDialog.Password);
                }
            }
            finally
            {
                if (parentWindow is MainWindow mainWindow2)
                {
                    mainWindow2.ResumeHooks();
                }
            }
        }
        else
        {
            // Open network - connect directly
            await ConnectWithPasswordAsync(network.Name, null);
        }
    }

    private async Task ConnectWithPasswordAsync(string networkName, string? password)
    {
        try
        {
            ConnectionStatusText.Text = $"Connecting to {networkName}...";
            ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // Amber

            // First, check if we have a profile for this network
            var hasProfile = await CheckNetworkProfileExistsAsync(networkName);

            if (!hasProfile && !string.IsNullOrEmpty(password))
            {
                // Create a temporary profile
                await CreateNetworkProfileAsync(networkName, password);
            }

            // Connect using netsh
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"wlan connect name=\"{networkName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                // Wait a moment for connection to establish
                await Task.Delay(2000);
                
                UpdateCurrentConnection();
                NetworkChanged?.Invoke(this, networkName);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error connecting to network: {ex.Message}");
            ConnectionStatusText.Text = "Connection failed";
            ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
        }
    }

    private async Task<bool> CheckNetworkProfileExistsAsync(string networkName)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"wlan show profile name=\"{networkName}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        catch { }
        return false;
    }

    private async Task CreateNetworkProfileAsync(string networkName, string password)
    {
        try
        {
            // Create XML profile
            var profileXml = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{System.Security.SecurityElement.Escape(networkName)}</name>
    <SSIDConfig>
        <SSID>
            <name>{System.Security.SecurityElement.Escape(networkName)}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{System.Security.SecurityElement.Escape(password)}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>";

            // Save to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), $"wifi_profile_{Guid.NewGuid()}.xml");
            await File.WriteAllTextAsync(tempPath, profileXml);

            try
            {
                // Add the profile
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan add profile filename=\"{tempPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            finally
            {
                // Clean up temp file
                try { File.Delete(tempPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating network profile: {ex.Message}");
        }
    }

    private void DisconnectCurrentNetwork()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan disconnect",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(processInfo);
            
            // Update UI after a moment
            Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(UpdateCurrentConnection));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error disconnecting: {ex.Message}");
        }
    }

    private void UpdateCurrentConnection()
    {
        try
        {
            // Get current WiFi connection
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Parse SSID from output
                var ssidMatch = Regex.Match(output, @"^\s*SSID\s*:\s*(.+)$", RegexOptions.Multiline);
                var stateMatch = Regex.Match(output, @"^\s*State\s*:\s*(.+)$|^\s*Estado\s*:\s*(.+)$", RegexOptions.Multiline);

                var state = stateMatch.Success ? (stateMatch.Groups[1].Value + stateMatch.Groups[2].Value).Trim() : "";
                var isConnected = state.Contains("connected", StringComparison.OrdinalIgnoreCase) ||
                                  state.Contains("conectado", StringComparison.OrdinalIgnoreCase);

                if (ssidMatch.Success && isConnected)
                {
                    _currentNetworkName = ssidMatch.Groups[1].Value.Trim();
                    CurrentNetworkName.Text = _currentNetworkName;
                    CurrentNetworkType.Text = "WiFi • Connected";
                    ConnectionStatusText.Text = "Connected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    ConnectionIcon.Text = "📶";
                    DisconnectButton.Visibility = Visibility.Visible;
                }
                else
                {
                    _currentNetworkName = null;
                    CurrentNetworkName.Text = "Not Connected";
                    CurrentNetworkType.Text = "WiFi";
                    ConnectionStatusText.Text = "Disconnected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    ConnectionIcon.Text = "📵";
                    DisconnectButton.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating current connection: {ex.Message}");
        }
    }

    private void UpdateEthernetStatus()
    {
        try
        {
            var ethernetConnected = NetworkInterface.GetAllNetworkInterfaces()
                .Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                          ni.OperationalStatus == OperationalStatus.Up);

            if (ethernetConnected)
            {
                EthernetStatus.Background = new SolidColorBrush(Color.FromRgb(6, 78, 59)); // Green bg
                EthernetStatusText.Text = "Connected";
                EthernetStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
            }
            else
            {
                EthernetStatus.Background = new SolidColorBrush(Color.FromRgb(55, 65, 81)); // Gray bg
                EthernetStatusText.Text = "Not connected";
                EthernetStatusText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // Gray
            }
        }
        catch
        {
            EthernetStatus.Background = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            EthernetStatusText.Text = "Unknown";
            EthernetStatusText.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        }
    }
}

public class WiFiNetwork
{
    public string Name { get; set; } = string.Empty;
    public string SecurityType { get; set; } = "Unknown";
    public bool IsSecured { get; set; } = true;
    public string SignalStrength { get; set; } = "0%";
    public int SignalStrengthPercent { get; set; }
    public string SignalIcon { get; set; } = "📶";
}
