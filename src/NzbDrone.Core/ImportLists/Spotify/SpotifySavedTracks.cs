using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifySavedTracksSettings : SpotifySettingsBase<SpotifySavedTracksSettings>
    {
        public override string Scope => "user-library-read";
    }

    public class SpotifySavedTracks : SpotifyImportListBase<SpotifySavedTracksSettings>
    {
        public SpotifySavedTracks(ISpotifyProxy spotifyProxy,
                                      IMetadataRequestBuilder requestBuilder,
                                      IImportListStatusService importListStatusService,
                                      IImportListRepository importListRepository,
                                      IConfigService configService,
                                      IParsingService parsingService,
                                      IHttpClient httpClient,
                                      Logger logger)
        : base(spotifyProxy, requestBuilder, importListStatusService, importListRepository, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Spotify Saved Tracks";

        public override IList<SpotifyImportListItemInfo> Fetch(SpotifyWebAPI api)
        {
            var result = new List<SpotifyImportListItemInfo>();

            var savedTracks = _spotifyProxy.GetSavedTracks(this, api);

            // savedTracks may be null if the spotify proxy returns nothing (e.g. user has no saved tracks)
            if (savedTracks == null)
            {
                _logger.Trace("No saved tracks returned");
                return result;
            }

            _logger.Trace($"Got {savedTracks.Total} saved tracks");

            while (true)
            {
                if (savedTracks?.Items == null)
                {
                    return result;
                }

                foreach (var savedTrack in savedTracks.Items)
                {
                    result.AddIfNotNull(ParseSavedTrack(savedTrack));
                }

                if (!savedTracks.HasNextPage())
                {
                    break;
                }

                savedTracks = _spotifyProxy.GetNextPage(this, api, savedTracks);
            }

            return result;
        }

        private SpotifyImportListItemInfo ParseSavedTrack(SavedTrack savedTrack)
        {
            // From spotify docs: "Note, a track object may be null. This can happen if a track is no longer available."
            if (savedTrack?.Track?.Album != null)
            {
                var album = savedTrack.Track.Album;
                var albumName = album.Name;
                var artistName = album.Artists?.FirstOrDefault()?.Name ?? savedTrack.Track?.Artists?.FirstOrDefault()?.Name;

                if (albumName.IsNotNullOrWhiteSpace() && artistName.IsNotNullOrWhiteSpace())
                {
                    return new SpotifyImportListItemInfo
                    {
                        Artist = artistName,
                        Album = album.Name,
                        AlbumSpotifyId = album.Id,
                        ReleaseDate = ParseSpotifyDate(album.ReleaseDate, album.ReleaseDatePrecision)
                    };
                }
            }

            return null;
        }
    }
}
