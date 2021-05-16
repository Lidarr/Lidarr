using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;

namespace Lidarr.Api.V1.Artist
{
    [V1ApiController("artist/lookup")]
    public class ArtistLookupController : Controller
    {
        private readonly ISearchForNewArtist _searchProxy;

        public ArtistLookupController(ISearchForNewArtist searchProxy)
        {
            _searchProxy = searchProxy;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewArtist(term);
            return MapToResource(searchResults).ToList();
        }

        private static IEnumerable<ArtistResource> MapToResource(IEnumerable<NzbDrone.Core.Music.Artist> artist)
        {
            foreach (var currentArtist in artist)
            {
                var resource = currentArtist.ToResource();
                var poster = currentArtist.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.Url;
                }

                yield return resource;
            }
        }
    }
}
