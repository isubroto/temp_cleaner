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
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28"); // CHANGE: always set

            string url = $@"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            client.Timeout = TimeSpan.FromSeconds(15);

            // CHANGE: local helper to GET with or without token
            async Task<HttpResponseMessage> GetReleaseAsync(bool useToken)
            {
                if (useToken && !string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = null;
                }
                return await client.GetAsync(url);
            }

            // CHANGE: try anonymous first
            using HttpResponseMessage responseAnon = await GetReleaseAsync(useToken: false);

            HttpResponseMessage responseFinal = responseAnon;
            bool usedToken = false; // CHANGE: track which mode succeeded

            // CHANGE: if anon failed and we have a token, retry with token
            if (!responseAnon.IsSuccessStatusCode &&
                !string.IsNullOrWhiteSpace(token) &&
                (responseAnon.StatusCode == HttpStatusCode.NotFound ||
                 responseAnon.StatusCode == HttpStatusCode.Unauthorized ||
                 responseAnon.StatusCode == HttpStatusCode.Forbidden))
            {
                responseAnon.Dispose();
                var responseWithToken = await GetReleaseAsync(useToken: true);
                responseFinal = responseWithToken;
                usedToken = true;
            }

            // CHANGE: handle common errors after both attempts
            if (responseFinal.StatusCode == HttpStatusCode.NotFound)
            {
                throw new HttpRequestException("Repository or releases not found. If the repo is private, ensure your token has access.");
            }
            if (responseFinal.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("GitHub authentication failed. The provided token may be invalid or expired.");
            }
            if (responseFinal.StatusCode == HttpStatusCode.Forbidden)
            {
                var body = await responseFinal.Content.ReadAsStringAsync();
                if (body.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new HttpRequestException("GitHub rate limit hit. Try again later or provide a token.");
                if (body.IndexOf("User-Agent", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new HttpRequestException("GitHub requires a User-Agent header. Please set it.");
                responseFinal.EnsureSuccessStatusCode();
            }

            responseFinal.EnsureSuccessStatusCode();

            string result = await responseFinal.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(result);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("tag_name", out var tagName))
            {
                string tag = tagName.GetString()!;
                string versionForFile = tag.TrimStart('v', 'V'); // CHANGE: for filename
                string? msiUrl = null;
                string? msiName = null;

                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        if (asset.TryGetProperty("name", out var nameProp))
                        {
                            string assetName = nameProp.GetString() ?? "unknown";
                            if (assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                            {
                                if (usedToken)
                                {
                                    // CHANGE: private path (token): use API asset URL (requires Accept: application/octet-stream on download)
                                    if (asset.TryGetProperty("url", out var apiUrlProp))
                                    {
                                        msiUrl = apiUrlProp.GetString();
                                        msiName = assetName;
                                        break;
                                    }
                                }
                                else
                                {
                                    // CHANGE: public path: use direct browser download URL
                                    if (asset.TryGetProperty("browser_download_url", out var bduProp))
                                    {
                                        msiUrl = bduProp.GetString();
                                        msiName = assetName;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(msiUrl))
                {
                    // CHANGE: fallback to predictable public URL
                    string baseDownloadUrl = $"https://github.com/{owner}/{repo}/releases/download/{tag}";
                    msiName = $"Temp_Cleaner-{versionForFile}.msi";
                    msiUrl = $"{baseDownloadUrl}/{msiName}";
                }

                return new GetInformations(tag, msiUrl ?? string.Empty, msiName ?? string.Empty);
            }

            return null;
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
