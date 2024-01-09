using Lidarr.Api.V1.Albums;
using Lidarr.Api.V1.Artist;
using Lidarr.Api.V1.CustomFormats;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Parser;

namespace Lidarr.Api.V1.Parse
{
    [V1ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;
        private readonly IRemoteAlbumAggregationService _aggregationService;
        private readonly ICustomFormatCalculationService _formatCalculator;

        public ParseController(IParsingService parsingService,
                               IRemoteAlbumAggregationService aggregationService,
                               ICustomFormatCalculationService formatCalculator)
        {
            _parsingService = parsingService;
            _aggregationService = aggregationService;
            _formatCalculator = formatCalculator;
        }

        [HttpGet]
        [Produces("application/json")]
        public ParseResource Parse(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsedAlbumInfo = Parser.ParseAlbumTitle(title);

            if (parsedAlbumInfo == null)
            {
                return new ParseResource
                {
                    Title = title
                };
            }

            var remoteAlbum = _parsingService.Map(parsedAlbumInfo);

            if (remoteAlbum != null)
            {
                _aggregationService.Augment(remoteAlbum);

                remoteAlbum.CustomFormats = _formatCalculator.ParseCustomFormat(remoteAlbum, 0);
                remoteAlbum.CustomFormatScore = remoteAlbum?.Artist?.QualityProfile?.Value.CalculateCustomFormatScore(remoteAlbum.CustomFormats) ?? 0;

                return new ParseResource
                {
                    Title = title,
                    ParsedAlbumInfo = remoteAlbum.ParsedAlbumInfo,
                    Artist = remoteAlbum.Artist.ToResource(),
                    Albums = remoteAlbum.Albums.ToResource(),
                    CustomFormats = remoteAlbum.CustomFormats?.ToResource(false),
                    CustomFormatScore = remoteAlbum.CustomFormatScore
                };
            }
            else
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedAlbumInfo = parsedAlbumInfo
                };
            }
        }
    }
}
