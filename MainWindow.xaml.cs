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
    public partial class MainWindow : Window
    {
        private string _userName = "";
        private string[] _nameParts = [];
        private DirFinder _dirFinder = new();
        private List<string> _fullList = new(4096);
        private Storyboard? _shimmerStoryboard;

        // Cached UI elements
        private Grid? _progressContainer;
        private Border? _progressFill;
        private Rectangle? _progressShimmer;
        private TextBlock? _storageSize;

        private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];
        private static readonly StringBuilder _stringBuilder = new(256);

        private long _lastUIUpdateTicks;
        private const long UIUpdateIntervalTicks = 500000;

        public MainWindow()
        {
            InitializeComponent();
        }

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
                var envPaths = new[]
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

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Handle both TextBlock (from new menu) and Button (legacy) clicks
                string originalContent = "⟳ Updates";
                bool isTextBlock = sender is TextBlock;

                if (isTextBlock)
                {
                    var textBlock = (TextBlock)sender;
                    originalContent = textBlock.Text;
                    textBlock.Text = "Checking...";
                    textBlock.IsEnabled = false;
                }

                var settings = LoadSettings();
                var token = settings?.GitHub?.Token ?? "";

                var versionInfo = await InitializeGitHubUpdaterAsync(token);

                if (isTextBlock)
                {
                    var textBlock = (TextBlock)sender;
                    textBlock.Text = originalContent;
                    textBlock.IsEnabled = true;
                }

                if (versionInfo == null)
                {
                    CustomMessageBox.Show(this,
                        "Unable to check for updates. Please check your internet connection and try again.",
                        "Update Check",
                        CustomMessageBox.MessageBoxButton.OK,
                        CustomMessageBox.MessageBoxImage.Warning);
                    return;
                }

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

                if (GitHubUpdater.CompareVersions(currentVersion, versionInfo.Version) < 0)
                {
                    UpdateDialogHelper.Show(this, currentVersion, versionInfo.Version.TrimStart('v'),
                        versionInfo.Url, versionInfo.FileName, token);
                }
                else
                {
                    CustomMessageBox.Show(this,
                        $"You're already running the latest version (v{currentVersion})!\n\nYour system is up to date.",
                        "Up to Date",
                        CustomMessageBox.MessageBoxButton.OK,
                        CustomMessageBox.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check error: {ex.Message}");

                // Reset the text if it's a TextBlock
                if (sender is TextBlock textBlock)
                {
                    textBlock.Text = "⟳ Updates";
                    textBlock.IsEnabled = true;
                }

                CustomMessageBox.Show(this,
                    "Failed to check for updates. Please try again later.",
                    "Update Check Failed",
                    CustomMessageBox.MessageBoxButton.OK,
                    CustomMessageBox.MessageBoxImage.Error);
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            var currentYear = DateTime.Now.Year;

            var infoDialog = new Window
            {
                Title = "About DeepCleaner Pro",
                Width = 520,
                Height = 340,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = (System.Windows.Media.Brush)FindResource("AbyssSurface"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BioTeal"),
                BorderThickness = new Thickness(2),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = System.Windows.Media.Color.FromRgb(0, 212, 170),
                    Direction = 270,
                    ShadowDepth = 0,
                    Opacity = 0.4,
                    BlurRadius = 30
                },
                Padding = new Thickness(32)
            };

            var mainStack = new StackPanel();

            mainStack.Children.Add(new TextBlock
            {
                Text = "About DeepCleaner Pro",
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimary"),
                FontSize = 20,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24)
            });

            AddInfoRow(mainStack, "Version", $"v{version}");
            AddInfoRow(mainStack, "Developer", "Subroto Saha");
            AddInfoRowWithLink(mainStack, "Repository", "github.com/isubroto/temp_cleaner", "https://github.com/isubroto/temp_cleaner");
            AddInfoRow(mainStack, "Copyright", $"© {currentYear} Subroto Saha");

            var closeButton = new Button
            {
                Content = "Close",
                Height = 40,
                Margin = new Thickness(0, 20, 0, 0),
                Style = (Style)FindResource("AbyssButton"),
                FontSize = 13,
                Background = (System.Windows.Media.Brush)FindResource("AbyssCard"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimary")
            };
            closeButton.Click += (s, args) => infoDialog.Close();
            mainStack.Children.Add(closeButton);

            border.Child = mainStack;
            infoDialog.Content = border;
            infoDialog.ShowDialog();
        }

        private void AddInfoRow(StackPanel parent, string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary"),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Text = value,
                Foreground = (System.Windows.Media.Brush)FindResource("BioTeal"),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);

            parent.Children.Add(grid);
        }

        private void AddInfoRowWithLink(StackPanel parent, string label, string displayText, string url)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary"),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var linkBlock = new TextBlock
            {
                Text = displayText,
                Foreground = (System.Windows.Media.Brush)FindResource("BioTeal"),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Cursor = Cursors.Hand,
                TextDecorations = TextDecorations.Underline,
                ToolTip = $"Click to open: {url}"
            };
            linkBlock.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            };
            linkBlock.MouseEnter += (s, e) => linkBlock.Foreground = (System.Windows.Media.Brush)FindResource("BioCyan");
            linkBlock.MouseLeave += (s, e) => linkBlock.Foreground = (System.Windows.Media.Brush)FindResource("BioTeal");

            Grid.SetColumn(linkBlock, 1);
            grid.Children.Add(linkBlock);

            parent.Children.Add(grid);
        }

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

            foreach (var path in paths)
            {
                TrimLogs();

                message.Text = $"🔍 Scanning: {path}";
                AddLog($"📂 Exploring: {path}");

                var items = await Task.Run(() => _dirFinder.DirandFile(path, msg =>
                {
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

            _fullList.AddRange(allItems);

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

            int processed = 0;
            long totalSizeDeleted = 0;
            _lastUIUpdateTicks = DateTime.UtcNow.Ticks;

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

                double percent = (double)processed / totalFiles * 100.0;
                SetProgressValue(percent);
                Percent.Text = $"{percent:F0}%";
                message.Text = $"🌊 Purified {processed}/{totalFiles} ({FormatFileSize(totalSizeDeleted)})";
                UpdateStorageCard(totalSizeDeleted);

                TrimLogs();
                await Task.Yield();
            }

            SetProgressValue(100.0);
            Percent.Text = "100%";
            UpdateStorageCard(totalSizeDeleted);

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
