namespace TempCleaner
{
    public class AppSettings
    {
        public GitHubSettings GitHub { get; set; } = new GitHubSettings();
    }

    public class GitHubSettings
    {
        public string Token { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Repo { get; set; } = string.Empty;
    }

    public class GetInformations
    {
        public string Version { set; get; }
        public string Url { set; get; }
        public string FileName { set; get; }

        public GetInformations(string _version, string _url,string _name)
        {
            Version = _version;
            Url = _url;
            FileName = _name;
        }
    }
}
