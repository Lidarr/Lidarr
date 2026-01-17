using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.TrackImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification<LocalTrack>
    {
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _customFormatCalculationService;
        private readonly Logger _logger;

        public UpgradeSpecification(IConfigService configService,
                                    ICustomFormatCalculationService customFormatCalculationService,
                                    Logger logger)
        {
            _configService = configService;
            _customFormatCalculationService = customFormatCalculationService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalTrack localTrack, DownloadClientItem downloadClientItem)
        {
            if (!localTrack.Tracks.Any(e => e.TrackFileId > 0))
            {
                // No existing tracks, skip.  This guards against new artists not having a QualityProfile.
                return Decision.Accept();
            }

            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;
            var qualityComparer = new QualityModelComparer(localTrack.Artist.QualityProfile);
            var qualityProfile = localTrack.Artist.QualityProfile.Value;

            // Calculate custom formats for the new track
            var newCustomFormats = _customFormatCalculationService.ParseCustomFormat(localTrack);
            var newFormatScore = qualityProfile.CalculateCustomFormatScore(newCustomFormats);

            foreach (var track in localTrack.Tracks.Where(e => e.TrackFileId > 0))
            {
                var trackFile = track.TrackFile.Value;

                if (trackFile == null)
                {
                    _logger.Trace("Unable to get track file details from the DB. TrackId: {0} TrackFileId: {1}", track.Id, track.TrackFileId);
                    continue;
                }

                var qualityCompare = qualityComparer.Compare(localTrack.Quality.Quality, trackFile.Quality.Quality);

                if (qualityCompare < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for all tracks. New Quality is {0}. Skipping {1}", localTrack.Quality.Quality, localTrack.Path);
                    return Decision.Reject("Not an upgrade for existing track file(s). New Quality is {0}", localTrack.Quality.Quality);
                }

                // When quality is equal, compare custom format scores
                if (qualityCompare == 0)
                {
                    var existingCustomFormats = _customFormatCalculationService.ParseCustomFormat(trackFile, localTrack.Artist);
                    var existingFormatScore = qualityProfile.CalculateCustomFormatScore(existingCustomFormats);

                    if (newFormatScore < existingFormatScore)
                    {
                        _logger.Debug("This file isn't a custom format upgrade. Existing: {0} [{1}], New: {2} [{3}]. Skipping {4}",
                                      existingFormatScore,
                                      string.Join(", ", existingCustomFormats.Select(x => x.Name)),
                                      newFormatScore,
                                      string.Join(", ", newCustomFormats.Select(x => x.Name)),
                                      localTrack.Path);
                        return Decision.Reject("Not a custom format upgrade for existing track file(s). Existing score: {0}, New score: {1}",
                                                existingFormatScore,
                                                newFormatScore);
                    }

                    if (downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                        localTrack.Quality.Revision.CompareTo(trackFile.Quality.Revision) < 0)
                    {
                        _logger.Debug("This file isn't a quality upgrade for all tracks. Skipping {0}", localTrack.Path);
                        return Decision.Reject("Not an upgrade for existing track file(s)");
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
