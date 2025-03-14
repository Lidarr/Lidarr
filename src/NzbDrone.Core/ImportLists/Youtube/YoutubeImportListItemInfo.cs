using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Youtube;

public class YoutubeImportListItemInfo : ImportListItemInfo
{
    public string ArtistYoutubeId { get; set; }
    public string AlbumYoutubeId { get; set; }

    public override string ToString()
    {
        return string.Format("[{0}] {1}", ArtistYoutubeId, AlbumYoutubeId);
    }
}
