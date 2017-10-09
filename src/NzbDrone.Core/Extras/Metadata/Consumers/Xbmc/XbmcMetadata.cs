using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Xbmc
{
    public class XbmcMetadata : MetadataBase<XbmcMetadataSettings>
    {
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public XbmcMetadata(IMapCoversToLocal mediaCoverService,
                            Logger logger)
        {
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        private static readonly Regex SeriesImagesRegex = new Regex(@"^(?<type>poster|banner|fanart)\.(?:png|jpg)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SeasonImagesRegex = new Regex(@"^season(?<season>\d{2,}|-all|-specials)-(?<type>poster|banner|fanart)\.(?:png|jpg)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex EpisodeImageRegex = new Regex(@"-thumb\.(?:png|jpg)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override string Name => "Kodi (XBMC) / Emby";

        public override string GetFilenameAfterMove(Artist artist, TrackFile trackFile, MetadataFile metadataFile)
        {
            var episodeFilePath = Path.Combine(artist.Path, trackFile.RelativePath);

            if (metadataFile.Type == MetadataType.TrackImage)
            {
                return GetEpisodeImageFilename(episodeFilePath);
            }

            if (metadataFile.Type == MetadataType.TrackMetadata)
            {
                return GetEpisodeMetadataFilename(episodeFilePath);
            }

            _logger.Debug("Unknown episode file metadata: {0}", metadataFile.RelativePath);
            return Path.Combine(artist.Path, metadataFile.RelativePath);
        }

        public override MetadataFile FindMetadataFile(Artist artist, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null) return null;

            var metadata = new MetadataFile
            {
                ArtistId = artist.Id,
                Consumer = GetType().Name,
                RelativePath = artist.Path.GetRelativePath(path)
            };

            if (SeriesImagesRegex.IsMatch(filename))
            {
                metadata.Type = MetadataType.ArtistImage;
                return metadata;
            }

            var seasonMatch = SeasonImagesRegex.Match(filename);

            if (seasonMatch.Success)
            {
                metadata.Type = MetadataType.AlbumImage;

                var seasonNumberMatch = seasonMatch.Groups["season"].Value;
                int seasonNumber;

                if (seasonNumberMatch.Contains("specials"))
                {
                    metadata.AlbumId = 0;
                }

                else if (int.TryParse(seasonNumberMatch, out seasonNumber))
                {
                    metadata.AlbumId = seasonNumber;
                }

                else
                {
                    return null;
                }

                return metadata;
            }

            if (EpisodeImageRegex.IsMatch(filename))
            {
                metadata.Type = MetadataType.TrackImage;
                return metadata;
            }

            if (filename.Equals("tvshow.nfo", StringComparison.InvariantCultureIgnoreCase))
            {
                metadata.Type = MetadataType.ArtistMetadata;
                return metadata;
            }

            var parseResult = Parser.Parser.ParseTitle(filename);

            if (parseResult != null &&
                !parseResult.FullSeason &&
                Path.GetExtension(filename) == ".nfo")
            {
                metadata.Type = MetadataType.TrackMetadata;
                return metadata;
            }

            return null;
        }

        public override MetadataFileResult SeriesMetadata(Artist series)
        {
            if (!Settings.SeriesMetadata)
            {
                return null;
            }

            _logger.Debug("Generating tvshow.nfo for: {0}", series.Name);
            var sb = new StringBuilder();
            var xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = false;

            var episodeGuideUrl = string.Format("http://www.thetvdb.com/api/1D62F2F90030C444/series/{0}/all/en.zip", series.ForeignArtistId);

            using (var xw = XmlWriter.Create(sb, xws))
            {
                var tvShow = new XElement("tvshow");

                tvShow.Add(new XElement("title", series.Name));

                if (series.Ratings != null && series.Ratings.Votes > 0)
                {
                    tvShow.Add(new XElement("rating", series.Ratings.Value));
                }

                tvShow.Add(new XElement("plot", series.Overview));
                tvShow.Add(new XElement("episodeguide", new XElement("url", episodeGuideUrl)));
                tvShow.Add(new XElement("episodeguideurl", episodeGuideUrl));
                tvShow.Add(new XElement("id", series.ForeignArtistId));

                foreach (var genre in series.Genres)
                {
                    tvShow.Add(new XElement("genre", genre));
                }
                

                foreach (var actor in series.Members)
                {
                    var xmlActor = new XElement("actor",
                        new XElement("name", actor.Name),
                        new XElement("role", actor.Instrument));

                    if (actor.Images.Any())
                    {
                        xmlActor.Add(new XElement("thumb", actor.Images.First().Url));
                    }

                    tvShow.Add(xmlActor);
                }

                var doc = new XDocument(tvShow);
                doc.Save(xw);

                _logger.Debug("Saving tvshow.nfo for {0}", series.Name);

                return new MetadataFileResult("tvshow.nfo", doc.ToString());
            }
        }

        public override MetadataFileResult EpisodeMetadata(Artist series, TrackFile episodeFile)
        {
            if (!Settings.EpisodeMetadata)
            {
                return null;
            }

            _logger.Debug("Generating Episode Metadata for: {0}", Path.Combine(series.Path, episodeFile.RelativePath));

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

                    var details = new XElement("episodedetails");
                    details.Add(new XElement("title", episode.Title));
                    details.Add(new XElement("episode", episode.TrackNumber));

                    //If trakt ever gets airs before information for specials we should add set it
                    details.Add(new XElement("displayseason"));
                    details.Add(new XElement("displayepisode"));

                    details.Add(new XElement("watched", "false"));

                    if (episode.Ratings != null && episode.Ratings.Votes > 0)
                    {
                        details.Add(new XElement("rating", episode.Ratings.Value));
                    }

                    if (episodeFile.MediaInfo != null)
                    {
                        var fileInfo = new XElement("fileinfo");
                        var streamDetails = new XElement("streamdetails");

                        var video = new XElement("video");
                        video.Add(new XElement("aspect", (float)episodeFile.MediaInfo.Width / (float)episodeFile.MediaInfo.Height));
                        video.Add(new XElement("bitrate", episodeFile.MediaInfo.VideoBitrate));
                        video.Add(new XElement("codec", episodeFile.MediaInfo.VideoCodec));
                        video.Add(new XElement("framerate", episodeFile.MediaInfo.VideoFps));
                        video.Add(new XElement("height", episodeFile.MediaInfo.Height));
                        video.Add(new XElement("scantype", episodeFile.MediaInfo.ScanType));
                        video.Add(new XElement("width", episodeFile.MediaInfo.Width));

                        if (episodeFile.MediaInfo.RunTime != null)
                        {
                            video.Add(new XElement("duration", episodeFile.MediaInfo.RunTime.TotalMinutes));
                            video.Add(new XElement("durationinseconds", episodeFile.MediaInfo.RunTime.TotalSeconds));
                        }

                        streamDetails.Add(video);

                        var audio = new XElement("audio");
                        audio.Add(new XElement("bitrate", episodeFile.MediaInfo.AudioBitrate));
                        audio.Add(new XElement("channels", episodeFile.MediaInfo.AudioChannels));
                        audio.Add(new XElement("codec", GetAudioCodec(episodeFile.MediaInfo.AudioFormat)));
                        audio.Add(new XElement("language", episodeFile.MediaInfo.AudioLanguages));
                        streamDetails.Add(audio);

                        if (episodeFile.MediaInfo.Subtitles != null && episodeFile.MediaInfo.Subtitles.Length > 0)
                        {
                            var subtitle = new XElement("subtitle");
                            subtitle.Add(new XElement("language", episodeFile.MediaInfo.Subtitles));
                            streamDetails.Add(subtitle);
                        }

                        fileInfo.Add(streamDetails);
                        details.Add(fileInfo);
                    }

                    //Todo: get guest stars, writer and director
                    //details.Add(new XElement("credits", tvdbEpisode.Writer.FirstOrDefault()));
                    //details.Add(new XElement("director", tvdbEpisode.Directors.FirstOrDefault()));

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
            if (!Settings.SeriesImages)
            {
                return new List<ImageFileResult>();
            }

            return ProcessSeriesImages(series).ToList();
        }

        public override List<ImageFileResult> SeasonImages(Artist series, Album season)
        {
            if (!Settings.SeasonImages)
            {
                return new List<ImageFileResult>();
            }

            return ProcessSeasonImages(series, season).ToList();
        }

        public override List<ImageFileResult> EpisodeImages(Artist series, TrackFile episodeFile)
        {
            //if (!Settings.EpisodeImages)
            //{
            //    return new List<ImageFileResult>();
            //}

            //try
            //{
            //    var screenshot = episodeFile.Tracks.Value.First().Images.SingleOrDefault(i => i.CoverType == MediaCoverTypes.Screenshot);

            //    if (screenshot == null)
            //    {
            //        _logger.Debug("Episode screenshot not available");
            //        return new List<ImageFileResult>();
            //    }

            //    return new List<ImageFileResult>
            //       {
            //           new ImageFileResult(GetEpisodeImageFilename(episodeFile.RelativePath), screenshot.Url)
            //       };
            //}
            //catch (Exception ex)
            //{
            //    _logger.Error(ex, "Unable to process episode image for file: {0}", Path.Combine(series.Path, episodeFile.RelativePath));

            //    return new List<ImageFileResult>();
            //}

            return new List<ImageFileResult>();
        }

        private IEnumerable<ImageFileResult> ProcessSeriesImages(Artist series)
        {
            foreach (var image in series.Images)
            {
                var source = _mediaCoverService.GetCoverPath(series.Id, image.CoverType);
                var destination = image.CoverType.ToString().ToLowerInvariant() + Path.GetExtension(source);

                yield return new ImageFileResult(destination, source);
            }
        }

        private IEnumerable<ImageFileResult> ProcessSeasonImages(Artist series, Album season)
        {
            foreach (var image in season.Images)
            {
                var filename = string.Format("season{0:00}-{1}.jpg", season.Title, image.CoverType.ToString().ToLower());

                yield return new ImageFileResult(filename, image.Url);
            }
        }

        private string GetEpisodeMetadataFilename(string episodeFilePath)
        {
            return Path.ChangeExtension(episodeFilePath, "nfo");
        }

        private string GetEpisodeImageFilename(string episodeFilePath)
        {
            return Path.ChangeExtension(episodeFilePath, "").Trim('.') + "-thumb.jpg";
        }

        private string GetAudioCodec(string audioCodec)
        {
            if (audioCodec == "AC-3")
            {
                return "AC3";
            }

            return audioCodec;
        }
    }
}
