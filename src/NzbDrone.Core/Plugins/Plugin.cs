using System.Reflection;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Owner { get; }
        string GithubUrl { get; }
        PluginVersion InstalledVersion { get; }
        PluginVersion AvailableVersion { get; set; }
    }

    public abstract class Plugin : IPlugin
    {
        private PluginVersion _installedVersion;

        public virtual string Name { get; }
        public virtual string Owner { get; }
        public virtual string GithubUrl { get; }

        public PluginVersion InstalledVersion
        {
            get
            {
                if (_installedVersion != null)
                {
                    return _installedVersion;
                }

                var informationalVersion = GetType().Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;

                if (informationalVersion.IsNotNullOrWhiteSpace())
                {
                    _installedVersion = PluginVersion.Parse(informationalVersion);
                }

                return _installedVersion ??= GetType().Assembly.GetName().Version;
            }
        }

        public PluginVersion AvailableVersion { get; set; }
    }

    public class RemotePlugin
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string GithubUrl { get; set; }
        public PluginVersion Version { get; set; }
        public string PackageUrl { get; set; }
        public string Tree { get; set; }
    }
}
