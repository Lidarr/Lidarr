using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class SameFileSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly Logger _logger;

        public SameFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack localTrack, DownloadClientItem downloadClientItem)
        {
            var trackFiles = localTrack.Tracks.Where(e => e.TrackFileId != 0).Select(e => e.TrackFile).ToList();

            if (trackFiles.Count == 0)
            {
                _logger.Debug("No existing track file, skipping");
                return Decision.Accept();
            }

            if (trackFiles.Count > 1)
            {
                _logger.Debug("More than one existing track file, skipping.");
                return Decision.Accept();
            }

            var trackFile = trackFiles.First().Value;

            if (trackFile == null)
            {
                var track = localTrack.Tracks.First();
                _logger.Trace("Unable to get track file details from the DB. TrackId: {0} TrackFileId: {1}", track.Id, track.TrackFileId);

                return Decision.Accept();
            }

            if (trackFiles.First().Value.Size == localTrack.Size)
            {
                _logger.Debug("'{0}' Has the same filesize as existing file", localTrack.Path);
                return Decision.Reject("Has the same filesize as existing file");
            }

            return Decision.Accept();
        }
    }
}
