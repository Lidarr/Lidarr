using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Music.Commands
{
    public class RefreshArtistCommand : Command
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ArtistId
        {
            get => 0;
            set
            {
                if (ArtistIds.Empty())
                {
                    ArtistIds.Add(value);
                }
            }
        }

        public List<int> ArtistIds { get; set; }
        public bool IsNewArtist { get; set; }

        public RefreshArtistCommand()
        {
            ArtistIds = new List<int>();
        }

        public RefreshArtistCommand(List<int> artistIds, bool isNewArtist = false)
        {
            ArtistIds = artistIds;
            IsNewArtist = isNewArtist;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => ArtistIds.Empty();

        public override bool IsLongRunning => true;

        public override string CompletionMessage => "Completed";
    }
}
