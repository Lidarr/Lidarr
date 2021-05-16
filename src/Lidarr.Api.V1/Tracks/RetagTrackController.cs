using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;

namespace Lidarr.Api.V1.Tracks
{
    [V1ApiController("retag")]
    public class RetagTrackController : Controller
    {
        private readonly IAudioTagService _audioTagService;

        public RetagTrackController(IAudioTagService audioTagService)
        {
            _audioTagService = audioTagService;
        }

        [HttpGet]
        public List<RetagTrackResource> GetTracks(int artistId, int? albumId)
        {
            if (albumId.HasValue)
            {
                return _audioTagService.GetRetagPreviewsByAlbum(albumId.Value).Where(x => x.Changes.Any()).ToResource();
            }

            return _audioTagService.GetRetagPreviewsByArtist(artistId).Where(x => x.Changes.Any()).ToResource();
        }
    }
}
