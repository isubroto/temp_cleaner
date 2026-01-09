using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace TempCleaner
{
    public partial class DownloadProgressWindow : Window
    {
        private readonly string _downloadUrl;
        private readonly string _fileName;
        private readonly string? _githubToken;
        private readonly string _downloadPath;
        private HttpClient? _httpClient;
        private CancellationTokenSource? _cts;
        private bool _downloadCompleted;
        
        // Speed calculation
        private long _downloadStartTicks;
        private long _totalBytesDownloaded;
        
        // Cached format strings
        private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];
        private static readonly string[] SpeedUnits = ["B/s", "KB/s", "MB/s", "GB/s"];

        public DownloadProgressWindow(string downloadUrl, string fileName, string? githubToken = null)
        {
            InitializeComponent();
            _downloadUrl = downloadUrl;
            _fileName = fileName;
            _githubToken = githubToken;
            _downloadPath = Path.Combine(Path.GetTempPath(), _fileName);
            
            FileNameText.Text = _fileName;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) => await StartDownloadAsync();

        private async Task StartDownloadAsync()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
                
                if (!string.IsNullOrEmpty(_githubToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "TempCleaner");
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
                    _httpClient.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                    _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                }
                
                StatusText.Text = "🌊 Connecting...";
                
                using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Download failed: {response.StatusCode}");
                }
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                TotalSizeText.Text = FormatSize(totalBytes);
                StatusText.Text = "📥 Downloading...";
                
                _downloadStartTicks = Stopwatch.GetTimestamp();
                _totalBytesDownloaded = 0;
                
                await using var contentStream = await response.Content.ReadAsStreamAsync(_cts.Token);
                await using var fileStream = new FileStream(_downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                
                // Use larger buffer for better throughput
                var buffer = new byte[81920];
                long lastUpdateBytes = 0;
                var lastUpdateTime = Stopwatch.GetTimestamp();
                var ticksPerSecond = Stopwatch.Frequency;
                
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(), _cts.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cts.Token);
                    _totalBytesDownloaded += bytesRead;
                    
                    // Update UI every 200ms or 256KB
                    var currentTicks = Stopwatch.GetTimestamp();
                    if (_totalBytesDownloaded - lastUpdateBytes >= 262144 || 
                        currentTicks - lastUpdateTime >= ticksPerSecond / 5)
                    {
                        UpdateProgressUI(totalBytes, currentTicks);
                        lastUpdateBytes = _totalBytesDownloaded;
                        lastUpdateTime = currentTicks;
                    }
                }
                
                // Final update
                UpdateProgressUI(totalBytes, Stopwatch.GetTimestamp());
                
                _downloadCompleted = true;
                StatusText.Text = "✅ Download complete!";
                CancelButton.Content = "🚪 Close";
                InstallButton.Visibility = Visibility.Visible;
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "❌ Cancelled";
                CancelButton.Content = "🚪 Close";
            }
            catch (Exception ex)
            {
                StatusText.Text = "⚠️ Download failed";
                CustomMessageBox.Show(this, 
                    $"Download error occurred:\n\n{ex.Message}\n\nPlease check your internet connection and try again.", 
                    "Download Failed", 
                    CustomMessageBox.MessageBoxButton.OK, 
                    CustomMessageBox.MessageBoxImage.Error);
                Close();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateProgressUI(long totalBytes, long currentTicks)
        {
            var percentage = totalBytes > 0 ? (double)_totalBytesDownloaded / totalBytes * 100 : 0;
            
            ProgressText.Text = $"{percentage:F1}%";
            DownloadedText.Text = FormatSize(_totalBytesDownloaded);
            
            // Update progress bar
            if (ProgressFill.Parent is Border container && container.ActualWidth > 2)
            {
                ProgressFill.Width = Math.Max(0, (container.ActualWidth - 2) * (percentage / 100.0));
            }
            
            // Calculate speed
            var elapsedTicks = currentTicks - _downloadStartTicks;
            if (elapsedTicks > Stopwatch.Frequency / 2) // After 500ms
            {
                var bytesPerSecond = _totalBytesDownloaded * Stopwatch.Frequency / elapsedTicks;
                SpeedText.Text = FormatSpeed(bytesPerSecond);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < SizeUnits.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {SizeUnits[order]}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0) return "0 B/s";
            
            int order = 0;
            while (bytesPerSecond >= 1024 && order < SpeedUnits.Length - 1)
            {
                order++;
                bytesPerSecond /= 1024;
            }
            return $"{bytesPerSecond:0.#} {SpeedUnits[order]}";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadCompleted)
            {
                Close();
                return;
            }
            
            var result = CustomMessageBox.Show(this, 
                "Are you sure you want to cancel the download?", 
                "Confirm Cancellation", 
                CustomMessageBox.MessageBoxButton.YesNo, 
                CustomMessageBox.MessageBoxImage.Question);
            
            if (result == CustomMessageBox.MessageBoxResult.Yes)
            {
                _cts?.Cancel();
                Close();
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_downloadPath))
            {
                CustomMessageBox.Show(this, 
                    "The downloaded file was not found!\n\nPlease try downloading again.", 
                    "File Not Found", 
                    CustomMessageBox.MessageBoxButton.OK, 
                    CustomMessageBox.MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{_downloadPath}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                });
                
                _ = Task.Delay(1500).ContinueWith(_ => Dispatcher.Invoke(() => Application.Current.Shutdown()));
            }
            catch
            {
                // Fallback: open file location
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{_downloadPath}\"");
                    CustomMessageBox.Show(this, 
                        $"Unable to launch the installer automatically.\n\nPlease install manually from:\n\n{_downloadPath}", 
                        "Manual Installation Required", 
                        CustomMessageBox.MessageBoxButton.OK, 
                        CustomMessageBox.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(this, 
                        $"Failed to launch installer:\n\n{ex.Message}\n\nFile location:\n{_downloadPath}", 
                        "Installation Error", 
                        CustomMessageBox.MessageBoxButton.OK, 
                        CustomMessageBox.MessageBoxImage.Error);
                }
            }
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _httpClient?.Dispose();
            
            if (!_downloadCompleted && File.Exists(_downloadPath))
            {
                try { File.Delete(_downloadPath); } catch { }
            }
        }
    }
}