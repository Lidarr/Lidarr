using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;

namespace Lidarr.Api.V1.Albums
{
    [V1ApiController("album/lookup")]
    public class AlbumLookupController : Controller
    {
        private readonly ISearchForNewAlbum _searchProxy;

        public AlbumLookupController(ISearchForNewAlbum searchProxy)
        {
            _searchProxy = searchProxy;
        }

        [HttpGet]
        public object Search(string term)
        {
            var searchResults = _searchProxy.SearchForNewAlbum(term, null);
            return MapToResource(searchResults).ToList();
        }

        private static IEnumerable<AlbumResource> MapToResource(IEnumerable<NzbDrone.Core.Music.Album> albums)
        {
            foreach (var currentAlbum in albums)
            {
                var resource = currentAlbum.ToResource();
                var cover = currentAlbum.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);
                if (cover != null)
                {
                    resource.RemoteCover = cover.Url;
                }

                yield return resource;
            }
        }
    }
}
