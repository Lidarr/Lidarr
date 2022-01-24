using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Releases;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CutoffSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IMediaFileService _mediaFileService;
        private readonly ITrackService _trackService;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly Logger _logger;
        private readonly ICached<bool> _missingFilesCache;

        public CutoffSpecification(UpgradableSpecification upgradableSpecification,
                                   Logger logger,
                                   ICacheManager cacheManager,
                                   IMediaFileService mediaFileService,
                                   ICustomFormatCalculationService formatService,
                                   ITrackService trackService)
        {
            _upgradableSpecification = upgradableSpecification;
            _mediaFileService = mediaFileService;
            _trackService = trackService;
            _missingFilesCache = cacheManager.GetCache<bool>(GetType());
            _formatService = formatService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.Artist.QualityProfile.Value;

            foreach (var album in subject.Albums)
            {
                var tracksMissing = _missingFilesCache.Get(album.Id.ToString(),
                    () => _trackService.TracksWithoutFiles(album.Id).Any(),
                    TimeSpan.FromSeconds(30));
                var trackFiles = _mediaFileService.GetFilesByAlbum(album.Id);

                if (!tracksMissing && trackFiles.Any())
                {
                    // Get a distinct list of all current track qualities for a given album
                    var currentQualities = trackFiles.Select(c => c.Quality).Distinct().ToList();

                    _logger.Debug("Comparing file quality with report. Existing files contain {0}", currentQualities.ConcatToString());

                    var customFormats = _formatService.ParseCustomFormat(trackFiles[0]);

                    if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                                                               currentQualities,
                                                               customFormats,
                                                               subject.ParsedAlbumInfo.Quality))
                    {
                        _logger.Debug("Cutoff already met by existing files, rejecting.");

                        var qualityCutoffIndex = qualityProfile.GetIndex(qualityProfile.Cutoff);
                        var qualityCutoff = qualityProfile.Items[qualityCutoffIndex.Index];

                        return Decision.Reject("Existing files meets cutoff: {0}", qualityCutoff);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
