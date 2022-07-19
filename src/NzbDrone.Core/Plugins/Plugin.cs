using System;

namespace NzbDrone.Core.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Owner { get; }
        string GithubUrl { get; }
        Version InstalledVersion { get; }
        Version AvailableVersion { get; set; }
    }

    public abstract class Plugin : IPlugin
    {
        public virtual string Name { get; }
        public virtual string Owner { get; }
        public virtual string GithubUrl { get; }

        public Version InstalledVersion => GetType().Assembly.GetName().Version;
        public Version AvailableVersion { get; set; }
    }

    public class RemotePlugin
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string GithubUrl { get; set; }
        public Version Version { get; set; }
        public string PackageUrl { get; set; }
    }
}
