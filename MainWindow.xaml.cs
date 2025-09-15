using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

// To fix the CS0246 error, you need to ensure that the Squirrel NuGet package is installed in your project.  
// Follow these steps:  
// 1. Open the NuGet Package Manager in Visual Studio.  
// 2. Search for "Squirrel.Windows" and install it.  
// 3. After installation, the error should be resolved.  

// If the package is already installed but the error persists, ensure the project file includes the reference to Squirrel.Windows.  
// You can also try cleaning and rebuilding the solution.  

// No code changes are required in this file to fix the error.
namespace TempCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        private string UserName = "";
        private string[] name = [];
        private DirFinder Dirfinder = new DirFinder();
        private List<string> FullList = new List<string>();


        public MainWindow()
        {
            InitializeComponent();
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        private async Task<GetInformations> InitializeGitHubUpdaterAsync(string token)
        {
            var latest = await GitHubUpdater.GetLatestReleaseVersionAsync("isubroto", "temp_cleaner", token);
            return latest; // Ensure this method returns a string
        }
        public static AppSettings LoadSettings()
        {
            var json = File.ReadAllText("./appsettings.json");
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings;
        }

        [DllImport("Shell32.dll", SetLastError = true)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlag dwFlags);

        enum RecycleFlag : int

        {

            SHERB_NOCONFIRMATION = 0x00000001, // No confirmation, when emptying

            SHERB_NOPROGRESSUI = 0x00000001, // No progress tracking window during the emptyting of the recycle bin

            SHERB_NOSOUND = 0x00000004 // No sound when the emptyting of the recycle bin is complete
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void mibimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            // Complete UI reset
            Clear.IsEnabled = false;
            Clear.Content = "🧽 Purify System";

            UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            name = UserName.Split('\\');
            Check.Content = "🌊 Scanning Depths...";

            // Reset all UI elements
            Logs.Items.Clear();
            Progress.Value = 0;
            FullList.Clear();
            Dirfinder.count = 0;
            
            // Reset stats
            countNum.Text = "0 Files";
            Percent.Text = "0%";
            UpdateStorageCard(0); // Reset storage size
            
            // Set indeterminate state
            SetProgressIndeterminate(true);

            // Comprehensive temporary file paths
            string[] paths = new[]
            {
                // Windows System Temporary Files
                @"C:\Windows\Prefetch",
                @"C:\Windows\SoftwareDistribution\Download",
                @"C:\Windows\Temp",
                @"C:\Windows\System32\LogFiles",
                @"C:\Windows\Logs",
                @"C:\Windows\Debug",
    
                
                // User Profile Temporary Files
                $@"C:\Users\{name[1]}\AppData\Local\Temp",
                $@"C:\Users\{name[1]}\AppData\Local\CrashDumps",
            };

            List<string> allItems = new();
            message.Text = "🔍 Exploring the digital depths...";

            // Add initial log entry - Test to see if logs work
            Logs.Items.Add("🌊 Starting deep sea exploration...");
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.UpdateLayout();
            await Task.Delay(500); // Allow UI to update

            int discoveredCount = 0;
            
            // Scan each path sequentially and update UI immediately
            foreach (string path in paths)
            {
                // Update UI immediately for each directory
                message.Text = $"🔍 Scanning: {path}";
                Logs.Items.Add($"📂 Exploring: {path}");
                Logs.UpdateLayout();
                Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
                await Task.Delay(100);

                // Scan directory in background but update UI on main thread
                await Task.Run(() =>
                {
                    var items = Dirfinder.DirandFile(path, msg =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            countNum.Text = $"{msg.Count} Files";
                            discoveredCount = msg.Count;
                        });
                    });

                    if (items != null && items.Count > 0)
                    {
                        // Add files to main list
                        allItems.AddRange(items);

                        // Update UI with found files
                        Dispatcher.Invoke(() =>
                        {
                            foreach (var item in items.Take(5)) // Show first 5 files as examples
                            {
                                Logs.Items.Add($"🔍 Found: {System.IO.Path.GetFileName(item)}");
                            }
                            
                            if (items.Count > 5)
                            {
                                Logs.Items.Add($"... and {items.Count - 5} more files");
                            }
                            
                            Logs.Items.Add($"✅ Completed: {path} ({items.Count} files)");
                            Logs.UpdateLayout();
                            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
                            
                            countNum.Text = $"{allItems.Count} Files";
                            discoveredCount = allItems.Count;
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Logs.Items.Add($"✅ Completed: {path} (0 files)");
                            Logs.UpdateLayout();
                            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
                        });
                    }
                });

                await Task.Delay(200); // Small delay between directories
            }

            // Switch to determinate progress for processing phase
            SetProgressIndeterminate(false);
            
            Check.Content = "📊 Processing Discoveries...";
            message.Text = "🔬 Analyzing discovered debris...";

            // Add separator
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.Items.Add("🔬 Starting analysis phase...");
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.UpdateLayout();
            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);

            // Processing phase - add files to FullList with progress updates
            int cnt = 0;
            foreach (var item in allItems)
            {
                FullList.Add(item);
                cnt++;
                
                // Update progress every 50 items
                if (cnt % 50 == 0 || cnt == allItems.Count)
                {
                    double progressPercent = allItems.Count > 0 ? (double)cnt / allItems.Count * 100.0 : 100.0;
                    SetProgressValue(progressPercent);
                    Percent.Text = $"{Math.Round(progressPercent, 1)}%";
                    message.Text = $"📊 Analyzed {cnt}/{allItems.Count} deep sea artifacts";
                    
                    Logs.Items.Add($"📋 Processed {cnt}/{allItems.Count} artifacts ({progressPercent:F1}%)");
                    Logs.UpdateLayout();
                    Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
                    
                    await Task.Delay(10);
                }
            }

            // Final completion
            SetProgressValue(100.0);
            Percent.Text = "100%";
            
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.Items.Add($"🎯 Deep scan complete - {allItems.Count} artifacts ready for purification");
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.UpdateLayout();
            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);

            Check.Content = "✅ Scan Complete";
            countNum.Text = $"{allItems.Count} Files";
            message.Text = $"🎯 Deep scan complete - {allItems.Count} artifacts ready for purification";

            if (allItems.Count > 0)
            {
                Clear.IsEnabled = true;
                message.Text = "🚀 Ready to purify the digital ocean depths";
            }
            else
            {
                message.Text = "🌊 The digital seas are crystal clear - no debris found";
            }
        }


        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Logs.Items.Clear();
            Clear.Content = "🌊 Purifying...";
            Clear.IsEnabled = false;
            
            int processed = 0;
            int totalFiles = FullList.Count;
            long totalSizeDeleted = 0;
            message.Text = "🧽 Beginning deep sea purification...";
            
            SetProgressValue(0);

            // Add initial logs
            Logs.Items.Add("🌊 Beginning deep sea purification ritual...");
            Logs.Items.Add($"🎯 Target: {totalFiles} digital artifacts");
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.UpdateLayout();
            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
            await Task.Delay(500);

            foreach (var filePath in FullList)
            {
                string logMessage;
                long fileSize = 0;
                
                try
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        fileSize = fileInfo.Length;
                        
                        File.Delete(filePath);
                        totalSizeDeleted += fileSize;
                        logMessage = $"🗑️ Dissolved: {System.IO.Path.GetFileName(filePath)} ({FormatFileSize(fileSize)})";
                    }
                    else if (Directory.Exists(filePath))
                    {
                        fileSize = GetDirectorySize(filePath);
                        
                        Directory.Delete(filePath, true);
                        totalSizeDeleted += fileSize;
                        logMessage = $"🧹 Cleared cavern: {System.IO.Path.GetFileName(filePath)} ({FormatFileSize(fileSize)})";
                    }
                    else
                    {
                        logMessage = $"👻 Phantom artifact: {System.IO.Path.GetFileName(filePath)}";
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    logMessage = $"🔒 Guardian protected: {System.IO.Path.GetFileName(filePath)}";
                }
                catch (DirectoryNotFoundException)
                {
                    logMessage = $"🌫️ Vanished into depths: {System.IO.Path.GetFileName(filePath)}";
                }
                catch (IOException)
                {
                    logMessage = $"🌊 Turbulent waters: {System.IO.Path.GetFileName(filePath)}";
                }
                catch (Exception)
                {
                    logMessage = $"⚠️ Sea creature blocked: {System.IO.Path.GetFileName(filePath)}";
                }

                processed++;
                
                // Add log immediately
                Logs.Items.Add(logMessage);
                
                // Update progress every few items
                if (processed % 5 == 0 || processed == totalFiles)
                {
                    double progressPercent = totalFiles > 0 ? (double)processed / totalFiles * 100.0 : 100.0;
                    SetProgressValue(progressPercent);
                    Percent.Text = $"{Math.Round(progressPercent, 1)}%";
                    message.Text = $"🌊 Purified {processed}/{totalFiles} artifacts ({FormatFileSize(totalSizeDeleted)} recovered)";
                    
                    UpdateStorageCard(totalSizeDeleted);
                    
                    Logs.UpdateLayout();
                    Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
                    
                    await Task.Delay(20);
                }
            }

            // Final updates
            SetProgressValue(100.0);
            Percent.Text = "100%";
            UpdateStorageCard(totalSizeDeleted);

            // Empty recycle bin
            SHEmptyRecycleBin(IntPtr.Zero, string.Empty, RecycleFlag.SHERB_NOSOUND | RecycleFlag.SHERB_NOCONFIRMATION);

            // Final completion logs
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Logs.Items.Add("🎊 The digital seas are now crystal clear!");
            Logs.Items.Add($"💾 Total space recovered: {FormatFileSize(totalSizeDeleted)}");
            Logs.Items.Add($"📊 Files processed: {processed}/{totalFiles}");
            Logs.Items.Add("♻️ Recycle bin depths have been purged");
            Logs.Items.Add("🌊 Deep sea purification ritual complete");
            Logs.Items.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            Logs.UpdateLayout();
            Logs.ScrollIntoView(Logs.Items[Logs.Items.Count - 1]);
            
            Clear.Content = "✨ Purified";
            Check.Content = "🔍 Deep Scan";
            message.Text = "🌟 The digital abyss sparkles with renewed clarity";
        }

        // Custom progress bar control methods
        private void SetProgressValue(double percentage)
        {
            var progressContainer = FindName("ProgressContainer") as Grid;
            var progressFill = FindName("ProgressFill") as Border;
            
            if (progressContainer != null && progressFill != null)
            {
                // Ensure we update layout first to get correct ActualWidth
                progressContainer.UpdateLayout();
                
                double containerWidth = progressContainer.ActualWidth;
                if (containerWidth > 0)
                {
                    // Calculate fill width with proper bounds checking
                    double maxFillWidth = containerWidth - 8; // Account for margins and borders (2px border + 2px margin on each side)
                    double fillWidth = maxFillWidth * (percentage / 100.0);
                    
                    // Ensure width doesn't exceed container or go negative
                    fillWidth = Math.Max(0, Math.Min(fillWidth, maxFillWidth));
                    progressFill.Width = fillWidth;
                }
                else
                {
                    // If ActualWidth is 0, set a minimum width based on percentage
                    progressFill.Width = percentage * 2; // Fallback calculation
                }
            }
            
            Progress.Value = percentage;
        }

        private void SetProgressIndeterminate(bool isIndeterminate)
        {
            var progressFill = FindName("ProgressFill") as Border;
            var progressShimmer = FindName("ProgressShimmer") as Rectangle;
            var progressContainer = FindName("ProgressContainer") as Grid;
            
            if (progressFill != null && progressShimmer != null)
            {
                if (isIndeterminate)
                {
                    progressFill.Visibility = Visibility.Collapsed;
                    progressShimmer.Visibility = Visibility.Visible;
                    
                    // Get container width to calculate proper animation bounds
                    double containerWidth = 400; // Default fallback width
                    if (progressContainer != null)
                    {
                        progressContainer.UpdateLayout();
                        if (progressContainer.ActualWidth > 0)
                        {
                            containerWidth = progressContainer.ActualWidth - 4; // Account for margins
                        }
                    }
                    
                    // Calculate proper animation bounds
                    double shimmerWidth = 100; // Width of shimmer rectangle
                    double startX = -shimmerWidth; // Start completely off-screen to the left
                    double endX = containerWidth; // End completely off-screen to the right
                    
                    // Start shimmer animation with proper bounds
                    var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
                    var animation = new DoubleAnimation
                    {
                        From = startX,
                        To = endX,
                        Duration = TimeSpan.FromSeconds(1.8)
                    };
                    Storyboard.SetTarget(animation, progressShimmer);
                    Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    storyboard.Children.Add(animation);
                    storyboard.Begin();
                }
                else
                {
                    progressFill.Visibility = Visibility.Visible;
                    progressShimmer.Visibility = Visibility.Collapsed;
                    
                    // Reset progress to 0 when switching to determinate
                    SetProgressValue(0);
                }
            }
            
            Progress.IsIndeterminate = isIndeterminate;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure layout is updated for proper progress bar calculations
            var progressContainer = FindName("ProgressContainer") as Grid;
            if (progressContainer != null)
            {
                progressContainer.UpdateLayout();
            }
            
            await Task.Yield();
            try
            {
                var settings = LoadSettings();
                string githubToken = settings?.GitHub?.Token ?? "";
                
                var versionInfo = await InitializeGitHubUpdaterAsync(githubToken);
                if (versionInfo == null)
                {
                    return;
                }   
                
                var result = GitHubUpdater.CompareVersions(Assembly.GetExecutingAssembly().GetName().Version.ToString(), versionInfo.Version);
                if (result < 0)
                {
                    MessageBoxResult res = MessageBox.Show($@"🌊 A new deep sea adventure awaits!{Environment.NewLine}{Environment.NewLine}Current Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}{Environment.NewLine}Latest Version: {versionInfo.Version.ToString().Replace("v","")}{Environment.NewLine}{Environment.NewLine}Dive deeper with the latest DeepCleaner Pro features?", "DeepCleaner Pro Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (res == MessageBoxResult.Yes)
                    {
                        // Direct download from versionInfo.Url with GitHub token for private repo access
                        if (!string.IsNullOrEmpty(versionInfo.Url))
                        {
                            string fileName = versionInfo.FileName;
                            // Clean and validate the filename
                            fileName = CleanFileName(fileName);

                            // Direct download with GitHub token for private repository authentication
                            GitHubUpdater.DownloadUpdateWithProgress(versionInfo.Url, fileName, githubToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check error: {ex.Message}");
            }
        }

        // Helper method to get directory size
        private long GetDirectorySize(string directoryPath)
        {
            try
            {
                var directory = new DirectoryInfo(directoryPath);
                return directory.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Where(file => !file.Attributes.HasFlag(FileAttributes.ReparsePoint)) // Skip junction points
                    .Sum(file => 
                    {
                        try { return file.Length; }
                        catch { return 0; } // Skip files we can't access
                    });
            }
            catch
            {
                return 0;
            }
        }

        // Helper method to format file sizes
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

        // Helper method to update storage card
        private void UpdateStorageCard(long totalBytes)
        {
            var storageText = FindName("StorageSize") as TextBlock;
            if (storageText != null)
            {
                storageText.Text = FormatFileSize(totalBytes);
            }
        }

        // Helper method to clean filename
        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "TempCleaner-Setup.msi";
            
            // Remove invalid characters from filename
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string cleanName = fileName;
            
            foreach (char c in invalidChars)
                cleanName = cleanName.Replace(c, '_');
            
            // Remove any extra spaces and replace with underscores
            cleanName = cleanName.Trim().Replace(" ", "_");
            
            return string.IsNullOrEmpty(cleanName) ? "TempCleaner-Setup.msi" : cleanName;
        }
    }
}