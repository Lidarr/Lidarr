using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Aggregation.Aggregators
{
    public class AggregateReleaseGroup : IAggregate<LocalTrack>
    {
        public int Order => 1;

        public LocalTrack Aggregate(LocalTrack localTrack, bool otherFiles)
        {
            var releaseGroup = localTrack.DownloadClientAlbumInfo?.ReleaseGroup;

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localTrack.FolderAlbumInfo?.ReleaseGroup;
            }

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localTrack.FileTrackInfo?.ReleaseGroup;
            }

            localTrack.ReleaseGroup = releaseGroup;

            return localTrack;
        }
    }
}
