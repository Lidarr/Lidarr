using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Music.Commands
{
    public class RefreshAlbumCommand : Command
    {
        public int? AlbumId { get; set; }
        public bool IsNewAlbum { get; set; }

        public RefreshAlbumCommand()
        {
        }

        public RefreshAlbumCommand(int? albumId, bool isNewAlbum = false)
        {
            AlbumId = albumId;
            IsNewAlbum = isNewAlbum;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !AlbumId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
