using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Music;


namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class EpisodeRequestedSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public EpisodeRequestedSpecification(Logger logger)
        {
            _logger = logger;
        }

        public RejectionType Type => RejectionType.Permanent;

        //public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        //{
        //    if (searchCriteria == null)
        //    {
        //        return Decision.Accept();
        //    }

        //    var criteriaEpisodes = searchCriteria.Episodes.Select(v => v.Id).ToList();
        //    var remoteEpisodes = remoteEpisode.Episodes.Select(v => v.Id).ToList();

        //    if (!criteriaEpisodes.Intersect(remoteEpisodes).Any())
        //    {
        //        _logger.Debug("Release rejected since the episode wasn't requested: {0}", remoteEpisode.ParsedEpisodeInfo);
        //        return Decision.Reject("Episode wasn't requested");
        //    }

        //    return Decision.Accept();
        //}

        public Decision IsSatisfiedBy(RemoteAlbum remoteAlbum, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var criteriaAlbum = searchCriteria.Album.Id; //s.Select(v => v.Id).ToList();
            var remoteAlbums = remoteAlbum.Albums.Select(v => v.Id).ToList();

            if (!remoteAlbums.Contains(criteriaAlbum))
            {
                _logger.Debug("Release rejected since the album wasn't requested: {0}", remoteAlbum.ParsedAlbumInfo);
                return Decision.Reject("Episode wasn't requested");
            }

            return Decision.Accept();
        }
    }
}
