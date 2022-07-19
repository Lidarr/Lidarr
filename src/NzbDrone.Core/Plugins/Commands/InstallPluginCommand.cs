using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Plugins.Commands
{
    public class InstallPluginCommand : Command
    {
        public string GithubUrl { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool IsExclusive => true;
        public override string CompletionMessage => null;
    }
}
