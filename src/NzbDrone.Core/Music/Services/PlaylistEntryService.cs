using System.Collections.Generic;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.ImportLists.Exclusions
{
    public interface IPlaylistEntryService
    {
        List<PlaylistEntry> UpsertMany(List<PlaylistEntry> entries);
        List<PlaylistEntry> FindByPlaylistId(int playlistId);
        List<int> FindPlaylistsByForeignAlbumId(string foreignAlbumId);
    }

    public class PlaylistEntryService : IPlaylistEntryService
    {
        private readonly IPlaylistEntryRepository _repo;

        public PlaylistEntryService(IPlaylistEntryRepository repo)
        {
            _repo = repo;
        }

        public List<PlaylistEntry> FindByPlaylistId(int playlistId)
        {
            return _repo.FindByPlaylistId(playlistId);
        }

        public List<PlaylistEntry> UpsertMany(List<PlaylistEntry> entries)
        {
            return _repo.UpsertMany(entries);
        }

        public List<int> FindPlaylistsByForeignAlbumId(string foreignAlbumId)
        {
            return _repo.FindPlaylistsByForeignAlbumId(foreignAlbumId);
        }
    }
}
