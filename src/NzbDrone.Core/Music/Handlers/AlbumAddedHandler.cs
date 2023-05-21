using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public class AlbumAddedHandler : IHandle<AlbumAddedEvent>
    {
        private readonly ICheckIfArtistShouldBeRefreshed _checkIfArtistShouldBeRefreshed;
        private readonly IManageCommandQueue _commandQueueManager;

        public AlbumAddedHandler(ICheckIfArtistShouldBeRefreshed checkIfArtistShouldBeRefreshed,
                                 IManageCommandQueue commandQueueManager)
        {
            _checkIfArtistShouldBeRefreshed = checkIfArtistShouldBeRefreshed;
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(AlbumAddedEvent message)
        {
            if (message.DoRefresh)
            {
                var artist = message.Album.Artist.Value;

                if (_checkIfArtistShouldBeRefreshed.ShouldRefresh(artist))
                {
                    _commandQueueManager.Push(new RefreshArtistCommand(new List<int> { artist.Id }));
                }
                else
                {
                    _commandQueueManager.Push(new RefreshAlbumCommand(message.Album.Id, true));
                }
            }
        }
    }
}
