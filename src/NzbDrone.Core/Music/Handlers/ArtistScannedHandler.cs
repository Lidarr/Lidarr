using NLog;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public class ArtistScannedHandler : IHandle<ArtistScannedEvent>,
                                        IHandle<ArtistScanSkippedEvent>
    {
        private readonly IAlbumMonitoredService _albumMonitoredService;
        private readonly IArtistService _artistService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IAlbumAddedService _albumAddedService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ArtistScannedHandler(IAlbumMonitoredService albumMonitoredService,
                                    IArtistService artistService,
                                    IManageCommandQueue commandQueueManager,
                                    IAlbumAddedService albumAddedService,
                                    IEventAggregator eventAggregator,
                                    Logger logger)
        {
            _albumMonitoredService = albumMonitoredService;
            _artistService = artistService;
            _commandQueueManager = commandQueueManager;
            _albumAddedService = albumAddedService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private void HandleScanEvents(Artist artist)
        {
            var addOptions = artist.AddOptions;

            if (addOptions == null)
            {
                _albumAddedService.SearchForRecentlyAdded(artist.Id);
                return;
            }

            _logger.Info("[{0}] was recently added, performing post-add actions", artist.Name);
            _albumMonitoredService.SetAlbumMonitoredStatus(artist, addOptions);

            _eventAggregator.PublishEvent(new ArtistAddCompletedEvent(artist));

            if (addOptions.SearchForMissingAlbums)
            {
                _commandQueueManager.Push(new MissingAlbumSearchCommand(artist.Id));
            }

            artist.AddOptions = null;
            _artistService.RemoveAddOptions(artist);
        }

        public void Handle(ArtistScannedEvent message)
        {
            HandleScanEvents(message.Artist);
        }

        public void Handle(ArtistScanSkippedEvent message)
        {
            HandleScanEvents(message.Artist);
        }
    }
}
