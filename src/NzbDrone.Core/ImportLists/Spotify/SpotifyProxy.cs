using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using SpotifyAPI.Web;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public interface ISpotifyProxy
    {
        PrivateUser GetPrivateProfile<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        Paging<SimplePlaylist> GetUserPlaylists<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string id)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        FollowedArtistsResponse GetFollowedArtists<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        Paging<SavedAlbum> GetSavedAlbums<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        FullPlaylist GetPlaylist<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string id, List<string> fields)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        Paging<T> GetNextPage<T, TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, Paging<T> item)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        FollowedArtistsResponse GetNextPage<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, FollowedArtistsResponse item)
            where TSettings : SpotifySettingsBase<TSettings>, new();
        SearchResponse SearchItems<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string query, SearchRequest.Types type)
            where TSettings : SpotifySettingsBase<TSettings>, new();
    }

    public class SpotifyProxy : ISpotifyProxy
    {
        private readonly Logger _logger;

        public SpotifyProxy(Logger logger)
        {
            _logger = logger;
        }

        public PrivateUser GetPrivateProfile<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, x => x.UserProfile.Current());
        }

        public Paging<SimplePlaylist> GetUserPlaylists<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string id)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, x => x.Playlists.GetUsers(id));
        }

        public FollowedArtistsResponse GetFollowedArtists<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, x => x.Follow.OfCurrentUser(new FollowOfCurrentUserRequest { Limit = 50 }));
        }

        public Paging<SavedAlbum> GetSavedAlbums<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, x => x.Library.GetAlbums(new LibraryAlbumsRequest { Limit = 50 }));
        }

        public FullPlaylist GetPlaylist<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string id, List<string> fields)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            var request = new PlaylistGetRequest(PlaylistGetRequest.AdditionalTypes.Track);
            foreach (var field in fields)
            {
                request.Fields.Add(field);
            }

            return Execute(list, api, x => x.Playlists.Get(id, request));
        }

        public Paging<T> GetNextPage<T, TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, Paging<T> item)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, (x) => x.NextPage(item));
        }

        public FollowedArtistsResponse GetNextPage<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, FollowedArtistsResponse item)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, (x) => x.NextPage(item.Artists));
        }

        public SearchResponse SearchItems<TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, string query, SearchRequest.Types type)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return Execute(list, api, (x) => x.Search.Item(new SearchRequest(type, query)));
        }

        public T Execute<T, TSettings>(SpotifyImportListBase<TSettings> list, SpotifyClient api, Func<SpotifyClient, Task<T>> method)
            where TSettings : SpotifySettingsBase<TSettings>, new()
        {
            return method(api).GetAwaiter().GetResult();
        }
    }
}
