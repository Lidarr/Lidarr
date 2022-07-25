namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylistItemInfo : SpotifyImportListItemInfo
    {
        public string ForeignPlaylistId { get; set; }
        public string PlaylistTitle { get; set; }
        public string TrackTitle { get; set; }
        public int Order { get; set; }
    }
}
