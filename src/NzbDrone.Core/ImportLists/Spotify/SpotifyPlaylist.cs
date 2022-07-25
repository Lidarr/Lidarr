using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Validation;
using SpotifyAPI.Web;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifyPlaylist : SpotifyImportListBase<SpotifyPlaylistSettings>
    {
        private readonly List<string> _fields = new () { "id", "name", "tracks.next", "tracks.items(track(type, name, artists(id, name), album(id, album_type, name, release_date, release_date_precision, artists(id, name))))" };
        private readonly IPlaylistService _playlistService;

        public SpotifyPlaylist(ISpotifyProxy spotifyProxy,
                               IMetadataRequestBuilder requestBuilder,
                               IImportListStatusService importListStatusService,
                               IImportListRepository importListRepository,
                               IPlaylistService playlistService,
                               IConfigService configService,
                               IParsingService parsingService,
                               IHttpClient httpClient,
                               Logger logger)
        : base(spotifyProxy, requestBuilder, importListStatusService, importListRepository, configService, parsingService, httpClient, logger)
        {
            _playlistService = playlistService;
        }

        public override string Name => "Spotify Playlists";

        public override IList<SpotifyImportListItemInfo> Fetch(SpotifyClient api)
        {
            return Settings.PlaylistIds.SelectMany(x => Fetch(api, x)).ToList();
        }

        public IList<SpotifyImportListItemInfo> Fetch(SpotifyClient api, string playlistId)
        {
            var result = new List<SpotifyImportListItemInfo>();

            _logger.Trace($"Processing playlist {playlistId}");

            var playlist = _spotifyProxy.GetPlaylist(this, api, playlistId, _fields);
            var playlistTracks = playlist?.Tracks;
            int order = 0;

            while (true)
            {
                if (playlistTracks?.Items == null)
                {
                    return result;
                }

                foreach (var playlistTrack in playlistTracks.Items)
                {
                    result.AddIfNotNull(ParsePlaylistTrack(api, playlistTrack, playlistId, playlist.Name, ref order));
                }

                if (playlistTracks.Next == null)
                {
                    break;
                }

                playlistTracks = _spotifyProxy.GetNextPage(this, api, playlistTracks);
            }

            return result;
        }

        protected override void ProcessMappedReleases(IList<SpotifyImportListItemInfo> items)
        {
            var spotifyPlaylistItems = items.Select(x => (SpotifyPlaylistItemInfo)x);

            var groups = spotifyPlaylistItems.GroupBy(x => x.ForeignPlaylistId);

            foreach (var group in groups)
            {
                var first = group.First();

                var playlistItems = group.Select(x => new PlaylistEntry
                {
                    Order = x.Order,
                    ForeignAlbumId = x.AlbumMusicBrainzId,
                    TrackTitle = x.TrackTitle
                }).ToList();

                var playlist = new Playlist
                {
                    ForeignPlaylistId = first.ForeignPlaylistId,
                    Title = first.PlaylistTitle,
                    OutputFolder = Settings.PlaylistFolder,
                    Items = playlistItems
                };

                _playlistService.UpdatePlaylist(playlist);
            }
        }

        private SpotifyPlaylistItemInfo ParsePlaylistTrack(SpotifyClient api, PlaylistTrack<IPlayableItem> playableItem, string playlistId, string playlistName, ref int order)
        {
            var track = playableItem?.Track as FullTrack;

            // From spotify docs: "Note, a track object may be null. This can happen if a track is no longer available."
            if (track?.Album == null)
            {
                return null;
            }

            var album = track.Album;
            var trackName = track.Name;
            var artistName = album.Artists?.FirstOrDefault()?.Name ?? track?.Artists?.FirstOrDefault()?.Name;

            if (artistName.IsNullOrWhiteSpace())
            {
                return null;
            }

            _logger.Trace($"album {album.Name} type: {album.AlbumType}");

            if (album.AlbumType == "single")
            {
                album = GetBestAlbum(api, artistName, trackName, album.TotalTracks) ?? album;
                _logger.Trace($"revised type: {album.AlbumType}");
            }

            var albumName = album.Name;

            if (albumName.IsNullOrWhiteSpace())
            {
                return null;
            }

            return new SpotifyPlaylistItemInfo
            {
                ForeignPlaylistId = playlistId,
                Artist = artistName,
                Album = album.Name,
                AlbumSpotifyId = album.Id,
                PlaylistTitle = playlistName,
                TrackTitle = track.Name,
                Order = ++order,
                ReleaseDate = ParseSpotifyDate(album.ReleaseDate, album.ReleaseDatePrecision)
            };
        }

        private SimpleAlbum GetBestAlbum(SpotifyClient api, string artistName, string trackName, int currentTrackCount)
        {
            _logger.Trace($"Finding full album for {artistName}: {trackName}");
            var search = _spotifyProxy.SearchItems(this, api, $"artist:\"{artistName}\" track:\"{trackName}\"", SearchRequest.Types.Track);

            var result = search?.Tracks?.Items?.FirstOrDefault(x => x?.Album?.AlbumType == "album" && IsAcceptableAlbumOrSingle(x, artistName, trackName))?.Album ??
                         search?.Tracks?.Items?.FirstOrDefault(x => x?.Album?.AlbumType == "single" && x.Album.TotalTracks > 3 && x.Album.TotalTracks > currentTrackCount && IsAcceptableAlbumOrSingle(x, artistName, trackName))?.Album;

            if (result != null)
            {
                _logger.Trace($"Found {result.AlbumType} {result.Name} by {result.Artists.FirstOrDefault()?.Name}");
            }

            return result;
        }

        private bool IsAcceptableAlbumOrSingle(FullTrack x, string artistName, string trackName)
        {
            return x.Name == trackName &&
                (x.Artists?.Any(a => a.Name == artistName) ?? false) &&
                !(x.Album.Artists?.Any(a => a.Name == "Various Artists") ?? false) &&
                ParseSpotifyDate(x?.Album.ReleaseDate, x.Album.ReleaseDatePrecision) <= DateTime.UtcNow;
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getPlaylists")
            {
                if (Settings.RefreshToken.IsNullOrWhiteSpace())
                {
                    return new
                    {
                        playlists = new List<object>()
                    };
                }

                Settings.Validate().Filter("RefreshToken").ThrowOnError();

                var api = GetApi();
                try
                {
                    var profile = _spotifyProxy.GetPrivateProfile(this, api);
                    var playlistPage = _spotifyProxy.GetUserPlaylists(this, api, profile.Id);
                    _logger.Trace($"Got {playlistPage.Total} playlists");

                    var playlists = new List<SimplePlaylist>();
                    while (true)
                    {
                        if (playlistPage == null)
                        {
                            break;
                        }

                        playlists.AddRange(playlistPage.Items);

                        if (playlistPage.Next == null)
                        {
                            break;
                        }

                        playlistPage = _spotifyProxy.GetNextPage(this, api, playlistPage);
                    }

                    return new
                    {
                        options = new
                        {
                            user = profile.DisplayName,
                            playlists = playlists.OrderBy(p => p.Name)
                                .Select(p => new
                                {
                                    id = p.Id,
                                    name = p.Name
                                })
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error fetching playlists from Spotify");
                    return new { };
                }
            }
            else
            {
                return base.RequestAction(action, query);
            }
        }
    }
}
