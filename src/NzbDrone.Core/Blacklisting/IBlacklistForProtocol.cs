using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blacklisting
{
    public interface IBlacklistForProtocol
    {
        string Protocol { get; }
        bool IsBlacklisted(int artistId, ReleaseInfo release);
        Blacklist GetBlacklist(DownloadFailedEvent message);
    }
}
