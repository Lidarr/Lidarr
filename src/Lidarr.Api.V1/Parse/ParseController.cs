using Lidarr.Api.V1.Albums;
using Lidarr.Api.V1.Artist;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Parser;

namespace Lidarr.Api.V1.Parse
{
    [V1ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;

        public ParseController(IParsingService parsingService)
        {
            _parsingService = parsingService;
        }

        [HttpGet]
        public ParseResource Parse(string title)
        {
            var parsedAlbumInfo = Parser.ParseAlbumTitle(title);

            if (parsedAlbumInfo == null)
            {
                return null;
            }

            var remoteAlbum = _parsingService.Map(parsedAlbumInfo);

            if (remoteAlbum != null)
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedAlbumInfo = remoteAlbum.ParsedAlbumInfo,
                    Artist = remoteAlbum.Artist.ToResource(),
                    Albums = remoteAlbum.Albums.ToResource()
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
