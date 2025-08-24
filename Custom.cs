namespace TempCleaner
{
    public class AppSettings
    {
        public GitHubSettings GitHub { get; set; } = new GitHubSettings();
    }

    public class GitHubSettings
    {
        public string Token { get; set; } = string.Empty;
    }

    public class GetInformations
    {
        public string Version { set; get; }
        public string Url { set; get; }

        public GetInformations(string _version, string? _url)
        {
            Version = _version ?? string.Empty;
            Url = _url ?? string.Empty;
        }
    }
}
