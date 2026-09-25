using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class CloseAlbumMatchSpecification : IImportDecisionEngineSpecification<LocalAlbumRelease>
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public CloseAlbumMatchSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalAlbumRelease item, DownloadClientItem downloadClientItem)
        {
            double dist;
            string reasons;
            var albumThreshold = 1 - (_configService.AlbumMatchThreshold / 100.0);
            var trackThreshold = 1 - (_configService.TrackMatchThreshold / 100.0);

            // strict when a new download
            if (item.NewDownload)
            {
                dist = item.Distance.NormalizedDistance();
                reasons = item.Distance.Reasons;
                if (dist > albumThreshold)
                {
                    _logger.Debug($"Album match is not close enough: {dist} vs {albumThreshold} {reasons}. Skipping {item}");
                    return Decision.Reject($"Album match is not close enough: {1 - dist:P1} vs {1 - albumThreshold:P0} {reasons}");
                }

                var worstTrackMatch = item.LocalTracks.Where(x => x.Distance != null).MaxBy(x => x.Distance.NormalizedDistance());
                if (worstTrackMatch == null)
                {
                    _logger.Debug($"No tracks matched");
                    return Decision.Reject("No tracks matched");
                }
                else
                {
                    var maxTrackDist = worstTrackMatch.Distance.NormalizedDistance();
                    var trackReasons = worstTrackMatch.Distance.Reasons;
                    if (maxTrackDist > trackThreshold)
                    {
                        _logger.Debug($"Worst track match: {maxTrackDist} vs {trackThreshold} {trackReasons}. Skipping {item}");
                        return Decision.Reject($"Worst track match: {1 - maxTrackDist:P1} vs {1 - trackThreshold:P0} {trackReasons}");
                    }
                }
            }

            // otherwise importing existing files in library
            else
            {
                // get album distance ignoring whether tracks are missing
                dist = item.Distance.NormalizedDistanceExcluding(new List<string> { "missing_tracks", "unmatched_tracks" });
                reasons = item.Distance.Reasons;
                if (dist > albumThreshold)
                {
                    _logger.Debug($"Album match is not close enough: {dist} vs {albumThreshold} {reasons}. Skipping {item}");
                    return Decision.Reject($"Album match is not close enough: {1 - dist:P1} vs {1 - albumThreshold:P0} {reasons}");
                }
            }

            _logger.Debug($"Accepting release {item}: dist {dist} vs {albumThreshold} {reasons}");
            return Decision.Accept();
        }
    }
}
