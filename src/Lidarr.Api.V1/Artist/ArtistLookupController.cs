using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;

namespace Lidarr.Api.V1.Artist
{
    [V1ApiController("artist/lookup")]
    public class ArtistLookupController : Controller
    {
        private readonly ISearchForNewArtist _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public ArtistLookupController(ISearchForNewArtist searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        [Produces("application/json")]
        public IEnumerable<ArtistResource> Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewArtist(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<ArtistResource> MapToResource(IEnumerable<NzbDrone.Core.Music.Artist> artist)
        {
            foreach (var currentArtist in artist)
            {
                var resource = currentArtist.ToResource();

                _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Artist, resource.Images);

                var poster = currentArtist.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                resource.Folder = _fileNameBuilder.GetArtistFolder(currentArtist);

                yield return resource;
            }
        }
    }
}
