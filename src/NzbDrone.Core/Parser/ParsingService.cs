using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Artist GetArtist(string title);
        Artist GetArtistFromTag(string file);
        RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, SearchCriteriaBase searchCriteria = null);
        RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, int artistId, IEnumerable<int> albumIds);
        List<Album> GetAlbums(ParsedAlbumInfo parsedAlbumInfo, Artist artist, SearchCriteriaBase searchCriteria = null);
        Album GetLocalAlbum(string filename, Artist artist);
    }

    public class ParsingService : IParsingService
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly ITrackService _trackService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IAlbumYearMatcher _yearMatcher;
        private readonly Logger _logger;

        public ParsingService(ITrackService trackService,
                              IArtistService artistService,
                              IAlbumService albumService,
                              IMediaFileService mediaFileService,
                              IAlbumYearMatcher yearMatcher,
                              Logger logger)
        {
            _albumService = albumService;
            _artistService = artistService;
            _trackService = trackService;
            _mediaFileService = mediaFileService;
            _yearMatcher = yearMatcher;
            _logger = logger;
        }

        public Artist GetArtist(string title)
        {
            var parsedAlbumInfo = Parser.ParseAlbumTitle(title);

            if (parsedAlbumInfo != null && !parsedAlbumInfo.ArtistName.IsNullOrWhiteSpace())
            {
                title = parsedAlbumInfo.ArtistName;
            }

            var artistInfo = _artistService.FindByName(title);

            if (artistInfo == null)
            {
                _logger.Debug("Trying inexact artist match for {0}", title);
                artistInfo = _artistService.FindByNameInexact(title);
            }

            return artistInfo;
        }

        public Artist GetArtistFromTag(string file)
        {
            var parsedTrackInfo = Parser.ParseMusicPath(file);

            var artist = new Artist();

            if (parsedTrackInfo.ArtistMBId.IsNotNullOrWhiteSpace())
            {
                artist = _artistService.FindById(parsedTrackInfo.ArtistMBId);

                if (artist != null)
                {
                    return artist;
                }
            }

            if (parsedTrackInfo == null || parsedTrackInfo.ArtistTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            artist = _artistService.FindByName(parsedTrackInfo.ArtistTitle);

            if (artist == null)
            {
                _logger.Debug("Trying inexact artist match for {0}", parsedTrackInfo.ArtistTitle);
                artist = _artistService.FindByNameInexact(parsedTrackInfo.ArtistTitle);
            }

            return artist;
        }

        public RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteAlbum = new RemoteAlbum
            {
                ParsedAlbumInfo = parsedAlbumInfo,
            };

            var artist = GetArtist(parsedAlbumInfo, searchCriteria);

            if (artist == null)
            {
                return remoteAlbum;
            }

            remoteAlbum.Artist = artist;
            remoteAlbum.Albums = GetAlbums(parsedAlbumInfo, artist, searchCriteria);

            return remoteAlbum;
        }

        public List<Album> GetAlbums(ParsedAlbumInfo parsedAlbumInfo, Artist artist, SearchCriteriaBase searchCriteria = null)
        {
            var albumTitle = parsedAlbumInfo.AlbumTitle;
            var result = new List<Album>();

            if (parsedAlbumInfo.AlbumTitle == null)
            {
                return new List<Album>();
            }

            Album albumInfo = null;

            if (parsedAlbumInfo.Discography)
            {
                if (parsedAlbumInfo.DiscographyStart > 0)
                {
                    return _albumService.ArtistAlbumsBetweenDates(artist,
                        new DateTime(parsedAlbumInfo.DiscographyStart, 1, 1),
                        new DateTime(parsedAlbumInfo.DiscographyEnd, 12, 31),
                        false);
                }

                if (parsedAlbumInfo.DiscographyEnd > 0)
                {
                    return _albumService.ArtistAlbumsBetweenDates(artist,
                        new DateTime(1800, 1, 1),
                        new DateTime(parsedAlbumInfo.DiscographyEnd, 12, 31),
                        false);
                }

                return _albumService.GetAlbumsByArtist(artist.Id);
            }

            var releaseYear = parsedAlbumInfo.ReleaseYear;

            if (searchCriteria != null)
            {
                albumInfo = FindAlbumInSearchCriteria(searchCriteria.Albums, albumTitle, releaseYear);
            }

            if (albumInfo == null)
            {
                albumInfo = _albumService.FindByTitleAndYear(artist.ArtistMetadataId, parsedAlbumInfo.AlbumTitle, releaseYear);
            }

            if (albumInfo == null)
            {
                var yearInfo = releaseYear.HasValue ? $" ({releaseYear.Value})" : string.Empty;
                _logger.Debug("Trying inexact album match for {0}{1}", parsedAlbumInfo.AlbumTitle, yearInfo);
                albumInfo = _albumService.FindByTitleAndYearInexact(artist.ArtistMetadataId, parsedAlbumInfo.AlbumTitle, releaseYear);
            }

            if (albumInfo != null)
            {
                result.Add(albumInfo);
            }
            else
            {
                _logger.Debug("Unable to find {0}", parsedAlbumInfo);
            }

            return result;
        }

        private Album FindAlbumInSearchCriteria(List<Album> albums, string albumTitle, int? releaseYear)
        {
            var matchingAlbums = albums.Where(e => e.Title == albumTitle).ToList();

            if (!matchingAlbums.Any())
            {
                return null;
            }

            if (matchingAlbums.Count == 1)
            {
                var album = matchingAlbums.First();

                if (releaseYear.HasValue)
                {
                    var matchResult = _yearMatcher.Match(album, releaseYear);
                    if (!matchResult.IsMatch)
                    {
                        _logger.Debug("Album '{0}' matched by title but {1}", album.Title, matchResult.RejectionReason);
                        return null;
                    }
                }

                return album;
            }

            // Multiple albums with same title - use year to disambiguate
            if (releaseYear.HasValue)
            {
                var bestMatch = matchingAlbums
                    .Select(a => new
                    {
                        Album = a,
                        YearResult = _yearMatcher.Match(a, releaseYear)
                    })
                    .Where(x => x.YearResult.IsMatch)
                    .OrderByDescending(x => x.YearResult.ScoreAdjustment)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    return bestMatch.Album;
                }

                _logger.Debug("Multiple albums named '{0}' found but none match year {1}", albumTitle, releaseYear);
                return null;
            }

            // No year to disambiguate, cannot safely choose from multiple matches
            _logger.Trace("Multiple albums named '{0}' found without year to disambiguate, unable to determine correct match", albumTitle);
            return null;
        }

        public RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, int artistId, IEnumerable<int> albumIds)
        {
            return new RemoteAlbum
            {
                ParsedAlbumInfo = parsedAlbumInfo,
                Artist = _artistService.GetArtist(artistId),
                Albums = _albumService.GetAlbums(albumIds)
            };
        }

        private Artist GetArtist(ParsedAlbumInfo parsedAlbumInfo, SearchCriteriaBase searchCriteria)
        {
            Artist artist = null;

            if (searchCriteria != null)
            {
                if (searchCriteria.Artist.CleanName == parsedAlbumInfo.ArtistName.CleanArtistName())
                {
                    return searchCriteria.Artist;
                }
            }

            artist = _artistService.FindByName(parsedAlbumInfo.ArtistName);

            if (artist == null)
            {
                _logger.Debug("Trying inexact artist match for {0}", parsedAlbumInfo.ArtistName);
                artist = _artistService.FindByNameInexact(parsedAlbumInfo.ArtistName);
            }

            if (artist == null)
            {
                _logger.Debug("No matching artist {0}", parsedAlbumInfo.ArtistName);
                return null;
            }

            return artist;
        }

        public Album GetLocalAlbum(string filename, Artist artist)
        {
            if (Path.HasExtension(filename))
            {
                filename = Path.GetDirectoryName(filename);
            }

            var tracksInAlbum = _mediaFileService.GetFilesByArtist(artist.Id)
                .FindAll(s => Path.GetDirectoryName(s.Path) == filename)
                .DistinctBy(s => s.AlbumId)
                .ToList();

            return tracksInAlbum.Count == 1 ? _albumService.GetAlbum(tracksInAlbum.First().AlbumId) : null;
        }
    }
}
