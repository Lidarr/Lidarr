using NzbDrone.Core.Organizer;

namespace Lidarr.Api.V1.Config
{
    public class NamingExampleResource
    {
        public string SingleTrackExample { get; set; }
        public string ArtistFolderExample { get; set; }
        public string AlbumFolderExample { get; set; }
    }

    public static class NamingConfigResourceMapper
    {
        public static NamingConfigResource ToResource(this NamingConfig model)
        {
            return new NamingConfigResource
            {
                Id = model.Id,

                RenameTracks = model.RenameTracks,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters,
                StandardTrackFormat = model.StandardTrackFormat,
                ArtistFolderFormat = model.ArtistFolderFormat
                //IncludeSeriesTitle
                //IncludeEpisodeTitle
                //IncludeQuality
                //ReplaceSpaces
                //Separator
                //NumberStyle
            };
        }

        public static NamingConfig ToModel(this NamingConfigResource resource)
        {
            return new NamingConfig
            {
                Id = resource.Id,

                RenameTracks = resource.RenameTracks,
                ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
                StandardTrackFormat = resource.StandardTrackFormat,

                ArtistFolderFormat = resource.ArtistFolderFormat
            };
        }
    }
}
