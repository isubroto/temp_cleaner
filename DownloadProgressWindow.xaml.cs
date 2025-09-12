using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;

namespace TempCleaner
{
    public partial class DownloadProgressWindow : Window
    {
        private readonly string _downloadUrl;
        private readonly string _fileName;
        private readonly string? _githubToken;
        private string _downloadPath;
        private HttpClient? _httpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _downloadCompleted = false;

        public DownloadProgressWindow(string downloadUrl, string fileName, string? githubToken = null)
        {
            InitializeComponent();
            _downloadUrl = downloadUrl;
            _fileName = fileName;
            _githubToken = githubToken;
            
            _downloadPath = Path.Combine(Path.GetTempPath(), _fileName);
            
            FileNameText.Text = _fileName;
            Loaded += DownloadProgressWindow_Loaded;
            Closing += DownloadProgressWindow_Closing;
        }

        private async void DownloadProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await StartDownload();
        }

        private async Task StartDownload()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpClient = new HttpClient();
                
                // Add GitHub authentication for private repos
                if (!string.IsNullOrEmpty(_githubToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "TempCleaner");
                    _httpClient.DefaultRequestHeaders.Authorization =  new AuthenticationHeaderValue("Bearer", _githubToken);
                    _httpClient.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                    _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                }
                
                StatusText.Text = "🌊 Connecting to private repository...";
                
                // Add debug info for the URL
                System.Diagnostics.Debug.WriteLine($"Attempting to download: {_downloadUrl}");
                
                using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                
                // Check response status before proceeding
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = $"Download failed with status: {response.StatusCode} ({(int)response.StatusCode})\n\nURL: {_downloadUrl}";
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        errorMessage += "\n\n🔐 This appears to be a private repository.\nEnsure your GitHub token has:\n• 'repo' scope permissions\n• Access to the private repository\n• Valid authentication credentials";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        errorMessage += "\n\n🔐 Authentication failed for private repository.\nPlease check your GitHub token.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        errorMessage += "\n\n🔐 Access denied to private repository.\nYour token may lack proper permissions.";
                    }
                    
                    throw new HttpRequestException(errorMessage);
                }
                
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                TotalSizeText.Text = FormatFileSize(totalBytes);
                
                StatusText.Text = "📥 Downloading update";
                
                using var stream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);
                using var fileStream = new FileStream(_downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                long totalDownloaded = 0;
                var stopwatch = Stopwatch.StartNew();
                
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    if (bytesRead == 0) break;
                    
                    await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                    totalDownloaded += bytesRead;
                    
                    // Update UI every 100KB or every 100ms
                    if (totalDownloaded % (100 * 1024) == 0 || stopwatch.ElapsedMilliseconds > 100)
                    {
                        UpdateProgress(totalDownloaded, totalBytes, stopwatch.Elapsed);
                        stopwatch.Restart();
                    }
                }
                
                // Final update
                UpdateProgress(totalDownloaded, totalBytes, TimeSpan.Zero);
                
                _downloadCompleted = true;
                StatusText.Text = "✅ Private repository download completed! Ready to install.";
                CancelButton.Content = "🚪 Close";
                InstallButton.Visibility = Visibility.Visible;
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "❌ Download cancelled by user.";
                CancelButton.Content = "🚪 Close";
            }
            catch (HttpRequestException ex)
            {
                StatusText.Text = "🌊 Private repository access blocked!";
                string detailedError = ex.Message;
                
                // Provide more specific error messages for private repos
                if (ex.Message.Contains("404"))
                {
                    detailedError = "🔐 Private Repository Access Error (404)\n\nThe file cannot be accessed. This usually means:\n• Your GitHub token lacks 'repo' scope\n• Token doesn't have access to this private repository\n• The MSI file doesn't exist in the release\n• Authentication credentials are invalid";
                }
                else if (ex.Message.Contains("401"))
                {
                    detailedError = "🔐 Authentication Failed (401)\n\nYour GitHub token is invalid or expired.\nPlease update your token in appsettings.json";
                }
                else if (ex.Message.Contains("403"))
                {
                    detailedError = "🔐 Access Forbidden (403)\n\nYour token doesn't have permission to access this private repository.\nEnsure your token has 'repo' scope and repository access.";
                }
                
                MessageBox.Show($"🌊 Private Repository Download Failed\n\n{detailedError}\n\nURL: {_downloadUrl}", 
                    "Private Repository Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = "⚠️ Unknown sea creature blocked the private download!";
                MessageBox.Show($"🌊 Unexpected Private Repository Error\n\n{ex.Message}\n\nURL: {_downloadUrl}", 
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void UpdateProgress(long downloaded, long total, TimeSpan elapsed)
        {
            Dispatcher.Invoke(() =>
            {
                if (total > 0)
                {
                    var percentage = (double)downloaded / total * 100;
                    ProgressText.Text = $"{percentage:F1}%";
                    
                    // Update progress bar width
                    var progressContainer = ProgressFill.Parent as Border;
                    if (progressContainer != null)
                    {
                        progressContainer.UpdateLayout();
                        var containerWidth = progressContainer.ActualWidth - 2; // Account for borders
                        var fillWidth = containerWidth * (percentage / 100.0);
                        ProgressFill.Width = Math.Max(0, Math.Min(fillWidth, containerWidth));
                    }
                }
                
                DownloadedText.Text = FormatFileSize(downloaded);
                
                // Calculate speed
                if (elapsed.TotalSeconds > 0)
                {
                    var speed = downloaded / elapsed.TotalSeconds;
                    SpeedText.Text = FormatFileSize((long)speed) + "/s";
                }
            });
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadCompleted)
            {
                Close();
            }
            else
            {
                var result = MessageBox.Show("Are you sure you want to cancel the download?", "Cancel Download", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _cancellationTokenSource?.Cancel();
                    Close();
                }
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_downloadPath))
                {
                    StatusText.Text = "🚀 Launching installer...";
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _downloadPath,
                        UseShellExecute = true,
                        Verb = "runas" // Request admin privileges
                    };
                    
                    Process.Start(startInfo);
                    
                    // Close the application to allow update
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Downloaded file not found!", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start installer: {ex.Message}", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadProgressWindow_Closing(object sender, CancelEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _httpClient?.Dispose();
            
            // Clean up downloaded file if download wasn't completed
            if (!_downloadCompleted && File.Exists(_downloadPath))
            {
                try
                {
                    File.Delete(_downloadPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}