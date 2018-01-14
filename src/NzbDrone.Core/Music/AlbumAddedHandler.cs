using System.Linq;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public class AlbumAddedHandler : IHandle<AlbumAddedEvent>,
                                     IHandle<AlbumsAddedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public AlbumAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(AlbumAddedEvent message)
        {
            _commandQueueManager.Push(new RefreshAlbumCommand(message.Album.Id));
        }

        public void Handle(AlbumsAddedEvent message)
        {
            _commandQueueManager.PushMany(message.AlbumIds.Select(s => new RefreshAlbumCommand(s)).ToList());
        }
    }
}
