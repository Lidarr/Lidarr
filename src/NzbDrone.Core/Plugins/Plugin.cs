namespace NzbDrone.Core.Plugins
{
    public interface IPlugin
    {
        string Name { get; set; }
        string GithubUrl { get; set; }
        string Version { get; set; }
    }

    public class Plugin : IPlugin
    {
        public string Name { get; set; }
        public string GithubUrl { get; set; }
        public string Version { get; set; }
        public string PackageUrl { get; set; }
    }
}
