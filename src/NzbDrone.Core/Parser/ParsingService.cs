using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Music;
using System;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Artist GetArtist(string title);
        Artist GetArtistFromTag(string file);
        RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, SearchCriteriaBase searchCriteria = null);
        RemoteAlbum Map(ParsedAlbumInfo parsedAlbumInfo, int artistId, IEnumerable<int> albumIds);
        List<Album> GetAlbums(ParsedAlbumInfo parsedAlbumInfo, Artist artist, SearchCriteriaBase searchCriteria = null);

        // Music stuff here
        LocalTrack GetLocalTrack(string filename, Artist artist);
        LocalTrack GetLocalTrack(string filename, Artist artist, ParsedTrackInfo folderInfo);

    }

    public class ParsingService : IParsingService
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly ITrackService _trackService;
        private readonly Logger _logger;

        public ParsingService(ITrackService trackService,
                              IArtistService artistService,
                              IAlbumService albumService,
                              // ISceneMappingService sceneMappingService,
                              Logger logger)
        {
            _albumService = albumService;
            _artistService = artistService;
            // _sceneMappingService = sceneMappingService;
            _trackService = trackService;
            _logger = logger;
        }

        public Artist GetArtist(string title)
        {
            var parsedAlbumInfo = Parser.ParseAlbumTitle(title);
            
            if (parsedAlbumInfo == null || parsedAlbumInfo.ArtistName.IsNullOrWhiteSpace())
            {
                return _artistService.FindByName(title);
            }

            return _artistService.FindByName(parsedAlbumInfo.ArtistName);
            
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

            return _artistService.FindByName(parsedTrackInfo.ArtistTitle);

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

            if (searchCriteria != null)
            {
                albumInfo = searchCriteria.Albums.SingleOrDefault(e => e.Title == albumTitle);
            }

            if (albumInfo == null)
            {
                // TODO: Search by Title and Year instead of just Title when matching
                albumInfo = _albumService.FindByTitle(artist.Id, parsedAlbumInfo.AlbumTitle);
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
                _logger.Debug("No matching artist {0}", parsedAlbumInfo.ArtistName);
                return null;
            }

            return artist;
        }

        public LocalTrack GetLocalTrack(string filename, Artist artist)
        {
            return GetLocalTrack(filename, artist, null);
        }

        public LocalTrack GetLocalTrack(string filename, Artist artist, ParsedTrackInfo folderInfo)
        {
            ParsedTrackInfo parsedTrackInfo;


            if (folderInfo != null)
            {
                parsedTrackInfo = folderInfo.JsonClone();
                parsedTrackInfo.Quality = QualityParser.ParseQuality(Path.GetFileName(filename), null, 0);
            }

            else
            {
                parsedTrackInfo = Parser.ParseMusicPath(filename);
            }

            if (parsedTrackInfo == null || parsedTrackInfo.AlbumTitle.IsNullOrWhiteSpace())
            {
                if (MediaFileExtensions.Extensions.Contains(Path.GetExtension(filename)))
                {
                    _logger.Warn("Unable to parse track info from path {0}", filename);
                }

                return null;
            }

            var tracks = GetTracks(artist, parsedTrackInfo);
            var album = _albumService.FindByTitle(artist.Id, parsedTrackInfo.AlbumTitle);

            return new LocalTrack
            {
                Artist = artist,
                Album = album,
                Quality = parsedTrackInfo.Quality,
                Language = parsedTrackInfo.Language,
                Tracks = tracks,
                Path = filename,
                ParsedTrackInfo = parsedTrackInfo,
                ExistingFile = artist.Path.IsParentPath(filename)
            };
        }

        private List<Track> GetTracks(Artist artist, ParsedTrackInfo parsedTrackInfo)
        {
            var result = new List<Track>();

            if (parsedTrackInfo.AlbumTitle.IsNullOrWhiteSpace())
            {
                _logger.Debug("Album title could not be parsed for {0}", parsedTrackInfo);
                return new List<Track>();
            }

            var album = _albumService.FindByTitle(artist.Id, parsedTrackInfo.AlbumTitle);
            _logger.Debug("Album {0} selected for {1}", album, parsedTrackInfo);

            if (album == null)
            {
                _logger.Debug("Parsed album title not found in Db for {0}", parsedTrackInfo);
                return new List<Track>();
            }

            Track trackInfo = null;

            if (parsedTrackInfo.Title.IsNotNullOrWhiteSpace())
            {
                trackInfo = _trackService.FindTrackByTitle(artist.Id, album.Id, parsedTrackInfo.DiscNumber, parsedTrackInfo.Title);
                _logger.Debug("Track {0} selected for {1}", trackInfo, parsedTrackInfo);

                if (trackInfo != null)
                {
                    result.Add(trackInfo);
                    return result;
                }
            }

            _logger.Debug("Track title search unsuccessful, falling back to track number for {1}", trackInfo, parsedTrackInfo);

            if (parsedTrackInfo.TrackNumbers == null)
            {
                _logger.Debug("Track has no track numbers: {1}", trackInfo, parsedTrackInfo);
                return new List<Track>();
            }

            foreach (var trackNumber in parsedTrackInfo.TrackNumbers)
            {
                Track trackInfoByNumber = null;

                trackInfoByNumber = _trackService.FindTrack(artist.Id, album.Id, parsedTrackInfo.DiscNumber, trackNumber);
                _logger.Debug("Track {0} selected for {1}", trackInfoByNumber, parsedTrackInfo);

                if (trackInfoByNumber != null)
                {
                    result.Add(trackInfoByNumber);
                }

                else
                {
                    _logger.Debug("Unable to find {0}", parsedTrackInfo);
                }
            }

            return result;
        }
    }
}
