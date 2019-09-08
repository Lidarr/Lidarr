using System.Collections.Generic;
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
    public class SpotifyFollowedArtistsSettings : SpotifySettingsBase<SpotifyFollowedArtistsSettings>
    {
        public override string Scope => "user-follow-read";
    }

    public class SpotifyFollowedArtists : SpotifyImportListBase<SpotifyFollowedArtistsSettings>
    {
        public SpotifyFollowedArtists(ISpotifyProxy spotifyProxy,
                                      IImportListStatusService importListStatusService,
                                      IImportListRepository importListRepository,
                                      IConfigService configService,
                                      IParsingService parsingService,
                                      IHttpClient httpClient,
                                      Logger logger)
        : base(spotifyProxy, importListStatusService, importListRepository, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Spotify Followed Artists";

        public override IList<ImportListItemInfo> Fetch(SpotifyWebAPI api)
        {
            var result = new List<ImportListItemInfo>();

            var followedArtists = _spotifyProxy.GetFollowedArtists(this, api);
            var artists = followedArtists?.Artists;

            while (true)
            {
                if (artists == null)
                {
                    return result;
                }

                foreach (var artist in artists?.Items ?? new List<FullArtist>())
                {
                    if (artist.Name.IsNotNullOrWhiteSpace())
                    {
                        result.AddIfNotNull(new ImportListItemInfo
                                            {
                                                Artist = artist.Name,
                                            });
                    }
                }

                if (!artists.HasNext())
                {
                    break;
                }

                artists = _spotifyProxy.GetNextPage(this, api, artists);
            }

            return result;
        }
    }
}
