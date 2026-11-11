using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class TrackSearchCommand : Command
    {
        public List<int> TrackIds { get; set; }

        public override bool SendUpdatesToClient => true;

        public TrackSearchCommand()
        {
        }

        public TrackSearchCommand(List<int> trackIds)
        {
            TrackIds = trackIds;
        }
    }
}
