using System.Collections.Generic;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylistTracksRequestGenerator : SpotifyRequestGeneratorBase<SpotifyPlaylistTracksSettings>
    {

        public override ImportListPageableRequestChain GetSpotifyListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetPagedRequests());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetPagedRequests()
        {
            yield return AddTokenToRequest(new ImportListRequest($"{Settings.BaseUrl.TrimEnd('/')}/playlists/{Settings.PlaylistId}/tracks?fields=items(track(name,album(name,id),artists))&limit={Settings.Count}", HttpAccept.Json));
        }
    }
}
