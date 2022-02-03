using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download;

public class AlbumGrabbedEvent : IEvent
{
    public RemoteAlbum Album { get; private set; }
    public int DownloadClientId { get; set; }
    public string DownloadClient { get; set; }
    public string DownloadClientName { get; set; }
    public string DownloadId { get; set; }

    public AlbumGrabbedEvent(RemoteAlbum album)
    {
        Album = album;
    }
}
