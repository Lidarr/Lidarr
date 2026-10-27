using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class ReleaseWantedSpecification : IImportDecisionEngineSpecification<LocalAlbumRelease>
    {
        public const string FilterRejectionReason = "Album release does not match the album's track/media count filter";

        private readonly Logger _logger;

        public ReleaseWantedSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalAlbumRelease item, DownloadClientItem downloadClientItem)
        {
            var album = item.AlbumRelease.Album.Value;

            if (album.IsReleaseWanted(item.AlbumRelease))
            {
                return Decision.Accept();
            }

            if (album.AnyReleaseOk && album.HasReleaseFilter())
            {
                _logger.Debug("AlbumRelease {0} [{1} tracks, {2} media] does not match the album's release filter", item, item.AlbumRelease.TrackCount, item.AlbumRelease.AudioMediumCount());
                return Decision.Reject(FilterRejectionReason);
            }

            _logger.Debug("AlbumRelease {0} was not requested", item);
            return Decision.Reject("Album release not requested");
        }
    }
}
