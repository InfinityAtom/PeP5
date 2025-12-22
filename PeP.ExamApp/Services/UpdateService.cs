using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PeP.ExamApp.Services;

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsMandatory { get; set; }
    public string MinimumVersion { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _updateCheckUrl;
    private readonly string _appDataPath;
    
    public static string CurrentVersion => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
        ?? "1.0.0";

    public UpdateService(string updateServerBaseUrl)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _updateCheckUrl = $"{updateServerBaseUrl.TrimEnd('/')}/api/examapp/update";
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PeP.ExamApp");
        
        Directory.CreateDirectory(_appDataPath);
    }

    /// <summary>
    /// Check if an update is available
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_updateCheckUrl}?currentVersion={CurrentVersion}");
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (updateInfo != null && IsNewerVersion(updateInfo.Version, CurrentVersion))
            {
                return updateInfo;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Download the update installer
    /// </summary>
    public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        try
        {
            var installerPath = Path.Combine(_appDataPath, "Updates", $"PeP.ExamApp_Setup_{updateInfo.Version}.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(installerPath)!);

            // If already downloaded, verify and return
            if (File.Exists(installerPath))
            {
                if (await VerifyChecksumAsync(installerPath, updateInfo.Checksum))
                {
                    return installerPath;
                }
                File.Delete(installerPath);
            }

            using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var downloadedBytes = 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    progress?.Report((int)(downloadedBytes * 100 / totalBytes));
                }
            }

            // Verify checksum
            if (!string.IsNullOrEmpty(updateInfo.Checksum))
            {
                if (!await VerifyChecksumAsync(installerPath, updateInfo.Checksum))
                {
                    File.Delete(installerPath);
                    throw new Exception("Downloaded file checksum verification failed");
                }
            }

            return installerPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update download failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Install the update (launches installer and closes app)
    /// </summary>
    public void InstallUpdate(string installerPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SILENT /RESTARTAPPLICATIONS",
                UseShellExecute = true
            };

            Process.Start(startInfo);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start installer: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Show update dialog and handle user choice
    /// </summary>
    public async Task<bool> PromptAndInstallUpdateAsync(UpdateInfo updateInfo)
    {
        var message = $"A new version of PeP Exam App is available!\n\n" +
                      $"Current Version: {CurrentVersion}\n" +
                      $"New Version: {updateInfo.Version}\n" +
                      $"Released: {updateInfo.ReleaseDate:MMMM dd, yyyy}\n\n" +
                      $"{updateInfo.ReleaseNotes}\n\n" +
                      (updateInfo.IsMandatory 
                          ? "This update is required to continue using the application." 
                          : "Would you like to download and install the update now?");

        var buttons = updateInfo.IsMandatory ? MessageBoxButton.OK : MessageBoxButton.YesNo;
        var result = MessageBox.Show(message, "Update Available", buttons, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes || result == MessageBoxResult.OK || updateInfo.IsMandatory)
        {
            var progress = new Progress<int>(percent =>
            {
                // Could update a progress bar here
                Debug.WriteLine($"Download progress: {percent}%");
            });

            var installerPath = await DownloadUpdateAsync(updateInfo, progress);
            
            if (!string.IsNullOrEmpty(installerPath))
            {
                InstallUpdate(installerPath);
                return true;
            }
            else
            {
                MessageBox.Show(
                    "Failed to download the update. Please try again later or download manually from the website.",
                    "Download Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        return false;
    }

    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            var newVer = new Version(newVersion.Split('-')[0]);
            var curVer = new Version(currentVersion.Split('-')[0]);
            return newVer > curVer;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum)
    {
        if (string.IsNullOrEmpty(expectedChecksum)) return true;

        try
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            var actualChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clean up old update files
    /// </summary>
    public void CleanupOldUpdates()
    {
        try
        {
            var updatesPath = Path.Combine(_appDataPath, "Updates");
            if (Directory.Exists(updatesPath))
            {
                foreach (var file in Directory.GetFiles(updatesPath, "*.exe"))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < DateTime.Now.AddDays(-7))
                        {
                            File.Delete(file);
                        }
                    }
                    catch { /* Ignore individual file deletion errors */ }
                }
            }
        }
        catch { /* Ignore cleanup errors */ }
    }
}
