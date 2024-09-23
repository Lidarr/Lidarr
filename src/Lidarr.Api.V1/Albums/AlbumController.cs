using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Albums
{
    [V1ApiController]
    public class AlbumController : AlbumControllerWithSignalR,
        IHandle<AlbumGrabbedEvent>,
        IHandle<AlbumEditedEvent>,
        IHandle<AlbumUpdatedEvent>,
        IHandle<AlbumImportedEvent>,
        IHandle<TrackImportedEvent>,
        IHandle<TrackFileDeletedEvent>
    {
        protected readonly IArtistService _artistService;
        protected readonly IReleaseService _releaseService;
        protected readonly IAddAlbumService _addAlbumService;

        public AlbumController(IArtistService artistService,
                           IAlbumService albumService,
                           IAddAlbumService addAlbumService,
                           IReleaseService releaseService,
                           IArtistStatisticsService artistStatisticsService,
                           IMapCoversToLocal coverMapper,
                           IUpgradableSpecification upgradableSpecification,
                           IBroadcastSignalRMessage signalRBroadcaster,
                           RootFolderValidator rootFolderValidator,
                           MappedNetworkDriveValidator mappedNetworkDriveValidator,
                           ArtistAncestorValidator artistAncestorValidator,
                           RecycleBinValidator recycleBinValidator,
                           SystemFolderValidator systemFolderValidator,
                           AlbumExistsValidator albumExistsValidator,
                           RootFolderExistsValidator rootFolderExistsValidator,
                           QualityProfileExistsValidator qualityProfileExistsValidator,
                           MetadataProfileExistsValidator metadataProfileExistsValidator)

        : base(albumService, artistStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _artistService = artistService;
            _releaseService = releaseService;
            _addAlbumService = addAlbumService;

            PostValidator.RuleFor(s => s.ForeignAlbumId).NotEmpty().SetValidator(albumExistsValidator);
            PostValidator.RuleFor(s => s.Artist).NotNull();
            PostValidator.RuleFor(s => s.Artist.ForeignArtistId).NotEmpty().When(s => s.Artist != null);

            PostValidator.RuleFor(s => s.Artist.QualityProfileId).Cascade(CascadeMode.Stop)
                .ValidId()
                .SetValidator(qualityProfileExistsValidator)
                .When(s => s.Artist != null);

            PostValidator.RuleFor(s => s.Artist.MetadataProfileId).Cascade(CascadeMode.Stop)
                .ValidId()
                .SetValidator(metadataProfileExistsValidator)
                .When(s => s.Artist != null);

            PostValidator.RuleFor(s => s.Artist.Path).Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetValidator(rootFolderValidator)
                .SetValidator(mappedNetworkDriveValidator)
                .SetValidator(artistAncestorValidator)
                .SetValidator(recycleBinValidator)
                .SetValidator(systemFolderValidator)
                .When(s => s.Artist != null && s.Artist.Path.IsNotNullOrWhiteSpace());

            PostValidator.RuleFor(s => s.Artist.RootFolderPath)
                .IsValidPath()
                .SetValidator(rootFolderExistsValidator)
                .When(s => s.Artist != null && s.Artist.Path.IsNullOrWhiteSpace());
        }

        [HttpGet]
        public List<AlbumResource> GetAlbums([FromQuery]int? artistId,
            [FromQuery] List<int> albumIds,
            [FromQuery]string foreignAlbumId,
            [FromQuery]bool includeAllArtistAlbums = false)
        {
            if (!artistId.HasValue && !albumIds.Any() && foreignAlbumId.IsNullOrWhiteSpace())
            {
                var albums = _albumService.GetAllAlbums();

                var artists = _artistService.GetAllArtists().ToDictionary(x => x.ArtistMetadataId);
                var releases = _releaseService.GetAllReleases().GroupBy(x => x.AlbumId).ToDictionary(x => x.Key, y => y.ToList());

                foreach (var album in albums)
                {
                    if (!artists.TryGetValue(album.ArtistMetadataId, out var albumArtist))
                    {
                        continue;
                    }

                    album.Artist = albumArtist;
                    album.AlbumReleases = releases.TryGetValue(album.Id, out var albumReleases) ? albumReleases : new List<AlbumRelease>();
                }

                return MapToResource(albums, false);
            }

            if (artistId.HasValue)
            {
                return MapToResource(_albumService.GetAlbumsByArtist(artistId.Value), false);
            }

            if (foreignAlbumId.IsNotNullOrWhiteSpace())
            {
                var album = _albumService.FindById(foreignAlbumId);

                if (album == null)
                {
                    return MapToResource(new List<Album>(), false);
                }

                if (includeAllArtistAlbums)
                {
                    return MapToResource(_albumService.GetAlbumsByArtist(album.ArtistId), false);
                }
                else
                {
                    return MapToResource(new List<Album> { album }, false);
                }
            }

            return MapToResource(_albumService.GetAlbums(albumIds), false);
        }

        [RestPostById]
        public ActionResult<AlbumResource> AddAlbum(AlbumResource albumResource)
        {
            var album = _addAlbumService.AddAlbum(albumResource.ToModel());

            return Created(album.Id);
        }

        [RestPutById]
        public ActionResult<AlbumResource> UpdateAlbum(AlbumResource albumResource)
        {
            var album = _albumService.GetAlbum(albumResource.Id);

            var model = albumResource.ToModel(album);

            _albumService.UpdateAlbum(model);
            _releaseService.UpdateMany(model.AlbumReleases.Value);

            BroadcastResourceChange(ModelAction.Updated, model.Id);

            return Accepted(model.Id);
        }

        [RestDeleteById]
        public void DeleteAlbum(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _albumService.DeleteAlbum(id, deleteFiles, addImportListExclusion);
        }

        [HttpPut("monitor")]
        public IActionResult SetAlbumsMonitored([FromBody]AlbumsMonitoredResource resource)
        {
            _albumService.SetMonitored(resource.AlbumIds, resource.Monitored);

            return Accepted(MapToResource(_albumService.GetAlbums(resource.AlbumIds), false));
        }

        [NonAction]
        public void Handle(AlbumGrabbedEvent message)
        {
            foreach (var album in message.Album.Albums)
            {
                var resource = album.ToResource();
                resource.Grabbed = true;

                BroadcastResourceChange(ModelAction.Updated, resource);
            }
        }

        [NonAction]
        public void Handle(AlbumEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Album, true));
        }

        [NonAction]
        public void Handle(AlbumUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Album, true));
        }

        [NonAction]
        public void Handle(AlbumDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Album.ToResource());
        }

        [NonAction]
        public void Handle(AlbumImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Album, true));
        }

        [NonAction]
        public void Handle(TrackImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.TrackInfo.Album.ToResource());
        }

        [NonAction]
        public void Handle(TrackFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.TrackFile.Album.Value, true));
        }
    }
}
