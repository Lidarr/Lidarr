﻿using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IDecisionEngineSpecification
    {
        RejectionType Type { get; }

        //Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria);
        Decision IsSatisfiedBy(RemoteAlbum subject, SearchCriteriaBase searchCriteria);
    }
}
