using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public class AlbumEditedService : IHandle<AlbumEditedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly ITrackService _trackService;

        public AlbumEditedService(IManageCommandQueue commandQueueManager,
            ITrackService trackService)
        {
            _commandQueueManager = commandQueueManager;
            _trackService = trackService;
        }

        private static bool ReleaseFilterChanged(Album oldAlbum, Album newAlbum)
        {
            var oldTracks = new HashSet<int>(oldAlbum.PreferredTrackCounts ?? new List<int>());
            var newTracks = new HashSet<int>(newAlbum.PreferredTrackCounts ?? new List<int>());
            var oldMedia = new HashSet<int>(oldAlbum.PreferredMediumCounts ?? new List<int>());
            var newMedia = new HashSet<int>(newAlbum.PreferredMediumCounts ?? new List<int>());

            return !oldTracks.SetEquals(newTracks) || !oldMedia.SetEquals(newMedia);
        }

        public void Handle(AlbumEditedEvent message)
        {
            if (message.Album.AlbumReleases.IsLoaded && message.OldAlbum.AlbumReleases.IsLoaded)
            {
                var new_monitored = new HashSet<int>(message.Album.AlbumReleases.Value.Where(x => x.Monitored).Select(x => x.Id));
                var old_monitored = new HashSet<int>(message.OldAlbum.AlbumReleases.Value.Where(x => x.Monitored).Select(x => x.Id));
                if (!new_monitored.SetEquals(old_monitored) ||
                    (!message.OldAlbum.AnyReleaseOk && message.Album.AnyReleaseOk) ||
                    ReleaseFilterChanged(message.OldAlbum, message.Album))
                {
                    // Unlink any old track files
                    var tracks = _trackService.GetTracksByAlbum(message.Album.Id);
                    tracks.ForEach(x => x.TrackFileId = 0);
                    _trackService.SetFileIds(tracks);

                    var folders = new List<string> { message.Album.Artist.Value.Path };
                    _commandQueueManager.Push(new RescanFoldersCommand(folders, FilterFilesType.Matched, false, null));
                }
            }
        }
    }
}
