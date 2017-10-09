using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Roksbox
{
    public class RoksboxMetadata : MetadataBase<RoksboxMetadataSettings>
    {
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RoksboxMetadata(IMapCoversToLocal mediaCoverService,
                            IDiskProvider diskProvider,
                            Logger logger)
        {
            _mediaCoverService = mediaCoverService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        private static List<string> ValidCertification = new List<string> { "G", "NC-17", "PG", "PG-13", "R", "UR", "UNRATED", "NR", "TV-Y", "TV-Y7", "TV-Y7-FV", "TV-G", "TV-PG", "TV-14", "TV-MA" };
        private static readonly Regex SeasonImagesRegex = new Regex(@"^(season (?<season>\d+))|(?<specials>specials)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override string Name => "Roksbox";

        public override string GetFilenameAfterMove(Artist series, TrackFile episodeFile, MetadataFile metadataFile)
        {
            var episodeFilePath = Path.Combine(series.Path, episodeFile.RelativePath);

            if (metadataFile.Type == MetadataType.TrackImage)
            {
                return GetEpisodeImageFilename(episodeFilePath);
            }

            if (metadataFile.Type == MetadataType.TrackMetadata)
            {
                return GetEpisodeMetadataFilename(episodeFilePath);
            }

            _logger.Debug("Unknown episode file metadata: {0}", metadataFile.RelativePath);
            return Path.Combine(series.Path, metadataFile.RelativePath);
        }

        public override MetadataFile FindMetadataFile(Artist series, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null) return null;
            var parentdir = Directory.GetParent(path);

            var metadata = new MetadataFile
                           {
                               ArtistId = series.Id,
                               Consumer = GetType().Name,
                               RelativePath = series.Path.GetRelativePath(path)
                           };

            //Series and season images are both named folder.jpg, only season ones sit in season folders
            if (Path.GetFileNameWithoutExtension(filename).Equals(parentdir.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var seasonMatch = SeasonImagesRegex.Match(parentdir.Name);

                if (seasonMatch.Success)
                {
                    metadata.Type = MetadataType.AlbumImage;

                    if (seasonMatch.Groups["specials"].Success)
                    {
                        metadata.AlbumId = 0;
                    }

                    else
                    {
                        metadata.AlbumId = Convert.ToInt32(seasonMatch.Groups["season"].Value);
                    }

                    return metadata;
                }

                metadata.Type = MetadataType.ArtistImage;
                return metadata;
            }

            var parseResult = Parser.Parser.ParseTitle(filename);

            if (parseResult != null &&
                !parseResult.FullSeason)
            {
                var extension = Path.GetExtension(filename).ToLowerInvariant();

                if (extension == ".xml")
                {
                    metadata.Type = MetadataType.TrackMetadata;
                    return metadata;
                }

                if (extension == ".jpg")
                {
                    if (!Path.GetFileNameWithoutExtension(filename).EndsWith("-thumb"))
                    {
                        metadata.Type = MetadataType.TrackImage;
                        return metadata;
                    }
                }                
            }

            return null;
        }

        public override MetadataFileResult SeriesMetadata(Artist series)
        {
            //Series metadata is not supported
            return null;
        }

        public override MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile)
        {
            if (!Settings.EpisodeMetadata)
            {
                return null;
            }
            
            _logger.Debug("Generating Episode Metadata for: {0}", episodeFile.RelativePath);

            var xmlResult = string.Empty;
            foreach (var episode in episodeFile.Tracks.Value)
            {
                var sb = new StringBuilder();
                var xws = new XmlWriterSettings();
                xws.OmitXmlDeclaration = true;
                xws.Indent = false;

                using (var xw = XmlWriter.Create(sb, xws))
                {
                    var doc = new XDocument();

                    var details = new XElement("video");
                    details.Add(new XElement("title", string.Format("{0} - {1} - {2}", series.Name, episode.TrackNumber, episode.Title)));
                    details.Add(new XElement("genre", string.Join(" / ", series.Genres)));
                    var actors = string.Join(" , ", series.Members.ConvertAll(c => c.Name + " - " + c.Instrument).GetRange(0, Math.Min(3, series.Members.Count)));
                    details.Add(new XElement("actors", actors));

                    doc.Add(details);
                    doc.Save(xw);

                    xmlResult += doc.ToString();
                    xmlResult += Environment.NewLine;
                }
            }

            return new MetadataFileResult(GetEpisodeMetadataFilename(episodeFile.RelativePath), xmlResult.Trim(Environment.NewLine.ToCharArray()));
        }

        public override List<ImageFileResult> SeriesImages(Artist series)
        {
            var image = series.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? series.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable Series image for series {0}.", series.Name);
                return null;
            }

            var source = _mediaCoverService.GetCoverPath(series.Id, image.CoverType);
            var destination = Path.GetFileName(series.Path) + Path.GetExtension(source);

            return new List<ImageFileResult>{ new ImageFileResult(destination, source) };
        }

        public override List<ImageFileResult> SeasonImages(Artist series, Album season)
        {
            var seasonFolders = GetSeasonFolders(series);

            string seasonFolder;
            if (!seasonFolders.TryGetValue(season.ArtistId, out seasonFolder))
            {
                _logger.Trace("Failed to find season folder for series {0}, season {1}.", series.Name, season.Title);
                return new List<ImageFileResult>();
            }

            //Roksbox only supports one season image, so first of all try for poster otherwise just use whatever is first in the collection
            var image = season.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? season.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable season image for series {0}, season {1}.", series.Name, season.Title);
                return new List<ImageFileResult>();
            }

            var filename = Path.GetFileName(seasonFolder) + ".jpg";
            var path = series.Path.GetRelativePath(Path.Combine(series.Path, seasonFolder, filename));

            return new List<ImageFileResult> { new ImageFileResult(path, image.Url) };
        }

        public override List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile)
        {
            //var screenshot = episodeFile.Tracks.Value.First().Images.SingleOrDefault(i => i.CoverType == MediaCoverTypes.Screenshot);

            //if (screenshot == null)
            //{
            //    _logger.Trace("Episode screenshot not available");
            //    return new List<ImageFileResult>();
            //}

            //return new List<ImageFileResult> {new ImageFileResult(GetEpisodeImageFilename(episodeFile.RelativePath), screenshot.Url)};
            return new List<ImageFileResult>();
        }

        private string GetEpisodeMetadataFilename(string episodeFilePath)
        {
            return Path.ChangeExtension(episodeFilePath, "xml");
        }

        private string GetEpisodeImageFilename(string episodeFilePath)
        {
            return Path.ChangeExtension(episodeFilePath, "jpg");
        }

        private Dictionary<int, string> GetSeasonFolders(Artist series)
        {
            var seasonFolderMap = new Dictionary<int, string>();

            foreach (var folder in _diskProvider.GetDirectories(series.Path))
            {
                var directoryinfo = new DirectoryInfo(folder);
                var seasonMatch = SeasonImagesRegex.Match(directoryinfo.Name);

                if (seasonMatch.Success)
                {
                    var seasonNumber = seasonMatch.Groups["season"].Value;

                    if (seasonNumber.Contains("specials"))
                    {
                        seasonFolderMap[0] = folder;
                    }
                    else
                    {
                        int matchedSeason;
                        if (int.TryParse(seasonNumber, out matchedSeason))
                        {
                            seasonFolderMap[matchedSeason] = folder;
                        }
                        else
                        {
                            _logger.Debug("Failed to parse season number from {0} for series {1}.", folder, series.Name);
                        }
                    }
                }
                else
                {
                    _logger.Debug("Rejecting folder {0} for series {1}.", Path.GetDirectoryName(folder), series.Name);
                }
            }

            return seasonFolderMap;
        }
    }
}
