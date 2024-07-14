using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;

namespace Lidarr.Api.V1.Artist
{
    [V1ApiController("artist/editor")]
    public class ArtistEditorController : Controller
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IManageCommandQueue _commandQueueManager;

        public ArtistEditorController(IArtistService artistService, IAlbumService albumService, IManageCommandQueue commandQueueManager)
        {
            _artistService = artistService;
            _albumService = albumService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] ArtistEditorResource resource)
        {
            var artistToUpdate = _artistService.GetArtists(resource.ArtistIds);
            var artistToMove = new List<BulkMoveArtist>();

            foreach (var artist in artistToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    artist.Monitored = resource.Monitored.Value;
                }

                if (resource.MonitorNewItems.HasValue)
                {
                    artist.MonitorNewItems = resource.MonitorNewItems.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    artist.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.MetadataProfileId.HasValue)
                {
                    artist.MetadataProfileId = resource.MetadataProfileId.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    artist.RootFolderPath = resource.RootFolderPath;
                    artistToMove.Add(new BulkMoveArtist
                    {
                        ArtistId = artist.Id,
                        SourcePath = artist.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => artist.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => artist.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            artist.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            if (artistToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveArtistCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Artist = artistToMove,
                    MoveFiles = resource.MoveFiles
                });
            }

            var resources = _artistService.UpdateArtists(artistToUpdate, !resource.MoveFiles).ToResource();

            LinkNextPreviousAlbums(resources.ToArray());

            return Accepted(resources);
        }

        [HttpDelete]
        public object DeleteArtist([FromBody] ArtistEditorResource resource)
        {
            _artistService.DeleteArtists(resource.ArtistIds, resource.DeleteFiles, resource.AddImportListExclusion);

            return new { };
        }

        private void LinkNextPreviousAlbums(params ArtistResource[] artists)
        {
            var artistMetadataIds = artists.Select(x => x.ArtistMetadataId).Distinct().ToList();

            var nextAlbums = _albumService.GetNextAlbumsByArtistMetadataId(artistMetadataIds);
            var lastAlbums = _albumService.GetLastAlbumsByArtistMetadataId(artistMetadataIds);

            foreach (var artistResource in artists)
            {
                artistResource.NextAlbum = nextAlbums.FirstOrDefault(x => x.ArtistMetadataId == artistResource.ArtistMetadataId).ToResource();
                artistResource.LastAlbum = lastAlbums.FirstOrDefault(x => x.ArtistMetadataId == artistResource.ArtistMetadataId).ToResource();
            }
        }
    }
}
