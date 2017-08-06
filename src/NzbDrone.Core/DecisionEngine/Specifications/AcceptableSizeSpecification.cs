using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Music;
using System.Collections.Generic;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class AcceptableSizeSpecification : IDecisionEngineSpecification
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly IEpisodeService _episodeService;
        private readonly Logger _logger;

        public AcceptableSizeSpecification(IQualityDefinitionService qualityDefinitionService, IEpisodeService episodeService, Logger logger)
        {
            _qualityDefinitionService = qualityDefinitionService;
            _episodeService = episodeService;
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;
        
        public Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Beginning size check for: {0}", subject);

            var quality = subject.ParsedAlbumInfo.Quality.Quality;

            if (subject.Release.Size == 0)
            {
                _logger.Debug("Release has unknown size, skipping size check.");
                return Decision.Accept();
            }

            var qualityDefinition = _qualityDefinitionService.Get(quality);
            if (qualityDefinition.MinSize.HasValue)
            {
                var minSize = qualityDefinition.MinSize.Value.Megabytes();

                //TODO: implement if we can get album runtimes from metadata
                //Multiply maxSize by Series.Runtime
                //minSize = minSize * subject.Series.Runtime * subject.Episodes.Count;

                //If the parsed size is smaller than minSize we don't want it
                if (subject.Release.Size < minSize)
                {
                    //var runtimeMessage = subject.Albums.Count == 1 ? $"{subject.Series.Runtime}min" : $"{subject.Episodes.Count}x {subject.Series.Runtime}min";

                    _logger.Debug("Item: {0}, Size: {1} is smaller than minimum allowed size ({2} bytes), rejecting.", subject, subject.Release.Size, minSize);
                    return Decision.Reject("{0} is smaller than minimum allowed {1}", subject.Release.Size.SizeSuffix(), minSize.SizeSuffix());
                }
            }
            if (!qualityDefinition.MaxSize.HasValue || qualityDefinition.MaxSize.Value == 0)
            {
                _logger.Debug("Max size is unlimited - skipping check.");
            }
            else
            {
                var maxSize = qualityDefinition.MaxSize.Value.Megabytes();

                //TODO: implement if we can get album runtimes from metadata
                //Multiply maxSize by Series.Runtime
                //maxSize = maxSize * subject.Series.Runtime * subject.Episodes.Count;

                //If the parsed size is greater than maxSize we don't want it
                if (subject.Release.Size > maxSize)
                {
                    //var runtimeMessage = subject.Episodes.Count == 1 ? $"{subject.Series.Runtime}min" : $"{subject.Episodes.Count}x {subject.Series.Runtime}min";

                    _logger.Debug("Item: {0}, Size: {1} is greater than maximum allowed size ({2}), rejecting.", subject, subject.Release.Size, maxSize);
                    return Decision.Reject("{0} is larger than maximum allowed {1}", subject.Release.Size.SizeSuffix(), maxSize.SizeSuffix());
                }
            }

            _logger.Debug("Item: {0}, meets size constraints.", subject);
            return Decision.Accept();
        }
    }
}
