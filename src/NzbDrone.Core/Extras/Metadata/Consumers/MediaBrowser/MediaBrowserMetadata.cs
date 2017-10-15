using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata.Consumers.MediaBrowser
{
    public class MediaBrowserMetadata : MetadataBase<MediaBrowserMetadataSettings>
    {
        private readonly Logger _logger;

        public MediaBrowserMetadata(
                            Logger logger)
        {
            _logger = logger;
        }

        public override string Name => "Emby (Legacy)";

        public override MetadataFile FindMetadataFile(Artist series, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null) return null;

            var metadata = new MetadataFile
                           {
                               ArtistId = series.Id,
                               Consumer = GetType().Name,
                               RelativePath = series.Path.GetRelativePath(path)
                           };

            if (filename.Equals("artist.xml", StringComparison.InvariantCultureIgnoreCase))
            {
                metadata.Type = MetadataType.ArtistMetadata;
                return metadata;
            }

            return null;
        }

        public override MetadataFileResult ArtistMetadata(Artist series)
        {
            if (!Settings.ArtistMetadata)
            {
                return null;
            }

            _logger.Debug("Generating artist.xml for: {0}", series.Name);
            var sb = new StringBuilder();
            var xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = false;

            using (var xw = XmlWriter.Create(sb, xws))
            {
                var tvShow = new XElement("Artist");

                tvShow.Add(new XElement("id", series.ForeignArtistId));
                tvShow.Add(new XElement("Status", series.Status));

                tvShow.Add(new XElement("Added", series.Added.ToString("MM/dd/yyyy HH:mm:ss tt"))); 
                tvShow.Add(new XElement("LockData", "false"));
                tvShow.Add(new XElement("Overview", series.Overview));
                tvShow.Add(new XElement("LocalTitle", series.Name));

                tvShow.Add(new XElement("Rating", series.Ratings.Value));
                tvShow.Add(new XElement("Genres", series.Genres.Select(genre => new XElement("Genre", genre))));

                var persons   = new XElement("Persons");

                foreach (var person in series.Members)
                {
                    persons.Add(new XElement("Person",
                        new XElement("Name", person.Name),
                        new XElement("Type", "Actor"),
                        new XElement("Role", person.Instrument)
                        ));
                }

                tvShow.Add(persons);


                var doc = new XDocument(tvShow);
                doc.Save(xw);

                _logger.Debug("Saving artist.xml for {0}", series.Name);

                return new MetadataFileResult("artist.xml", doc.ToString());
            }
        }
 
        public override MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile)
        {
            return null;
        }
            
        public override List<ImageFileResult> ArtistImages(Artist series)
        {
            return new List<ImageFileResult>();
        }

        public override List<ImageFileResult> AlbumImages(Artist series, Album season)
        {
            return new List<ImageFileResult>();
        }

        public override List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile)
        {
            return new List<ImageFileResult>();
        }

        private IEnumerable<ImageFileResult> ProcessSeriesImages(Artist series)
        {
            return new List<ImageFileResult>();
        }

        private IEnumerable<ImageFileResult> ProcessSeasonImages(Artist series, Album season)
        {
            return new List<ImageFileResult>();
        }

        private string GetEpisodeNfoFilename(string episodeFilePath)
        {
            return null;
        }

        private string GetEpisodeImageFilename(string episodeFilePath)
        {
            return null;
        }
    }
}
