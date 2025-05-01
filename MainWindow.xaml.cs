using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
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

            SHERB_NOPROGRESSUI = 0x00000001, // No progress tracking window during the emptying of the recycle bin

            SHERB_NOSOUND = 0x00000004 // No sound when the emptying of the recycle bin is complete
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
            // UI reset
            Percent.Visibility = Visibility.Hidden;
            Clear.IsEnabled = false;
            Clear.Content = "Clear";
            Clear.Cursor = Cursors.No;

            UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            name = UserName.Split('\\');
            Check.Content = "Checking";

            Logs.Items.Clear();
            Progress.Value = 0;
            FullList.Clear();
            Dirfinder.count = 0;

            int cnt = 0;
            int intit = 0;

            string[] paths = new[]
            {
        @"C:\Windows\Prefetch",
        @"C:\Windows\SoftwareDistribution\Download",
        @"C:\Windows\Temp",
        $@"C:\Users\{name[1]}\AppData\Local\Temp",
        $@"C:\Users\{name[1]}\AppData\Local\Microsoft\Windows\INetCache",
        $@"C:\Users\{name[1]}\AppData\Local\Microsoft\Windows\Explorer",
        $@"C:\Users\{name[1]}\AppData\Local\CrashDumps",
        @"C:\Windows\System32\LogFiles"
    };

            List<string> allItems = new();
            Progress.IsIndeterminate = true;
            message.Visibility = Visibility.Visible;
            countNum.Visibility = Visibility.Visible;

            // Background scan
            await Task.Run(() =>
            {
                foreach (string path in paths)
                {
                    var items = Dirfinder.DirandFile(path, msg =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            message.Text = msg.Message;
                            countNum.Text = $"{msg.Count} files Found";
                            intit = msg.Count;
                        });
                    });

                    if (items != null)
                        allItems.AddRange(items);
                }
            });

            double unitStep = allItems.Count > 0 ? 396.0 / allItems.Count : 0;
            double current = 0;

            Percent.Visibility = Visibility.Visible;
            Progress.IsIndeterminate = false;
            Progress.ApplyTemplate();

            Check.Content = "Adding";
            countNum.Visibility = Visibility.Hidden;

            // Buffered UI updates
            const int updateInterval = 100;
            List<string> buffer = new();
            var sw = Stopwatch.StartNew();

            foreach (var item in allItems)
            {
                buffer.Add(item);
                FullList.Add(item); // internal tracking
                current += unitStep;
                cnt++;

                if (buffer.Count >= updateInterval || sw.ElapsedMilliseconds > 200)
                {
                    var currentBatch = new List<string>(buffer);
                    buffer.Clear();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var logItem in currentBatch)
                            Logs.Items.Add(logItem);

                        Logs.ScrollIntoView(currentBatch[^1]);
                        Progress.Value = current;
                        Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                        message.Text = $"{cnt}/{intit} files Added";
                    });

                    sw.Restart();
                }

                await Task.Delay(1); // yield for UI
            }

            // Flush remaining items
            if (buffer.Count > 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var logItem in buffer)
                        Logs.Items.Add(logItem);

                    Logs.ScrollIntoView(buffer[^1]);
                    Progress.Value = current;
                    Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                    message.Text = $"{cnt}/{intit} files Added";
                });
            }

            Check.Content = "Checked";
            message.Text = "Ready To Remove";

            if (allItems.Count > 0)
            {
                Clear.IsEnabled = true;
                Clear.Cursor = Cursors.Hand;
            }
        }


        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Percent.Visibility = Visibility.Visible;
            Logs.Items.Clear();
            Clear.Content = "Clearing";
            Clear.IsEnabled = false;
            Clear.Cursor = Cursors.No;
            int num = 0;
            int len = FullList.Count;
            message.Visibility = Visibility.Visible;
            double unitStep = FullList.Count > 0 ? 396.0 / FullList.Count : 0;
            double current = 0;

            List<string> buffer = new();
            var sw = Stopwatch.StartNew();

            await Task.Run(async () =>
            {
                foreach (var del in FullList)
                {
                    string log;
                    try
                    {
                        if (File.Exists(del))
                        {
                            File.Delete(del);
                            log = $"✔ Deleted file: {del}";
                        }
                        else if (Directory.Exists(del))
                        {
                            Directory.Delete(del, true);
                            log = $"✔ Deleted directory: {del}";
                        }
                        else
                        {
                            log = $"⚠ Not found: {del}";
                        }
                    }
                    catch
                    {
                        log = $"❌ Failed to delete: {del}";
                    }

                    current += unitStep;
                    buffer.Add(log);
                    num++;

                    if (buffer.Count >= 50 || sw.ElapsedMilliseconds > 300)
                    {
                        var batch = new List<string>(buffer);
                        buffer.Clear();

                        await Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var msg in batch)
                                Logs.Items.Add(msg);

                            Logs.ScrollIntoView(batch[^1]);
                            Progress.Value = current;
                            Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                            message.Text = $"{num}/{len} files Deleted";

                        });

                        sw.Restart();
                    }
                }

                // Final flush
                if (buffer.Count > 0)
                {
                    var finalBatch = new List<string>(buffer);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var msg in finalBatch)
                            Logs.Items.Add(msg);

                        Logs.ScrollIntoView(finalBatch[^1]);
                        Progress.Value = current;
                        Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                        message.Text = $"{num}/{len} files Deleted";
                    });
                }
            });

            SHEmptyRecycleBin(IntPtr.Zero, string.Empty, RecycleFlag.SHERB_NOSOUND | RecycleFlag.SHERB_NOCONFIRMATION);

            Dispatcher.Invoke(() =>
            {
                Logs.Items.Add("🎉 Clean up completed!");
                Logs.ScrollIntoView("🎉 Clean up completed!");
                Clear.Content = "Cleared";
                Check.Content = "Check";
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            try
            {
                var version = await InitializeGitHubUpdaterAsync(LoadSettings().GitHub.Token); // Await the task to get the result
            var result = GitHubUpdater.CompareVersions(Assembly.GetExecutingAssembly().GetName().Version.ToString(), version.Version);
            if (result < 0)
            {
                MessageBoxResult res = MessageBox.Show($@"An update is available!{Environment.NewLine}Current Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}{Environment.NewLine}Latest Version: {version.Version}{Environment.NewLine}Do you want to download?", "Update !", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    GitHubUpdater.OpenUrlInDefaultBrowser(version.Url);
                }
            }
            }
            catch 
            {
            
            }


        }
    }
}