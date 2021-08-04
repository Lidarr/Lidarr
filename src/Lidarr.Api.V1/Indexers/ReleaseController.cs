using System;
using System.Collections.Generic;
using FluentValidation;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Lidarr.Api.V1.Indexers
{
    [V1ApiController]
    public class ReleaseController : ReleaseControllerBase
    {
        private readonly IAlbumService _albumService;
        private readonly IArtistService _artistService;
        private readonly IFetchAndParseRss _rssFetcherAndParser;
        private readonly ISearchForNzb _nzbSearchService;
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IPrioritizeDownloadDecision _prioritizeDownloadDecision;
        private readonly IParsingService _parsingService;
        private readonly IDownloadService _downloadService;
        private readonly Logger _logger;

        private readonly ICached<RemoteAlbum> _remoteAlbumCache;

        public ReleaseController(IAlbumService albumService,
                             IArtistService artistService,
                             IFetchAndParseRss rssFetcherAndParser,
                             ISearchForNzb nzbSearchService,
                             IMakeDownloadDecision downloadDecisionMaker,
                             IPrioritizeDownloadDecision prioritizeDownloadDecision,
                             IParsingService parsingService,
                             IDownloadService downloadService,
                             ICacheManager cacheManager,
                             Logger logger)
        {
            _albumService = albumService;
            _artistService = artistService;
            _rssFetcherAndParser = rssFetcherAndParser;
            _nzbSearchService = nzbSearchService;
            _downloadDecisionMaker = downloadDecisionMaker;
            _prioritizeDownloadDecision = prioritizeDownloadDecision;
            _parsingService = parsingService;
            _downloadService = downloadService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteAlbumCache = cacheManager.GetCache<RemoteAlbum>(GetType(), "remoteAlbums");
        }

        [HttpPost]
        public ActionResult<ReleaseResource> Create(ReleaseResource release)
        {
            ValidateResource(release);

            var remoteAlbum = _remoteAlbumCache.Find(GetCacheKey(release));

            if (remoteAlbum == null)
            {
                _logger.Debug("Couldn't find requested release in cache, cache timeout probably expired.");

                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Couldn't find requested release in cache, try searching again");
            }

            try
            {
                if (remoteAlbum.Artist == null)
                {
                    if (release.AlbumId.HasValue)
                    {
                        var album = _albumService.GetAlbum(release.AlbumId.Value);

                        remoteAlbum.Artist = _artistService.GetArtist(album.ArtistId);
                        remoteAlbum.Albums = new List<Album> { album };
                    }
                    else if (release.ArtistId.HasValue)
                    {
                        var artist = _artistService.GetArtist(release.ArtistId.Value);
                        var albums = _parsingService.GetAlbums(remoteAlbum.ParsedAlbumInfo, artist);

                        if (albums.Empty())
                        {
                            throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to parse albums in the release");
                        }

                        remoteAlbum.Artist = artist;
                        remoteAlbum.Albums = albums;
                    }
                    else
                    {
                        throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to find matching artist and albums");
                    }
                }
                else if (remoteAlbum.Albums.Empty())
                {
                    var albums = _parsingService.GetAlbums(remoteAlbum.ParsedAlbumInfo, remoteAlbum.Artist);

                    if (albums.Empty() && release.AlbumId.HasValue)
                    {
                        var album = _albumService.GetAlbum(release.AlbumId.Value);

                        albums = new List<Album> { album };
                    }

                    remoteAlbum.Albums = albums;
                }

                if (remoteAlbum.Albums.Empty())
                {
                    throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to parse albums in the release");
                }

                _downloadService.DownloadReport(remoteAlbum);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, ex.Message);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Getting release from indexer failed");
            }

            return Ok(release);
        }

        [HttpGet]
        public List<ReleaseResource> GetReleases(int? albumId, int? artistId)
        {
            if (albumId.HasValue)
            {
                return GetAlbumReleases(int.Parse(Request.Query["albumId"]));
            }

            if (artistId.HasValue)
            {
                return GetArtistReleases(int.Parse(Request.Query["artistId"]));
            }

            return GetRss();
        }

        private List<ReleaseResource> GetAlbumReleases(int albumId)
        {
            try
            {
                var decisions = _nzbSearchService.AlbumSearch(albumId, true, true, true);
                var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

                return MapDecisions(prioritizedDecisions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Album search failed");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private List<ReleaseResource> GetArtistReleases(int artistId)
        {
            try
            {
                var decisions = _nzbSearchService.ArtistSearch(artistId, false, true, true);
                var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

                return MapDecisions(prioritizedDecisions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Artist search failed");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private List<ReleaseResource> GetRss()
        {
            var reports = _rssFetcherAndParser.Fetch();
            var decisions = _downloadDecisionMaker.GetRssDecision(reports);
            var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

            return MapDecisions(prioritizedDecisions);
        }

        protected override ReleaseResource MapDecision(DownloadDecision decision, int initialWeight)
        {
            var resource = base.MapDecision(decision, initialWeight);
            _remoteAlbumCache.Set(GetCacheKey(resource), decision.RemoteAlbum, TimeSpan.FromMinutes(30));
            return resource;
        }

        private string GetCacheKey(ReleaseResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
