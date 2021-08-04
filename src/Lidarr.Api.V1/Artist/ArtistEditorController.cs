using System.Collections.Generic;
using System.Linq;
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
        private readonly IManageCommandQueue _commandQueueManager;

        public ArtistEditorController(IArtistService artistService, IManageCommandQueue commandQueueManager)
        {
            _artistService = artistService;
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

            return Accepted(_artistService.UpdateArtists(artistToUpdate, !resource.MoveFiles).ToResource());
        }

        [HttpDelete]
        public object DeleteArtist([FromBody] ArtistEditorResource resource)
        {
            _artistService.DeleteArtists(resource.ArtistIds, resource.DeleteFiles);

            return new object();
        }
    }
}
