using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace TempCleaner
{
    public static class GitHubUpdater
    {
        private static readonly Lazy<HttpClient> LazyClient = new(() =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Add("User-Agent", "TempCleaner");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        });

        private static HttpClient Client => LazyClient.Value;

        public static async Task<GetInformations?> GetLatestReleaseVersionAsync(string owner, string repo, string token)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await Client.SendAsync(request);

                // Retry with token if needed
                HttpResponseMessage finalResponse = response;
                bool usedToken = false;

                if (!response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(token))
                {
                    using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                    finalResponse = await Client.SendAsync(tokenRequest);
                    usedToken = true;
                }

                if (!finalResponse.IsSuccessStatusCode)
                {
                    if (usedToken) finalResponse.Dispose();
                    return null;
                }

                var json = await finalResponse.Content.ReadAsStringAsync();
                if (usedToken) finalResponse.Dispose();

                return ParseReleaseInfo(json, owner, repo, usedToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GetInformations? ParseReleaseInfo(string json, string owner, string repo, bool usedToken)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElement))
                return null;

            var tag = tagElement.GetString() ?? "";
            var version = tag.TrimStart('v', 'V');
            string? msiUrl = null;
            string? msiName = null;

            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (!asset.TryGetProperty("name", out var nameProp)) continue;
                    
                    var name = nameProp.GetString();
                    if (name == null || !name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)) continue;

                    var urlProp = usedToken ? "url" : "browser_download_url";
                    if (asset.TryGetProperty(urlProp, out var urlElement))
                    {
                        msiUrl = urlElement.GetString();
                        msiName = name;
                        break;
                    }
                }
            }

            // Fallback URL
            if (string.IsNullOrEmpty(msiUrl))
            {
                msiName = $"Temp_Cleaner-{version}.msi";
                msiUrl = $"https://github.com/{owner}/{repo}/releases/download/{tag}/{msiName}";
            }

            return new GetInformations(tag, msiUrl, msiName ?? "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareVersions(string current, string latest)
        {
            var currentSpan = current.AsSpan().TrimStart(['v', 'V']);
            var latestSpan = latest.AsSpan().TrimStart(['v', 'V']);

            Span<int> currentParts = stackalloc int[4];
            Span<int> latestParts = stackalloc int[4];

            int currentCount = ParseVersionParts(currentSpan, currentParts);
            int latestCount = ParseVersionParts(latestSpan, latestParts);

            int maxParts = Math.Max(currentCount, latestCount);
            for (int i = 0; i < maxParts; i++)
            {
                int c = i < currentCount ? currentParts[i] : 0;
                int l = i < latestCount ? latestParts[i] : 0;
                
                if (c < l) return -1;
                if (c > l) return 1;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseVersionParts(ReadOnlySpan<char> version, Span<int> parts)
        {
            int partIndex = 0;
            int currentValue = 0;

            foreach (var c in version)
            {
                if (c == '.')
                {
                    if (partIndex < parts.Length)
                        parts[partIndex++] = currentValue;
                    currentValue = 0;
                }
                else if (char.IsDigit(c))
                {
                    currentValue = currentValue * 10 + (c - '0');
                }
            }

            if (partIndex < parts.Length)
                parts[partIndex++] = currentValue;

            return partIndex;
        }

        public static void DownloadUpdateWithProgress(string url, string? fileName = null, string? githubToken = null)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.GetFileName(new Uri(url).LocalPath);
                    if (string.IsNullOrEmpty(fileName) || fileName == "/")
                        fileName = "DeepCleaner_Pro_Setup.msi";
                }

                new DownloadProgressWindow(url, fileName, githubToken).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void OpenUrlInDefaultBrowser(string url, string? githubToken = null)
        {
            try
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                
                if (!string.IsNullOrEmpty(fileName) && fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadUpdateWithProgress(url, fileName, githubToken);
                }
                else
                {
                    if (MessageBox.Show("Open release page in browser?", "Download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
