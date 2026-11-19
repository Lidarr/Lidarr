using System;
using System.Collections.Generic;
using Lidarr.Api.V1.Artist;
using Lidarr.Api.V1.CustomFormats;
using Lidarr.Http.REST;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Qualities;

namespace Lidarr.Api.V1.Blocklist
{
    public class BlocklistResource : RestResource
    {
        public int ArtistId { get; set; }
        public List<int> AlbumIds { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public DateTime Date { get; set; }
        public string Protocol { get; set; }
        public string Indexer { get; set; }
        public string Message { get; set; }

        public ArtistResource Artist { get; set; }
    }

    public static class BlocklistResourceMapper
    {
        public static BlocklistResource MapToResource(this NzbDrone.Core.Blocklisting.Blocklist model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            return new BlocklistResource
            {
                Id = model.Id,

                ArtistId = model.ArtistId,
                AlbumIds = model.AlbumIds,
                SourceTitle = model.SourceTitle,
                Quality = model.Quality,
                CustomFormats = formatCalculator.ParseCustomFormat(model, model.Artist).ToResource(false),
                Date = model.Date,
                Protocol = model.Protocol,
                Indexer = model.Indexer,
                Message = model.Message,

                Artist = model.Artist.ToResource()
            };
        }
    }
}
