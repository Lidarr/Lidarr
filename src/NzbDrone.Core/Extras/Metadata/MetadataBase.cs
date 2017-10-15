using System;
using System.Collections.Generic;
using System.IO;
using FluentValidation.Results;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata
{
    public abstract class MetadataBase<TSettings> : IMetadata where TSettings : IProviderConfig, new()
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        public ValidationResult Test()
        {
            return new ValidationResult();
        }

        public virtual string GetFilenameAfterMove(Artist series, TrackFile episodeFile, MetadataFile metadataFile)
        {
            var existingFilename = Path.Combine(series.Path, metadataFile.RelativePath);
            var extension = Path.GetExtension(existingFilename).TrimStart('.');
            var newFileName = Path.ChangeExtension(Path.Combine(series.Path, episodeFile.RelativePath), extension);

            return newFileName;
        }

        public abstract MetadataFile FindMetadataFile(Artist series, string path);

        public abstract MetadataFileResult ArtistMetadata(Artist series);
        public abstract MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile);
        public abstract List<ImageFileResult> ArtistImages(Artist series);
        public abstract List<ImageFileResult> AlbumImages(Artist series, Album season);
        public abstract List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile);

        public virtual object RequestAction(string action, IDictionary<string, string> query) { return null; }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
