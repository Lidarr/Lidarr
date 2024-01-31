using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Aggregation.Aggregators
{
    public class AggregateQuality : IAggregate<LocalTrack>
    {
        public int Order => 1;

        public LocalTrack Aggregate(LocalTrack localTrack, bool otherFiles)
        {
            var quality = localTrack.FileTrackInfo?.Quality;

            if (quality == null)
            {
                quality = localTrack.FolderAlbumInfo?.Quality;
            }

            if (quality == null)
            {
                quality = localTrack.DownloadClientAlbumInfo?.Quality;
            }

            localTrack.Quality = quality;
            return localTrack;
        }
    }
}
