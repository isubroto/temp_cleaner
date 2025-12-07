using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace TempCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _userName = "";
        private string[] _nameParts = [];
        private DirFinder _dirFinder = new();
        private List<string> _fullList = new(4096);
        private Storyboard? _shimmerStoryboard;
        
        // Cached UI elements - avoid repeated FindName calls (expensive visual tree lookup)
        private Grid? _progressContainer;
        private Border? _progressFill;
        private Rectangle? _progressShimmer;
        private TextBlock? _storageSize;
        
        // Cached format strings
        private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];
        private static readonly StringBuilder _stringBuilder = new(256);
        
        // Throttle UI updates
        private long _lastUIUpdateTicks;
        private const long UIUpdateIntervalTicks = 500000; // 50ms in ticks

        public MainWindow()
        {
            InitializeComponent();
        }

        // Cache UI elements after window loads
        private void CacheUIElements()
        {
            _progressContainer ??= FindName("ProgressContainer") as Grid;
            _progressFill ??= FindName("ProgressFill") as Border;
            _progressShimmer ??= FindName("ProgressShimmer") as Rectangle;
            _storageSize ??= FindName("StorageSize") as TextBlock;
        }

        private async Task<GetInformations?> InitializeGitHubUpdaterAsync(string token)
        {
            return await GitHubUpdater.GetLatestReleaseVersionAsync("isubroto", "temp_cleaner", token).ConfigureAwait(false);
        }

        // Optimized environment variable reading with caching
        private static readonly Dictionary<string, string> _envCache = new(StringComparer.Ordinal);
        
        private static string GetEnv(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            if (_envCache.TryGetValue(key, out var cached))
                return cached;

            var val = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(val))
            {
                _envCache[key] = val;
                return val;
            }

            try
            {
                var envPaths = new []
                {
                    Path.Combine(AppContext.BaseDirectory, ".env"),
                    Path.Combine(Directory.GetCurrentDirectory(), ".env")
                };

                foreach (var envPath in envPaths)
                {
                    if (!File.Exists(envPath)) continue;
                    
                    foreach (var rawLine in File.ReadLines(envPath))
                    {
                        var line = rawLine.AsSpan().Trim();
                        if (line.Length == 0 || line[0] == '#') continue;
                        
                        if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                            line = line.Slice(7).Trim();

                        int sep = line.IndexOf('=');
                        if (sep <= 0) continue;
                        
                        var k = line.Slice(0, sep).Trim();
                        if (!k.SequenceEqual(key.AsSpan())) continue;

                        var v = line.Slice(sep + 1).Trim();
                        if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
                            v = v.Slice(1, v.Length - 2);
                        
                        var result = v.ToString();
                        _envCache[key] = result;
                        return result;
                    }
                }
            }
            catch { }

            _envCache[key] = string.Empty;
            return string.Empty;
        }

        public static AppSettings LoadSettings()
        {
            var settings = new AppSettings();
            
            var token = GetEnv("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(token)) settings.GitHub.Token = token;
            
            var owner = GetEnv("GITHUB_OWNER");
            if (!string.IsNullOrWhiteSpace(owner)) settings.GitHub.Owner = owner;
            
            var repo = GetEnv("GITHUB_REPO");
            if (!string.IsNullOrWhiteSpace(repo)) settings.GitHub.Repo = repo;

            return settings;
        }

        [DllImport("Shell32.dll", SetLastError = true)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, RecycleFlag dwFlags);

        [Flags]
        private enum RecycleFlag
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void mibimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            CacheUIElements();
            
            Clear.IsEnabled = false;
            Clear.Content = "🧽 Purify System";

            _userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            _nameParts = _userName.Split('\\');
            Check.Content = "🌊 Scanning Depths...";

            Logs.Items.Clear();
            Progress.Value = 0;
            _fullList.Clear();
            _dirFinder.Reset();
            
            countNum.Text = "0 Files";
            Percent.Text = "0%";
            UpdateStorageCard(0);
            SetProgressIndeterminate(true);

            // Build paths array (use string[] instead of ReadOnlySpan for async compatibility)
            string[] paths =
            [
                @"C:\Windows\Prefetch",
                @"C:\Windows\SoftwareDistribution\Download",
                @"C:\Windows\Temp",
                @"C:\Windows\System32\LogFiles",
                @"C:\Windows\Logs",
                @"C:\Windows\Debug",
                $@"C:\Users\{_nameParts[1]}\AppData\Local\Temp",
                $@"C:\Users\{_nameParts[1]}\AppData\Local\CrashDumps",
            ];

            var allItems = new List<string>(8192);
            message.Text = "🔍 Exploring the digital depths...";

            AddLog("🌊 Starting deep sea exploration...");
            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            await Task.Delay(200).ConfigureAwait(true);

            _lastUIUpdateTicks = DateTime.UtcNow.Ticks;

            // Scan each path
            foreach (var path in paths)
            {
                TrimLogs();
                
                message.Text = $"🔍 Scanning: {path}";
                AddLog($"📂 Exploring: {path}");

                var items = await Task.Run(() => _dirFinder.DirandFile(path, msg =>
                {
                    // Throttle UI updates to reduce dispatcher overhead
                    var now = DateTime.UtcNow.Ticks;
                    if (now - _lastUIUpdateTicks > UIUpdateIntervalTicks)
                    {
                        _lastUIUpdateTicks = now;
                        Dispatcher.InvokeAsync(() => countNum.Text = $"{msg.Count} Files", 
                            System.Windows.Threading.DispatcherPriority.Background);
                    }
                })).ConfigureAwait(true);

                if (items.Count > 0)
                {
                    allItems.AddRange(items);
                    AddLog($"✅ Found {items.Count} items in {Path.GetFileName(path)}");
                    countNum.Text = $"{allItems.Count} Files";
                }
                else
                {
                    AddLog($"✅ Completed: {path} (0 files)");
                }

                await Task.Delay(50).ConfigureAwait(true);
            }

            SetProgressIndeterminate(false);
            Check.Content = "📊 Processing...";
            message.Text = "🔬 Analyzing discovered debris...";

            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Efficient bulk copy instead of individual adds
            _fullList.AddRange(allItems);
            
            // Single progress update instead of loop
            SetProgressValue(100.0);
            Percent.Text = "100%";
            
            AddLog($"🎯 Scan complete - {allItems.Count} artifacts found");
            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            Check.Content = "✅ Scan Complete";
            countNum.Text = $"{allItems.Count} Files";
            message.Text = allItems.Count > 0 
                ? "🚀 Ready to purify the digital ocean depths" 
                : "🌊 The digital seas are crystal clear";
            
            Clear.IsEnabled = allItems.Count > 0;
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            CacheUIElements();
            
            Logs.Items.Clear();
            Clear.Content = "🌊 Purifying...";
            Clear.IsEnabled = false;
            
            int totalFiles = _fullList.Count;
            message.Text = "🧽 Beginning deep sea purification...";
            
            SetProgressValue(0);

            AddLog("🌊 Beginning purification...");
            AddLog($"🎯 Target: {totalFiles} artifacts");
            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            await Task.Delay(200).ConfigureAwait(true);

            // Process deletions in batches on background thread
            int processed = 0;
            long totalSizeDeleted = 0;
            _lastUIUpdateTicks = DateTime.UtcNow.Ticks;

            // Use parallel processing for file deletion with controlled degree
            var batchSize = 50;
            for (int i = 0; i < _fullList.Count; i += batchSize)
            {
                var batch = _fullList.Skip(i).Take(batchSize).ToList();
                
                var batchResult = await Task.Run(() =>
                {
                    long batchSize = 0;
                    int batchCount = 0;
                    
                    foreach (var filePath in batch)
                    {
                        try
                        {
                            if (File.Exists(filePath))
                            {
                                var len = new FileInfo(filePath).Length;
                                File.Delete(filePath);
                                batchSize += len;
                                batchCount++;
                            }
                            else if (Directory.Exists(filePath))
                            {
                                Directory.Delete(filePath, true);
                                batchCount++;
                            }
                        }
                        catch { }
                    }
                    
                    return (batchSize, batchCount);
                }).ConfigureAwait(true);

                totalSizeDeleted += batchResult.batchSize;
                processed += batch.Count;

                // Update UI once per batch
                double percent = (double)processed / totalFiles * 100.0;
                SetProgressValue(percent);
                Percent.Text = $"{percent:F0}%";
                message.Text = $"🌊 Purified {processed}/{totalFiles} ({FormatFileSize(totalSizeDeleted)})";
                UpdateStorageCard(totalSizeDeleted);
                
                TrimLogs();
                await Task.Yield(); // Allow UI to breathe
            }

            SetProgressValue(100.0);
            Percent.Text = "100%";
            UpdateStorageCard(totalSizeDeleted);

            // Empty recycle bin on background
            await Task.Run(() => SHEmptyRecycleBin(IntPtr.Zero, null, 
                RecycleFlag.SHERB_NOSOUND | RecycleFlag.SHERB_NOCONFIRMATION)).ConfigureAwait(true);

            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            AddLog("🎊 Purification complete!");
            AddLog($"💾 Space recovered: {FormatFileSize(totalSizeDeleted)}");
            AddLog($"📊 Files processed: {processed}");
            
            Clear.Content = "✨ Purified";
            Check.Content = "🔍 Deep Scan";
            message.Text = "🌟 The digital abyss sparkles with renewed clarity";
            
            _fullList.Clear();
            _fullList.TrimExcess();
            _dirFinder.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddLog(string msg)
        {
            Logs.Items.Add(msg);
            Logs.ScrollIntoView(Logs.Items[^1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TrimLogs()
        {
            while (Logs.Items.Count > 200)
                Logs.Items.RemoveAt(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetProgressValue(double percentage)
        {
            if (_progressContainer != null && _progressFill != null)
            {
                double containerWidth = _progressContainer.ActualWidth;
                if (containerWidth > 0)
                {
                    double maxFillWidth = containerWidth - 8;
                    _progressFill.Width = Math.Clamp(maxFillWidth * (percentage / 100.0), 0, maxFillWidth);
                }
            }
            
            Progress.Value = percentage;
        }

        private void SetProgressIndeterminate(bool isIndeterminate)
        {
            if (_progressFill == null || _progressShimmer == null) return;

            if (isIndeterminate)
            {
                _progressFill.Visibility = Visibility.Collapsed;
                _progressShimmer.Visibility = Visibility.Visible;
                
                double containerWidth = _progressContainer?.ActualWidth ?? 400;
                if (containerWidth <= 0) containerWidth = 400;
                
                _shimmerStoryboard?.Stop();
                _shimmerStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
                
                var animation = new DoubleAnimation
                {
                    From = -100,
                    To = containerWidth,
                    Duration = TimeSpan.FromSeconds(1.8)
                };
                
                Storyboard.SetTarget(animation, _progressShimmer);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                _shimmerStoryboard.Children.Add(animation);
                _shimmerStoryboard.Begin();
            }
            else
            {
                _shimmerStoryboard?.Stop();
                _shimmerStoryboard = null;
                
                _progressFill.Visibility = Visibility.Visible;
                _progressShimmer.Visibility = Visibility.Collapsed;
                SetProgressValue(0);
            }
            
            Progress.IsIndeterminate = isIndeterminate;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CacheUIElements();
            await Task.Yield();
            
            try
            {
                var settings = LoadSettings();
                var token = settings?.GitHub?.Token ?? "";
                
                var versionInfo = await InitializeGitHubUpdaterAsync(token);
                if (versionInfo == null) return;
                
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
                
                if (GitHubUpdater.CompareVersions(currentVersion, versionInfo.Version) < 0)
                {
                    var result = MessageBox.Show(
                        $"🌊 A new version is available!\n\nCurrent: {currentVersion}\nLatest: {versionInfo.Version.TrimStart('v')}\n\nUpdate now?",
                        "Update Available", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(versionInfo.Url))
                    {
                        GitHubUpdater.DownloadUpdateWithProgress(versionInfo.Url, CleanFileName(versionInfo.FileName), token);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check error: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetDirectorySize(string path)
        {
            try
            {
                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };
                
                long size = 0;
                foreach (var file in Directory.EnumerateFiles(path, "*", options))
                {
                    try { size += new FileInfo(file).Length; }
                    catch { }
                }
                return size;
            }
            catch { return 0; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            int order = 0;
            double len = bytes;
            
            while (len >= 1024 && order < SizeUnits.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {SizeUnits[order]}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateStorageCard(long totalBytes)
        {
            if (_storageSize != null)
                _storageSize.Text = FormatFileSize(totalBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string CleanFileName(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "TempCleaner-Setup.msi";
            
            var invalidChars = Path.GetInvalidFileNameChars();
            var span = fileName.AsSpan();
            
            _stringBuilder.Clear();
            foreach (var c in span)
            {
                _stringBuilder.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
            }
            
            var result = _stringBuilder.ToString().Trim().Replace(" ", "_");
            return string.IsNullOrEmpty(result) ? "TempCleaner-Setup.msi" : result;
        }
    }
}