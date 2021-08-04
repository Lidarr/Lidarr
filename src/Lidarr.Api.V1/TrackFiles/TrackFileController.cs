using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.SignalR;
using BadRequestException = Lidarr.Http.REST.BadRequestException;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Lidarr.Api.V1.TrackFiles
{
    [V1ApiController]
    public class TrackFileController : RestControllerWithSignalR<TrackFileResource, TrackFile>,
                                 IHandle<TrackFileAddedEvent>,
                                 IHandle<TrackFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IAudioTagService _audioTagService;
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public TrackFileController(IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IAudioTagService audioTagService,
                               IArtistService artistService,
                               IAlbumService albumService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _audioTagService = audioTagService;
            _artistService = artistService;
            _albumService = albumService;
            _upgradableSpecification = upgradableSpecification;
        }

        private TrackFileResource MapToResource(TrackFile trackFile)
        {
            if (trackFile.AlbumId > 0 && trackFile.Artist != null && trackFile.Artist.Value != null)
            {
                return trackFile.ToResource(trackFile.Artist.Value, _upgradableSpecification);
            }
            else
            {
                return trackFile.ToResource();
            }
        }

        public override TrackFileResource GetResourceById(int id)
        {
            var resource = MapToResource(_mediaFileService.Get(id));
            resource.AudioTags = _audioTagService.ReadTags(resource.Path);
            return resource;
        }

        [HttpGet]
        public List<TrackFileResource> GetTrackFiles(int? artistId, [FromQuery] List<int> trackFileIds, [FromQuery(Name = "albumId")] List<int> albumIds, bool? unmapped)
        {
            if (!artistId.HasValue && !trackFileIds.Any() && !albumIds.Any() && !unmapped.HasValue)
            {
                throw new BadRequestException("artistId, albumId, trackFileIds or unmapped must be provided");
            }

            if (unmapped.HasValue && unmapped.Value)
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (artistId.HasValue && !albumIds.Any())
            {
                var artist = _artistService.GetArtist(artistId.Value);

                return _mediaFileService.GetFilesByArtist(artistId.Value).ConvertAll(f => f.ToResource(artist, _upgradableSpecification));
            }

            if (albumIds.Any())
            {
                var result = new List<TrackFileResource>();
                foreach (var albumId in albumIds)
                {
                    var album = _albumService.GetAlbum(albumId);
                    var albumArtist = _artistService.GetArtist(album.ArtistId);
                    result.AddRange(_mediaFileService.GetFilesByAlbum(album.Id).ConvertAll(f => f.ToResource(albumArtist, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                // trackfiles will come back with the artist already populated
                var trackFiles = _mediaFileService.Get(trackFileIds);
                return trackFiles.ConvertAll(e => MapToResource(e));
            }
        }

        [RestPutById]
        public ActionResult<TrackFileResource> SetQuality([FromBody] TrackFileResource trackFileResource)
        {
            var trackFile = _mediaFileService.Get(trackFileResource.Id);
            trackFile.Quality = trackFileResource.Quality;
            _mediaFileService.Update(trackFile);
            return Accepted(trackFile.Id);
        }

        [HttpPut("editor")]
        public IActionResult SetQuality([FromBody] TrackFileListResource resource)
        {
            var trackFiles = _mediaFileService.Get(resource.TrackFileIds);

            foreach (var trackFile in trackFiles)
            {
                if (resource.Quality != null)
                {
                    trackFile.Quality = resource.Quality;
                }
            }

            _mediaFileService.Update(trackFiles);

            return Accepted(trackFiles.ConvertAll(f => f.ToResource(trackFiles.First().Artist.Value, _upgradableSpecification)));
        }

        [RestDeleteById]
        public void DeleteTrackFile(int id)
        {
            var trackFile = _mediaFileService.Get(id);

            if (trackFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Track file not found");
            }

            if (trackFile.AlbumId > 0 && trackFile.Artist != null && trackFile.Artist.Value != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(trackFile.Artist.Value, trackFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteTrackFile(trackFile, "Unmapped_Files");
            }
        }

        [HttpDelete("bulk")]
        public IActionResult DeleteTrackFiles([FromBody] TrackFileListResource resource)
        {
            var trackFiles = _mediaFileService.Get(resource.TrackFileIds);
            var artist = trackFiles.First().Artist.Value;

            foreach (var trackFile in trackFiles)
            {
                _mediaFileDeletionService.DeleteTrackFile(artist, trackFile);
            }

            return Ok();
        }

        [NonAction]
        public void Handle(TrackFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.TrackFile));
        }

        [NonAction]
        public void Handle(TrackFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, MapToResource(message.TrackFile));
        }
    }
}
