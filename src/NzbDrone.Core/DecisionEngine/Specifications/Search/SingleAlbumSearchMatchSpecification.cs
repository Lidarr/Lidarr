using System;
using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SingleAlbumSearchMatchSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public SingleAlbumSearchMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteAlbum remoteAlbum, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var singleEpisodeSpec = searchCriteria as AlbumSearchCriteria;
            if (singleEpisodeSpec == null) return Decision.Accept();
                
            if (Parser.Parser.CleanArtistTitle(singleEpisodeSpec.AlbumTitle) != Parser.Parser.CleanArtistTitle(remoteAlbum.ParsedAlbumInfo.AlbumTitle))
            {
                _logger.Debug("Album does not match searched album title, skipping.");
                return Decision.Reject("Wrong album");
            }

            //if (!remoteAlbum.ParsedAlbumInfo.AlbumTitles.Any())
            //{
            //    _logger.Debug("Full discography result during single album search, skipping.");
            //    return Decision.Reject("Full artist pack");
            //}

            return Decision.Accept();
        }
    }
}