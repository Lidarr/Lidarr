using System.IO;
using System.Linq;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.ImportLists.Exclusions
{
    public interface IPlaylistService
    {
        Playlist UpdatePlaylist(Playlist playlist);
    }

    public class PlaylistService : IPlaylistService,
        IHandleAsync<AlbumImportedEvent>,
        IHandleAsync<TrackFileRenamedEvent>
    {
        private readonly IPlaylistRepository _repo;
        private readonly IPlaylistEntryService _playlistEntryService;
        private readonly ITrackService _trackService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public PlaylistService(IPlaylistRepository repo,
                               IPlaylistEntryService playlistEntryService,
                               ITrackService trackService,
                               IMediaFileService mediaFileService,
                               IDiskProvider diskProvider,
                               Logger logger)
        {
            _repo = repo;
            _playlistEntryService = playlistEntryService;
            _trackService = trackService;
            _mediaFileService = mediaFileService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public Playlist UpdatePlaylist(Playlist playlist)
        {
            var existing = _repo.GetPlaylist(playlist.ForeignPlaylistId);
            if (existing != null)
            {
                playlist.Id = existing.Id;
            }

            _repo.Upsert(playlist);
            playlist.Items.ForEach(x => x.PlaylistId = playlist.Id);

            _playlistEntryService.UpsertMany(playlist.Items);

            WritePlaylist(playlist.Id);

            return playlist;
        }

        public void WritePlaylist(int playlistId)
        {
            var playlist = _repo.Get(playlistId);

            _logger.Debug($"Writing playlist {playlist.Title}");

            playlist.Items = _playlistEntryService.FindByPlaylistId(playlistId);

            _logger.Trace($"Got {playlist.Items.Count} tracks");

            if (!playlist.Items.Any() || playlist.OutputFolder.IsNullOrWhiteSpace())
            {
                return;
            }

            var sb = new StringBuilder();
            bool doWrite = false;

            foreach (var item in playlist.Items.OrderBy(x => x.Order))
            {
                _logger.Trace($"Getting track for {item.ForeignAlbumId} {item.TrackTitle}");
                var track = _trackService.FindTrackByTitleInexact(item.ForeignAlbumId, item.TrackTitle);

                if (track?.HasFile == true)
                {
                    var file = _mediaFileService.Get(track.TrackFileId);
                    var relative = Path.GetRelativePath(playlist.OutputFolder, file.Path);

                    _logger.Debug($"Got track {relative}");
                    sb.AppendLine(relative);
                    doWrite = true;
                }
            }

            if (doWrite)
            {
                var filename = Path.Combine(playlist.OutputFolder, playlist.Title + ".m3u");
                _diskProvider.WriteAllText(filename, sb.ToString());
            }
        }

        public void HandleAsync(AlbumImportedEvent message)
        {
            var playlistIds = _playlistEntryService.FindPlaylistsByForeignAlbumId(message.Album.ForeignAlbumId);

            foreach (var id in playlistIds)
            {
                WritePlaylist(id);
            }
        }

        public void HandleAsync(TrackFileRenamedEvent message)
        {
            var albumId = message.TrackFile.Album.Value.ForeignAlbumId;
            var playlistIds = _playlistEntryService.FindPlaylistsByForeignAlbumId(albumId);

            foreach (var id in playlistIds)
            {
                WritePlaylist(id);
            }
        }
    }
}
