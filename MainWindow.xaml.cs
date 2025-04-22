
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
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
            Percent.Visibility = Visibility.Hidden;
            Clear.IsEnabled = false;
            UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            name = UserName.Split('\\');

            Logs.Document.Blocks.Clear();
            Progress.Value = 0;
            FullList.Clear();

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

            List<string> allItems = new List<string>();

            // Run the directory fetch in a background task
            await Task.Run(() =>
            {
                foreach (string path in paths)
                {
                    var items = Dirfinder.DirandFile(path);
                    allItems.AddRange(items);
                }
            });

            double unitStep = 396.0 / allItems.Count;
            double current = 0;
            Percent.Visibility = Visibility.Visible;
            // Now process the items and update the UI thread in real-time
            foreach (var item in allItems)
            {
                // Update the UI thread with Dispatcher.Invoke
                Dispatcher.Invoke(() =>
                {
                    FullList.Add(item);
                    Logs.Document.Blocks.Add(new Paragraph(new Run(item)));
                    current += unitStep;
                    Progress.Value = current;
                    Logs.ScrollToEnd();
                    Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                });

                // To avoid blocking UI, delay a bit before the next UI update
                await Task.Delay(10); // Delay to give time for UI to update
            }
            if (allItems.Count > 0)
            {
                Clear.IsEnabled = true;
            }


        }
        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Percent.Visibility = Visibility.Visible;
            Logs.Document.Blocks.Clear();

            double unitStep = 396.0 / FullList.Count;
            double current = 0;

            await Task.Run(async () =>
            {
                foreach (var del in FullList)
                {
                    if (File.Exists(del))
                    {
                        try
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            File.Delete(del);

                            Dispatcher.Invoke(() =>
                            {
                                Logs.Document.Blocks.Add(new Paragraph(new Run($"✔ Deleted file: {del}")));
                                Progress.Value = current += unitStep;
                                Logs.ScrollToEnd();
                                Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                            });
                        }
                        catch
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Logs.Document.Blocks.Add(new Paragraph(new Run($"❌ Failed to delete file: {del}")));
                                Progress.Value = current += unitStep;
                                Logs.ScrollToEnd();
                                Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%"; ;
                            });
                        }
                    }
                    else if (Directory.Exists(del))
                    {
                        try
                        {
                            Directory.Delete(del, true);
                            Dispatcher.Invoke(() =>
                            {
                                Logs.Document.Blocks.Add(new Paragraph(new Run($"✔ Deleted directory: {del}")));
                                Progress.Value = current += unitStep;
                                Logs.ScrollToEnd();
                                Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                            });
                        }
                        catch
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Logs.Document.Blocks.Add(new Paragraph(new Run($"❌ Failed to delete directory: {del}")));
                                Progress.Value = current += unitStep;
                                Logs.ScrollToEnd();
                                Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Progress.Value = current += unitStep;
                            Percent.Text = $"{Math.Round((current / 396.0) * 100, 2)}%";
                        });
                    }

                    // Add a short delay (e.g., 100 ms)
                    await Task.Delay(100);
                }
            });

            SHEmptyRecycleBin(IntPtr.Zero, string.Empty, RecycleFlag.SHERB_NOSOUND | RecycleFlag.SHERB_NOCONFIRMATION);

            Dispatcher.Invoke(() =>
            {
                Logs.Document.Blocks.Add(new Paragraph(new Run("🎉 Clean up completed!")));
                Logs.ScrollToEnd();
            });

            Clear.IsEnabled = false;
        }

    }
}