using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteAlbum remoteAlbum, long size);
        List<CustomFormat> ParseCustomFormat(TrackFile trackFile, Artist artist);
        List<CustomFormat> ParseCustomFormat(TrackFile trackFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Artist artist);
        List<CustomFormat> ParseCustomFormat(EntityHistory history, Artist artist);
        List<CustomFormat> ParseCustomFormat(LocalTrack localTrack);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService, Logger logger)
        {
            _formatService = formatService;
            _logger = logger;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteAlbum remoteAlbum, long size)
        {
            var input = new CustomFormatInput
            {
                AlbumInfo = remoteAlbum.ParsedAlbumInfo,
                Artist = remoteAlbum.Artist,
                Size = size
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(TrackFile trackFile, Artist artist)
        {
            return ParseCustomFormat(trackFile, artist, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(TrackFile trackFile)
        {
            return ParseCustomFormat(trackFile, trackFile.Artist.Value, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Artist artist)
        {
            var parsed = Parser.Parser.ParseAlbumTitle(blocklist.SourceTitle);

            var albumInfo = new ParsedAlbumInfo
            {
                ArtistName = artist.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Quality = blocklist.Quality,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                AlbumInfo = albumInfo,
                Artist = artist,
                Size = blocklist.Size ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Artist artist)
        {
            var parsed = Parser.Parser.ParseAlbumTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);

            var albumInfo = new ParsedAlbumInfo
            {
                ArtistName = artist.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Quality = history.Quality,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                AlbumInfo = albumInfo,
                Artist = artist,
                Size = size
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalTrack localTrack)
        {
            var albumInfo = new ParsedAlbumInfo
            {
                ArtistName = localTrack.Artist.Name,
                ReleaseTitle = localTrack.SceneName.IsNotNullOrWhiteSpace() ? localTrack.SceneName : Path.GetFileName(localTrack.Path),
                Quality = localTrack.Quality,
                ReleaseGroup = localTrack.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                AlbumInfo = albumInfo,
                Artist = localTrack.Artist,
                Size = localTrack.Size,
                Filename = Path.GetFileName(localTrack.Path)
            };

            return ParseCustomFormat(input);
        }

        private List<CustomFormat> ParseCustomFormat(CustomFormatInput input)
        {
            return ParseCustomFormat(input, _formatService.All());
        }

        private static List<CustomFormat> ParseCustomFormat(CustomFormatInput input, List<CustomFormat> allCustomFormats)
        {
            var matches = new List<CustomFormat>();

            foreach (var customFormat in allCustomFormats)
            {
                var specificationMatches = customFormat.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(input))
                    })
                    .ToList();

                if (specificationMatches.All(x => x.DidMatch))
                {
                    matches.Add(customFormat);
                }
            }

            return matches.OrderBy(x => x.Name).ToList();
        }

        private List<CustomFormat> ParseCustomFormat(TrackFile trackFile, Artist artist, List<CustomFormat> allCustomFormats)
        {
            var releaseTitle = string.Empty;

            if (trackFile.SceneName.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using scene name for release title: {0}", trackFile.SceneName);
                releaseTitle = trackFile.SceneName;
            }
            else if (trackFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using original file path for release title: {0}", Path.GetFileName(trackFile.OriginalFilePath));
                releaseTitle = Path.GetFileName(trackFile.OriginalFilePath);
            }
            else if (trackFile.Path.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using path for release title: {0}", Path.GetFileName(trackFile.Path));
                releaseTitle = Path.GetFileName(trackFile.Path);
            }

            var albumInfo = new ParsedAlbumInfo
            {
                ArtistName = artist.Name,
                ReleaseTitle = releaseTitle,
                Quality = trackFile.Quality,
                ReleaseGroup = trackFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                AlbumInfo = albumInfo,
                Artist = artist,
                Size = trackFile.Size,
                Filename = Path.GetFileName(trackFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
