using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public class ArtistAddedHandler : IHandle<ArtistAddedEvent>,
                                      IHandle<ArtistsImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IEventAggregator _eventAggregator;

        public ArtistAddedHandler(IManageCommandQueue commandQueueManager, IEventAggregator eventAggregator)
        {
            _commandQueueManager = commandQueueManager;
            _eventAggregator = eventAggregator;
        }

        public void Handle(ArtistAddedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new RefreshArtistCommand(new List<int> { message.Artist.Id }, true));
            }
            else
            {
                // Trigger Artist Metadata download when adding Albums
                _eventAggregator.PublishEvent(new ArtistRefreshCompleteEvent(message.Artist));
            }
        }

        public void Handle(ArtistsImportedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new BulkRefreshArtistCommand(message.Artists.Select(a => a.Id).ToList(), true));
            }
            else
            {
                // Trigger Artist Metadata download when adding Albums
                foreach (var artist in message.Artists)
                {
                    _eventAggregator.PublishEvent(new ArtistRefreshCompleteEvent(artist));
                }
            }
        }
    }
}
