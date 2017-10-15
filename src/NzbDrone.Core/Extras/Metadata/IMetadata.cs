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
        MetadataFileResult ArtistMetadata(Artist series);
        MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile);
        List<ImageFileResult> ArtistImages(Artist series);
        List<ImageFileResult> AlbumImages(Artist series, Album season);
        List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile);
    }
}
