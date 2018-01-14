using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;
using NzbDrone.Core.MetadataSource.SkyHook;

namespace NzbDrone.Core.Music
{
    public interface IAddAlbumService
    {
        Album AddAlbum(Album newAlbum);
        List<Album> AddAlbums(List<Album> newAlbums);
    }

    public class AddAlbumService : IAddAlbumService
    {
        private readonly IAlbumService _albumService;
        private readonly IProvideAlbumInfo _albumInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IAddArtistValidator _addArtistValidator;
        private readonly Logger _logger;

        public AddAlbumService(IAlbumService albumService,
                                IProvideAlbumInfo albumInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddArtistValidator addArtistValidator,
                                Logger logger)
        {
            _albumService = albumService;
            _albumInfo = albumInfo;
            _fileNameBuilder = fileNameBuilder;
            _addArtistValidator = addArtistValidator;
            _logger = logger;
        }

        public Album AddAlbum(Album newAlbum)
        {
            Ensure.That(newAlbum, () => newAlbum).IsNotNull();

            //newAlbum = AddSkyhookData(newAlbum);
            newAlbum = SetPropertiesAndValidate(newAlbum);

            _logger.Info("Adding Album {0}", newAlbum);
            _albumService.AddAlbum(newAlbum);

            return newAlbum;
        }

        public List<Album> AddAlbums(List<Album> newAlbums)
        {
            var added = DateTime.UtcNow;
            var artistsToAdd = new List<Album>();

            foreach (var s in newAlbums)
            {
                // TODO: Verify if adding skyhook data will be slow
                // var album = AddSkyhookData(s);
                var album = s;
                album = SetPropertiesAndValidate(album);
                album.Added = added;
                artistsToAdd.Add(album);
            }

            return _albumService.AddAlbums(artistsToAdd);
        }

        private Album AddSkyhookData(Album newAlbum)
        {
            Tuple<Album, List<Track>> tuple;

            try
            {
                tuple = _albumInfo.GetAlbumInfo(newAlbum.ForeignAlbumId);
            }
            catch (ArtistNotFoundException)
            {
                _logger.Error("LidarrId {1} was not found, it may have been removed from Lidarr.", newAlbum.ForeignAlbumId);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("MusicBrainzId", "An album with this ID was not found", newAlbum.ForeignAlbumId)
                                              });
            }

            var album = tuple.Item1;

            // If albums were passed in on the new artist use them, otherwise use the albums from Skyhook
            newAlbum.Tracks = newAlbum.Tracks != null && newAlbum.Tracks.Any() ? newAlbum.Tracks : album.Tracks;

            album.ApplyChanges(newAlbum);

            return album;
        }

        private Album SetPropertiesAndValidate(Album newAlbum)
        {
            newAlbum.CleanTitle = newAlbum.Title.CleanArtistName();
            //newAlbum.SortTitle = ArtistNameNormalizer.Normalize(newAlbum.Title, newAlbum.ForeignAlbumId);
            newAlbum.Added = DateTime.UtcNow;

            //var validationResult = _addArtistValidator.Validate(newAlbum);

            //if (!validationResult.IsValid)
            //{
            //    throw new ValidationException(validationResult.Errors);
            //}

            return newAlbum;
        }
    }
}
