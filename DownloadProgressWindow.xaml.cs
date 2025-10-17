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
        
        // Speed calculation variables
        private DateTime _downloadStartTime;
        private long _totalBytesDownloaded = 0;

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
                
                // Initialize download timing
                _downloadStartTime = DateTime.Now;
                _totalBytesDownloaded = 0;
                
                using var stream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);
                using var fileStream = new FileStream(_downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                DateTime lastUpdateTime = DateTime.Now;
                
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    if (bytesRead == 0) break;
                    
                    await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                    _totalBytesDownloaded += bytesRead;
                    
                    // Update UI every 100KB or every 250ms for smoother updates
                    var currentTime = DateTime.Now;
                    if (_totalBytesDownloaded % (100 * 1024) == 0 || 
                        (currentTime - lastUpdateTime).TotalMilliseconds > 250)
                    {
                        UpdateProgress(_totalBytesDownloaded, totalBytes);
                        lastUpdateTime = currentTime;
                    }
                }
                
                // Final update
                UpdateProgress(_totalBytesDownloaded, totalBytes);
                
                _downloadCompleted = true;
                StatusText.Text = "✅ Download completed! Ready to install.";
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

        private void UpdateProgress(long downloaded, long total)
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
                
                // Calculate speed based on total elapsed time
                var totalElapsed = DateTime.Now - _downloadStartTime;
                if (totalElapsed.TotalSeconds > 0.5) // Only calculate after 500ms to avoid initial spikes
                {
                    var bytesPerSecond = downloaded / totalElapsed.TotalSeconds;
                    
                    // Debug information
                    System.Diagnostics.Debug.WriteLine($"Downloaded: {downloaded} bytes");
                    System.Diagnostics.Debug.WriteLine($"Elapsed: {totalElapsed.TotalSeconds:F2} seconds");
                    System.Diagnostics.Debug.WriteLine($"Bytes per second: {bytesPerSecond:F2}");
                    System.Diagnostics.Debug.WriteLine($"Formatted speed: {FormatSpeed(bytesPerSecond)}");
                    
                    SpeedText.Text = FormatSpeed(bytesPerSecond);
                }
                else
                {
                    SpeedText.Text = "-- B/s";
                }
            });
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond == 0) return "0 B/s";
            
            string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
            double speed = bytesPerSecond;
            int order = 0;
            
            System.Diagnostics.Debug.WriteLine($"FormatSpeed input: {bytesPerSecond:F2} bytes/second");
            
            while (speed >= 1024 && order < sizes.Length - 1)
            {
                order++;
                speed = speed / 1024;
                System.Diagnostics.Debug.WriteLine($"Converted to order {order}: {speed:F2} {sizes[order]}");
            }

            var result = $"{speed:0.##} {sizes[order]}";
            System.Diagnostics.Debug.WriteLine($"Final result: {result}");
            return result;
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

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "🚀 Launching installer...";
                
                // Debug: Show the actual file path and verify file exists
                System.Diagnostics.Debug.WriteLine($"Installing MSI from: {_downloadPath}");
                System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(_downloadPath)}");
                
                if (File.Exists(_downloadPath))
                {
                    var fileInfo = new FileInfo(_downloadPath);
                    System.Diagnostics.Debug.WriteLine($"File size: {fileInfo.Length} bytes");
                    System.Diagnostics.Debug.WriteLine($"File name: {fileInfo.Name}");
                    
                    bool installationStarted = false;
                    
                    // Method 1: Use msiexec.exe to properly install MSI files
                    if (!installationStarted)
                    {
                        try
                        {
                            var msiExecStartInfo = new ProcessStartInfo
                            {
                                FileName = "msiexec.exe",
                                Arguments = $"/i \"{_downloadPath}\"", // Interactive installation
                                UseShellExecute = true,
                                Verb = "runas" // Request admin privileges
                            };
                            
                            System.Diagnostics.Debug.WriteLine($"Trying msiexec with: {msiExecStartInfo.Arguments}");
                            Process.Start(msiExecStartInfo);
                            installationStarted = true;
                            
                            // Close the application after starting installer
                            Task.Delay(2000).ContinueWith(_ => 
                            {
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            });
                        }
                        catch (Exception msiExecEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"msiexec method failed: {msiExecEx.Message}");
                        }
                    }
                    
                    // Method 2: Try direct file execution with shell
                    if (!installationStarted)
                    {
                        try
                        {
                            var directStartInfo = new ProcessStartInfo
                            {
                                FileName = _downloadPath,
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                            
                            System.Diagnostics.Debug.WriteLine("Trying direct execution");
                            Process.Start(directStartInfo);
                            installationStarted = true;
                            
                            Task.Delay(2000).ContinueWith(_ => 
                            {
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            });
                        }
                        catch (Exception directEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Direct execution failed: {directEx.Message}");
                        }
                    }
                    
                    // Method 3: Try using PowerShell to start the MSI
                    if (!installationStarted)
                    {
                        try
                        {
                            var powershellStartInfo = new ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-Command \"Start-Process -FilePath '{_downloadPath}' -Verb RunAs\"",
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
                            
                            System.Diagnostics.Debug.WriteLine("Trying PowerShell execution");
                            Process.Start(powershellStartInfo);
                            installationStarted = true;
                            
                            Task.Delay(2000).ContinueWith(_ => 
                            {
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            });
                        }
                        catch (Exception powershellEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"PowerShell method failed: {powershellEx.Message}");
                        }
                    }
                    
                    // Method 4: Try using cmd to execute the MSI
                    if (!installationStarted)
                    {
                        try
                        {
                            var cmdStartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c start \"\" \"{_downloadPath}\"",
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                Verb = "runas"
                            };
                            
                            System.Diagnostics.Debug.WriteLine("Trying CMD execution");
                            Process.Start(cmdStartInfo);
                            installationStarted = true;
                            
                            Task.Delay(2000).ContinueWith(_ => 
                            {
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            });
                        }
                        catch (Exception cmdEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"CMD method failed: {cmdEx.Message}");
                        }
                    }
                    
                    // Method 5: Manual installation guidance
                    if (!installationStarted)
                    {
                        try
                        {
                            var explorerStartInfo = new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = $"/select,\"{_downloadPath}\"",
                                UseShellExecute = false
                            };
                            
                            Process.Start(explorerStartInfo);
                            
                            MessageBox.Show($"🌊 Automatic installation failed. The installer has been located for you.\n\n📁 File: {Path.GetFileName(_downloadPath)}\n📍 Location: {Path.GetDirectoryName(_downloadPath)}\n\n🚀 Please double-click the highlighted MSI file to install.\n\nNote: You may need to right-click and select \"Run as administrator\"", 
                                "Manual Installation Required", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception explorerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Explorer method failed: {explorerEx.Message}");
                            
                            // Final fallback: Just show the path
                            MessageBox.Show($"🌊 Please install manually.\n\n📁 Downloaded file: {_downloadPath}\n\n🚀 Navigate to this location and double-click the MSI file to install.\n\nNote: You may need to right-click and select \"Run as administrator\"", 
                                "Manual Installation Required", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"🌊 Downloaded file not found!\n\n📍 Expected location: {_downloadPath}\n\nPlease check if the download completed successfully.", "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Install button error: {ex.Message}");
                MessageBox.Show($"🌊 Installation Error\n\n{ex.Message}\n\n📁 File location: {_downloadPath}\n\n🚀 Please try running the installer manually.", 
                    "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
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