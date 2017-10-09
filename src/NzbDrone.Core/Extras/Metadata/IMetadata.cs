using System.Collections.Generic;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Artist series, TrackFile episodeFile, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Artist series, string path);
        MetadataFileResult SeriesMetadata(Artist series);
        MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile);
        List<ImageFileResult> SeriesImages(Artist series);
        List<ImageFileResult> SeasonImages(Artist series, Album season);
        List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile);
    }
}
