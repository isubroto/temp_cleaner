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
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    string url = $@"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                    // Show constructed request info

                    // Set timeout explicitly
                    client.Timeout = TimeSpan.FromSeconds(10);

                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode(); // Throws exception if not 2xx

                        string result = await response.Content.ReadAsStringAsync();
                        using JsonDocument document = JsonDocument.Parse(result);
                        JsonElement root = document.RootElement;
                        if (root.TryGetProperty("tag_name", out var tagName))
                        {
                            string? msiUrl = null;

                            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var asset in assets.EnumerateArray())
                                {
                                    if (asset.TryGetProperty("name", out var nameProp) &&
                                        nameProp.GetString()?.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) == true &&
                                        asset.TryGetProperty("browser_download_url", out var urlProp))
                                    {
                                        msiUrl = urlProp.GetString();
                                        break; // Get the first .msi file
                                    }
                                }
                            }

                            return new GetInformations(tagName.GetString()!, msiUrl);
                        }

                        return null;// Will only run if no exception occurred
                    }
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Update Check Error: Can Not Connect To Server");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"General Error: {ex.Message}");
            }

            return null; // or throw if you want the caller to handle the error
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
        public static void OpenUrlInDefaultBrowser(string url)
        {
            try
            {
                // Open the URL in the default browser
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Handle any errors that may occur (e.g., invalid URL)
                MessageBox.Show($"Failed to open URL: {ex.Message}");
            }
        }

    }


}
