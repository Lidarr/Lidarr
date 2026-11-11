using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    // Reuse the album search transport so existing indexer plugins remain compatible.
    public class TrackSearchCriteria : AlbumSearchCriteria
    {
        public bool Matches(ParsedAlbumInfo parsed, Artist artist)
        {
            return artist.Id == Artist.Id && !parsed.Discography && parsed.AlbumTitle.IsNotNullOrWhiteSpace() &&
                Tracks.Any(track => track.Title.IsNotNullOrWhiteSpace() &&
                    track.Title.CleanAlbumTitle().NormalizeTrackTitle() == parsed.AlbumTitle.CleanAlbumTitle().NormalizeTrackTitle());
        }
    }
}
