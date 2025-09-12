using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;

namespace TempCleaner
{
    public static class GitHubUpdater
    {
        public static async Task<GetInformations> GetLatestReleaseVersionAsync(string owner, string repo, string token)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                    client.DefaultRequestHeaders.Add("User-Agent", "TempCleaner");
                    
                    // Enhanced authentication for private repos
                    if (!string.IsNullOrEmpty(token))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                    }

                    // Use GitHub API to get latest release info
                    string url = $@"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                    // Set longer timeout for private repo authentication
                    client.Timeout = TimeSpan.FromSeconds(15);

                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        // Better error handling for private repos
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            throw new HttpRequestException("Repository not found or access denied. Please check if:\n• The repository exists\n• Your GitHub token has access to this private repository\n• The token has 'repo' scope permissions");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            throw new HttpRequestException("GitHub authentication failed. Please check your GitHub token.");
                        }
                        
                        response.EnsureSuccessStatusCode();

                        string result = await response.Content.ReadAsStringAsync();
                        using JsonDocument document = JsonDocument.Parse(result);
                        JsonElement root = document.RootElement;
                        
                        if (root.TryGetProperty("tag_name", out var tagName))
                        {
                            string version = tagName.GetString()!;
                            string? msiUrl = null;
                            string? msiName = null;

                            // First, try to get MSI URL from assets (for public repos or properly configured private repos)
                            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                            {
                                var assetList = new List<string>();
                                foreach (var asset in assets.EnumerateArray())
                                {
                                    if (asset.TryGetProperty("name", out var nameProp))
                                    {
                                        string assetName = nameProp.GetString() ?? "unknown";
                                        assetList.Add(assetName);
                                        
                                        // Check for MSI file
                                        if (assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) &&
                                            asset.TryGetProperty("url", out var urlProp))
                                        {
                                            msiUrl = urlProp.GetString();
                                            msiName= nameProp.GetString();
                                            break; // Get the first .msi file
                                        }
                                    }
                                }

                                // If no MSI found in assets, construct common MSI URLs for this version
                                if (string.IsNullOrEmpty(msiUrl))
                                {
                                    // Try common MSI naming patterns and construct direct download URLs
                                    string[] commonMsiNames = {
                                        $"TempCleaner-{version}.msi",
                                        $"DeepCleaner-Pro-{version}.msi",
                                        $"temp-cleaner-{version}.msi",
                                        "TempCleaner-Setup.msi",
                                        "DeepCleaner-Pro-Setup.msi",
                                        "TempCleaner.msi"
                                    };

                                    // Construct direct GitHub release download URL
                                    string baseDownloadUrl = $"https://github.com/{owner}/{repo}/releases/download/{version}";
                                    
                                    // Return the first common pattern as the URL to try
                                    // The calling code will test these URLs to find the working one
                                    msiUrl = $"{baseDownloadUrl}/{commonMsiNames[0]}";
                                    
                                    System.Diagnostics.Debug.WriteLine($"No MSI in assets. Available assets: {string.Join(", ", assetList)}");
                                    System.Diagnostics.Debug.WriteLine($"Constructed MSI URL: {msiUrl}");
                                }
                            }
                            else
                            {
                                // No assets found, construct default MSI URL
                                string baseDownloadUrl = $"https://github.com/{owner}/{repo}/releases/download/{version}";
                                msiUrl = $"{baseDownloadUrl}/TempCleaner-{version}.msi";
                                
                                System.Diagnostics.Debug.WriteLine($"No assets property found. Constructed default MSI URL: {msiUrl}");
                            }

                            return new GetInformations(version, msiUrl ?? string.Empty, msiName ?? string.Empty);
                        }

                        return null;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"🌊 GitHub API Access Error\n\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update Check Error: {ex.Message}", "General Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }


        public static async Task DownloadUpdateAsync(string downloadUrl, string filePath)
        {
            using var client = new HttpClient();
            var content = await client.GetByteArrayAsync(downloadUrl);

            await System.IO.File.WriteAllBytesAsync(filePath, content);

            // Optionally, run the installer
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
            });
        }
        public static int CompareVersions(string currentVersion, string latestVersion)
        {
            // Remove leading 'v' if present
            currentVersion = currentVersion.TrimStart('v', 'V');
            latestVersion = latestVersion.TrimStart('v', 'V');

            var currentParts = currentVersion.Split('.').Select(int.Parse).ToList();
            var latestParts = latestVersion.Split('.').Select(int.Parse).ToList();

            // Pad the shorter version with zeros (e.g., 1.2 vs 1.2.0)
            int maxLength = Math.Max(currentParts.Count, latestParts.Count);
            while (currentParts.Count < maxLength) currentParts.Add(0);
            while (latestParts.Count < maxLength) latestParts.Add(0);

            for (int i = 0; i < maxLength; i++)
            {
                if (currentParts[i] < latestParts[i]) return -1;
                if (currentParts[i] > latestParts[i]) return 1;
            }

            return 0; // equal
        }
        public static void DownloadUpdateWithProgress(string url, string fileName = null, string githubToken = null)
        {
            try
            {
                // Extract filename from URL if not provided
                if (string.IsNullOrEmpty(fileName))
                {
                    var uri = new Uri(url);
                    fileName = System.IO.Path.GetFileName(uri.LocalPath);
                    
                    // Default filename if extraction fails
                    if (string.IsNullOrEmpty(fileName) || fileName == "/")
                    {
                        fileName = "DeepCleaner_Pro_Setup.msi";
                    }
                }

                // Direct download - no validation, no checks
                var downloadWindow = new DownloadProgressWindow(url, fileName, githubToken);
                downloadWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start download: {ex.Message}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void OpenUrlInDefaultBrowser(string url, string githubToken = null)
        {
            try
            {
                // Check if this is a direct download link
                var uri = new Uri(url);
                var fileName = System.IO.Path.GetFileName(uri.LocalPath);
                
                // Always prioritize MSI downloads with custom progress window
                if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    // Use custom download progress window for MSI files with GitHub token
                    DownloadUpdateWithProgress(url, fileName, githubToken);
                }
                else
                {
                    // For non-MSI URLs, show a message and don't proceed
                    MessageBox.Show("🌊 DeepCleaner Pro updates are only available as MSI installers.\n\nPlease use the official release page to download the MSI file.", "Update Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Optionally, still open the browser for manual download
                    var result = MessageBox.Show("Would you like to open the release page in your browser to manually download the MSI file?", "Open Release Page", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that may occur (e.g., invalid URL)
                MessageBox.Show($"Failed to process URL: {ex.Message}", "URL Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }


}
