using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Download.Clients.Flood.Models
{
    public enum AdditionalTags
    {
        [FieldOption(Hint = "Elvis Presley")]
        Artist = 0,

        [FieldOption(Hint = "MP3-320")]
        Quality = 1,

        [FieldOption(Hint = "Example-Raws")]
        ReleaseGroup = 2,

        [FieldOption(Hint = "2020")]
        Year = 3,

        [FieldOption(Hint = "Torznab")]
        Indexer = 4,
    }
}
