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

        public override string GetFilenameAfterMove(Artist artist, TrackFile trackFile, MetadataFile metadataFile)
        {
            var trackFilePath = Path.Combine(artist.Path, trackFile.RelativePath);

            if (metadataFile.Type == MetadataType.TrackImage)
            {
                return GetTrackImageFilename(trackFilePath);
            }

            if (metadataFile.Type == MetadataType.TrackMetadata)
            {
                return GetTrackMetadataFilename(trackFilePath);
            }

            _logger.Debug("Unknown track file metadata: {0}", metadataFile.RelativePath);
            return Path.Combine(artist.Path, metadataFile.RelativePath);
        }

        public override MetadataFile FindMetadataFile(Artist artist, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null) return null;
            var parentdir = Directory.GetParent(path);

            var metadata = new MetadataFile
                           {
                               ArtistId = artist.Id,
                               Consumer = GetType().Name,
                               RelativePath = artist.Path.GetRelativePath(path)
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

        public override MetadataFileResult ArtistMetadata(Artist artist)
        {
            //Artist metadata is not supported
            return null;
        }

        public override MetadataFileResult AlbumMetadata(Artist artist, Album album)
        {
            return null;
        }

        public override MetadataFileResult TrackMetadata(Artist artist, TrackFile trackFile)
        {
            if (!Settings.EpisodeMetadata)
            {
                return null;
            }
            
            _logger.Debug("Generating Track Metadata for: {0}", trackFile.RelativePath);

            var xmlResult = string.Empty;
            foreach (var track in trackFile.Tracks.Value)
            {
                var sb = new StringBuilder();
                var xws = new XmlWriterSettings();
                xws.OmitXmlDeclaration = true;
                xws.Indent = false;

                using (var xw = XmlWriter.Create(sb, xws))
                {
                    var doc = new XDocument();

                    var details = new XElement("video");
                    details.Add(new XElement("title", string.Format("{0} - {1} - {2}", artist.Name, track.TrackNumber, track.Title)));
                    details.Add(new XElement("genre", string.Join(" / ", artist.Genres)));
                    var actors = string.Join(" , ", artist.Members.ConvertAll(c => c.Name + " - " + c.Instrument).GetRange(0, Math.Min(3, artist.Members.Count)));
                    details.Add(new XElement("actors", actors));

                    doc.Add(details);
                    doc.Save(xw);

                    xmlResult += doc.ToString();
                    xmlResult += Environment.NewLine;
                }
            }

            return new MetadataFileResult(GetTrackMetadataFilename(trackFile.RelativePath), xmlResult.Trim(Environment.NewLine.ToCharArray()));
        }

        public override List<ImageFileResult> ArtistImages(Artist artist)
        {
            var image = artist.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? artist.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable Artist image for artist {0}.", artist.Name);
                return null;
            }

            var source = _mediaCoverService.GetCoverPath(artist.Id, image.CoverType);
            var destination = Path.GetFileName(artist.Path) + Path.GetExtension(source);

            return new List<ImageFileResult>{ new ImageFileResult(destination, source) };
        }

        public override List<ImageFileResult> AlbumImages(Artist artist, Album album)
        {
            var seasonFolders = GetAlbumFolders(artist);

            string seasonFolder;
            if (!seasonFolders.TryGetValue(album.ArtistId, out seasonFolder))
            {
                _logger.Trace("Failed to find season folder for series {0}, season {1}.", artist.Name, album.Title);
                return new List<ImageFileResult>();
            }

            //Roksbox only supports one season image, so first of all try for poster otherwise just use whatever is first in the collection
            var image = album.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? album.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable season image for series {0}, season {1}.", artist.Name, album.Title);
                return new List<ImageFileResult>();
            }

            var filename = Path.GetFileName(seasonFolder) + ".jpg";
            var path = artist.Path.GetRelativePath(Path.Combine(artist.Path, seasonFolder, filename));

            return new List<ImageFileResult> { new ImageFileResult(path, image.Url) };
        }

        public override List<ImageFileResult> TrackImages(Artist artist, TrackFile trackFile)
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

        private string GetTrackMetadataFilename(string trackFilePath)
        {
            return Path.ChangeExtension(trackFilePath, "xml");
        }

        private string GetTrackImageFilename(string trackFilePath)
        {
            return Path.ChangeExtension(trackFilePath, "jpg");
        }

        private Dictionary<int, string> GetAlbumFolders(Artist artist)
        {
            var seasonFolderMap = new Dictionary<int, string>();

            foreach (var folder in _diskProvider.GetDirectories(artist.Path))
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
                            _logger.Debug("Failed to parse season number from {0} for artist {1}.", folder, artist.Name);
                        }
                    }
                }
                else
                {
                    _logger.Debug("Rejecting folder {0} for artist {1}.", Path.GetDirectoryName(folder), artist.Name);
                }
            }

            return seasonFolderMap;
        }
    }
}
