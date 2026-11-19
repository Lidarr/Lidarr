using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Api.V1.Artist;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;

namespace Lidarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        [Produces("application/json")]
        public IEnumerable<SearchResource> Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            var id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Music.Artist artist)
                {
                    resource.Artist = artist.ToResource();
                    resource.ForeignId = artist.ForeignArtistId;

                    _coverMapper.ConvertToLocalUrls(resource.Artist.Id, MediaCoverEntity.Artist, resource.Artist.Images);

                    var poster = artist.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                    if (poster != null)
                    {
                        resource.Artist.RemotePoster = poster.RemoteUrl;
                    }

                    resource.Artist.Folder = _fileNameBuilder.GetArtistFolder(artist);
                }
                else if (result is NzbDrone.Core.Music.Album album)
                {
                    resource.Album = album.ToResource();
                    resource.ForeignId = album.ForeignAlbumId;

                    _coverMapper.ConvertToLocalUrls(resource.Album.Id, MediaCoverEntity.Album, resource.Album.Images);

                    var cover = album.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                    if (cover != null)
                    {
                        resource.Album.RemoteCover = cover.RemoteUrl;
                    }

                    resource.Album.Artist.Folder = _fileNameBuilder.GetArtistFolder(album.Artist);
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
