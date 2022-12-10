using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class AcceptableSizeSpecification : IDecisionEngineSpecification
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly Logger _logger;

        public AcceptableSizeSpecification(IQualityDefinitionService qualityDefinitionService, Logger logger)
        {
            _qualityDefinitionService = qualityDefinitionService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Beginning size check for: {0}", subject);

            var quality = subject.ParsedAlbumInfo.Quality.Quality;

            if (subject.Release.Size == 0)
            {
                _logger.Debug("Release has unknown size, skipping size check");
                return Decision.Accept();
            }

            var qualityDefinition = _qualityDefinitionService.Get(quality);

            var minReleaseDuration = subject.Albums.Select(a => a.AlbumReleases.Value.Where(r => r.Monitored || a.AnyReleaseOk).Select(r => r.Duration).MinOrDefault()).Sum() / 1000;
            var maxReleaseDuration = subject.Albums.Select(a => a.AlbumReleases.Value.Where(r => r.Monitored || a.AnyReleaseOk).Select(r => r.Duration).MaxOrDefault()).Sum() / 1000;

            if (qualityDefinition.MinSize.HasValue)
            {
                if (maxReleaseDuration == 0)
                {
                    _logger.Debug("Album duration is 0, unable to validate size until it is available, rejecting");
                    return Decision.Reject("Album duration is 0, unable to validate size until it is available");
                }

                var minSize = qualityDefinition.MinSize.Value.Kilobits();

                // Multiply minSize by smallest release duration
                minSize *= minReleaseDuration;

                // If the parsed size is smaller than minSize we don't want it
                if (subject.Release.Size < minSize)
                {
                    var runtimeMessage = $"{minReleaseDuration}sec";

                    _logger.Debug("Item: {0}, Size: {1} is smaller than minimum allowed size ({2} bytes for {3}), rejecting.", subject, subject.Release.Size, minSize, runtimeMessage);
                    return Decision.Reject("{0} is smaller than minimum allowed {1}", subject.Release.Size.SizeSuffix(), minSize.SizeSuffix());
                }
            }

            if (!qualityDefinition.MaxSize.HasValue || qualityDefinition.MaxSize.Value == 0)
            {
                _logger.Debug("Max size is unlimited, skipping size check");
            }
            else if (maxReleaseDuration == 0)
            {
                _logger.Debug("Album duration is 0, unable to validate size until it is available, rejecting");
                return Decision.Reject("Album duration is 0, unable to validate size until it is available");
            }
            else
            {
                var maxSize = qualityDefinition.MaxSize.Value.Kilobits();

                // Multiply maxSize by Album.Duration
                maxSize *= maxReleaseDuration;

                // If the parsed size is greater than maxSize we don't want it
                if (subject.Release.Size > maxSize)
                {
                    var runtimeMessage = $"{maxReleaseDuration}sec";

                    _logger.Debug("Item: {0}, Size: {1} is greater than maximum allowed size ({2} bytes for {3}), rejecting", subject, subject.Release.Size, maxSize, runtimeMessage);
                    return Decision.Reject("{0} is larger than maximum allowed {1}", subject.Release.Size.SizeSuffix(), maxSize.SizeSuffix());
                }
            }

            _logger.Debug("Item: {0}, meets size constraints", subject);
            return Decision.Accept();
        }
    }
}
