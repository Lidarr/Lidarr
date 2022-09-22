using System;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class RepackSpecification : IDecisionEngineSpecification
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public RepackSpecification(IMediaFileService mediaFileService, UpgradableSpecification upgradableSpecification, IConfigService configService, Logger logger)
        {
            _mediaFileService = mediaFileService;
            _upgradableSpecification = upgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            if (!subject.ParsedAlbumInfo.Quality.Revision.IsRepack)
            {
                return Decision.Accept();
            }

            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;

            if (downloadPropersAndRepacks == ProperDownloadTypes.DoNotPrefer)
            {
                _logger.Debug("Repacks are not preferred, skipping check");
                return Decision.Accept();
            }

            foreach (var album in subject.Albums)
            {
                var releaseGroup = subject.ParsedAlbumInfo.ReleaseGroup;
                var trackFiles = _mediaFileService.GetFilesByAlbum(album.Id);

                foreach (var file in trackFiles)
                {
                    if (_upgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedAlbumInfo.Quality))
                    {
                        if (downloadPropersAndRepacks == ProperDownloadTypes.DoNotUpgrade)
                        {
                            _logger.Debug("Auto downloading of repacks is disabled");
                            return Decision.Reject("Repack downloading is disabled");
                        }

                        var fileReleaseGroup = file.ReleaseGroup;

                        if (fileReleaseGroup.IsNullOrWhiteSpace())
                        {
                            return Decision.Reject("Unable to determine release group for the existing file");
                        }

                        if (releaseGroup.IsNullOrWhiteSpace())
                        {
                            return Decision.Reject("Unable to determine release group for this release");
                        }

                        if (!fileReleaseGroup.Equals(releaseGroup, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _logger.Debug("Release is a repack for a different release group. Release Group: {0}. File release group: {1}", releaseGroup, fileReleaseGroup);
                            return Decision.Reject("Release is a repack for a different release group. Release Group: {0}. File release group: {1}", releaseGroup, fileReleaseGroup);
                        }
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
