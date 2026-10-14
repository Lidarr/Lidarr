using Lidarr.Http.REST;
using NzbDrone.Core.IndexerSearch;

namespace Lidarr.Api.V1.Config
{
    public class SearchFormatConfigResource : RestResource
    {
        public bool UseCustomSearchFormat { get; set; }
        public string AlbumSearchFormat { get; set; }
        public string ArtistSearchFormat { get; set; }
    }

    public class SearchFormatExampleResource
    {
        public string AlbumSearchExample { get; set; }
        public string ArtistSearchExample { get; set; }
    }

    public static class SearchFormatConfigResourceMapper
    {
        public static SearchFormatConfigResource ToResource(this SearchFormatConfig model)
        {
            return new SearchFormatConfigResource
            {
                Id = model.Id,
                UseCustomSearchFormat = model.UseCustomSearchFormat,
                AlbumSearchFormat = model.AlbumSearchFormat ?? "",
                ArtistSearchFormat = model.ArtistSearchFormat ?? ""
            };
        }

        public static SearchFormatConfig ToModel(this SearchFormatConfigResource resource)
        {
            return new SearchFormatConfig
            {
                Id = resource.Id,
                UseCustomSearchFormat = resource.UseCustomSearchFormat,
                AlbumSearchFormat = resource.AlbumSearchFormat,
                ArtistSearchFormat = resource.ArtistSearchFormat
            };
        }
    }
}
