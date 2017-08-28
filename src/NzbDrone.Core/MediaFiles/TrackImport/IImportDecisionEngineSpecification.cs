using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public interface IImportDecisionEngineSpecification
    {
        //Decision IsSatisfiedBy(LocalEpisode localEpisode);
        Decision IsSatisfiedBy(LocalTrack localTrack);
    }
}
