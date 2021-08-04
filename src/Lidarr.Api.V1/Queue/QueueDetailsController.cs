using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Queue
{
    [V1ApiController("queue/details")]
    public class QueueDetailsController : RestControllerWithSignalR<QueueResource, NzbDrone.Core.Queue.Queue>,
                               IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>
    {
        private readonly IQueueService _queueService;
        private readonly IPendingReleaseService _pendingReleaseService;

        public QueueDetailsController(IBroadcastSignalRMessage broadcastSignalRMessage, IQueueService queueService, IPendingReleaseService pendingReleaseService)
            : base(broadcastSignalRMessage)
        {
            _queueService = queueService;
            _pendingReleaseService = pendingReleaseService;
        }

        public override QueueResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public List<QueueResource> GetQueue(int? artistId, [FromQuery]List<int> albumIds, bool includeArtist = false, bool includeAlbum = true)
        {
            var queue = _queueService.GetQueue();
            var pending = _pendingReleaseService.GetPendingQueue();
            var fullQueue = queue.Concat(pending);

            if (artistId.HasValue)
            {
                return fullQueue.Where(q => q.Artist?.Id == artistId.Value).ToResource(includeArtist, includeAlbum);
            }

            if (albumIds.Any())
            {
                return fullQueue.Where(q => q.Album != null && albumIds.Contains(q.Album.Id)).ToResource(includeArtist, includeAlbum);
            }

            return fullQueue.ToResource(includeArtist, includeAlbum);
        }

        [NonAction]
        public void Handle(QueueUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }

        [NonAction]
        public void Handle(PendingReleasesUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
