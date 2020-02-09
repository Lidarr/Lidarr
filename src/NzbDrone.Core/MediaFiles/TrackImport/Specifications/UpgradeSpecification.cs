using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public UpgradeSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack item, DownloadClientItem downloadClientItem)
        {
            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;
            var qualityComparer = new QualityModelComparer(item.Artist.QualityProfile);

            foreach (var track in item.Tracks.Where(e => e.TrackFileId > 0))
            {
                var trackFile = track.TrackFile.Value;

                if (trackFile == null)
                {
                    _logger.Trace("Unable to get track file details from the DB. TrackId: {0} TrackFileId: {1}", track.Id, track.TrackFileId);
                    continue;
                }

                var qualityCompare = qualityComparer.Compare(item.Quality.Quality, trackFile.Quality.Quality);

                if (qualityCompare < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for all tracks. Skipping {0}", item.Path);
                    return Decision.Reject("Not an upgrade for existing track file(s)");
                }

                if (qualityCompare == 0 && downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                    item.Quality.Revision.CompareTo(trackFile.Quality.Revision) < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for all tracks. Skipping {0}", item.Path);
                    return Decision.Reject("Not an upgrade for existing track file(s)");
                }
            }

            return Decision.Accept();
        }
    }
}
