﻿using System.Collections.Generic;
using System.Linq;
using NzbDrone.Api.REST;

namespace NzbDrone.Api.Tracks
{
    public class RenameTrackResource : RestResource
    {
        public int ArtistId { get; set; }
        public int AlbumId { get; set; }
        public List<int> TrackNumbers { get; set; }
        public int TrackFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }

    public static class RenameTrackResourceMapper
    {
        public static RenameTrackResource ToResource(this Core.MediaFiles.RenameTrackFilePreview model)
        {
            if (model == null) return null;

            return new RenameTrackResource
            {
                ArtistId = model.ArtistId,
                AlbumId = model.AlbumId,
                TrackNumbers = model.TrackNumbers.ToList(),
                TrackFileId = model.TrackFileId,
                ExistingPath = model.ExistingPath,
                NewPath = model.NewPath
            };
        }

        public static List<RenameTrackResource> ToResource(this IEnumerable<Core.MediaFiles.RenameTrackFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
