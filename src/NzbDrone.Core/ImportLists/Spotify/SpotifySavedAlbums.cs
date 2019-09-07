using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;

namespace NzbDrone.Core.ImportLists.Spotify
{
    public class SpotifySavedAlbumsSettings : SpotifySettingsBase<SpotifySavedAlbumsSettings>
    {
        public override string Scope => "user-library-read";
    }

    public class SpotifySavedAlbums : SpotifyImportListBase<SpotifySavedAlbumsSettings>
    {
        public SpotifySavedAlbums(ISpotifyProxy spotifyProxy,
                                  IImportListStatusService importListStatusService,
                                  IImportListRepository importListRepository,
                                  IConfigService configService,
                                  IParsingService parsingService,
                                  IHttpClient httpClient,
                                  Logger logger)
        : base(spotifyProxy, importListStatusService, importListRepository, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Spotify Saved Albums";

        public override IList<ImportListItemInfo> Fetch(SpotifyWebAPI api)
        {
            var result = new List<ImportListItemInfo>();

            var albums = _spotifyProxy.GetSavedAlbums(this, api);
            if (albums == null)
            {
                return result;
            }

            _logger.Trace($"Got {albums.Total} saved albums");

            while (true)
            {
                foreach (var album in albums?.Items ?? new List<SavedAlbum>())
                {
                    var artistName = album?.Album?.Artists?.FirstOrDefault()?.Name;
                    var albumName = album?.Album?.Name;
                    _logger.Trace($"Adding {artistName} - {albumName}");

                    if (artistName.IsNotNullOrWhiteSpace() && albumName.IsNotNullOrWhiteSpace())
                    {
                        result.AddIfNotNull(new ImportListItemInfo
                                            {
                                                Artist = artistName,
                                                Album = albumName
                                            });
                    }
                }
                if (!albums.HasNextPage())
                    break;
                albums = _spotifyProxy.GetNextPage(this, api, albums);
            }

            return result;
        }
    }
}
