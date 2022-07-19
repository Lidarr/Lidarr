using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistForProtocol
    {
        string Protocol { get; }
        bool IsBlocklisted(int artistId, ReleaseInfo release);
        Blocklist GetBlocklist(DownloadFailedEvent message);
    }
}
