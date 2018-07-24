using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Music
{
    public class RefreshArtistService : IExecute<RefreshArtistCommand>
    {
        private readonly IProvideArtistInfo _artistInfo;
        private readonly IArtistService _artistService;
        private readonly IAddAlbumService _addAlbumService;
        private readonly IAlbumService _albumService;
        private readonly IRefreshAlbumService _refreshAlbumService;
        private readonly IRefreshTrackService _refreshTrackService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDiskScanService _diskScanService;
        private readonly ICheckIfArtistShouldBeRefreshed _checkIfArtistShouldBeRefreshed;
        private readonly Logger _logger;

        public RefreshArtistService(IProvideArtistInfo artistInfo,
                                    IArtistService artistService,
                                    IAddAlbumService addAlbumService,
                                    IAlbumService albumService,
                                    IRefreshAlbumService refreshAlbumService,
                                    IRefreshTrackService refreshTrackService,
                                    IEventAggregator eventAggregator,
                                    IDiskScanService diskScanService,
                                    ICheckIfArtistShouldBeRefreshed checkIfArtistShouldBeRefreshed,
                                    Logger logger)
        {
            _artistInfo = artistInfo;
            _artistService = artistService;
            _addAlbumService = addAlbumService;
            _albumService = albumService;
            _refreshAlbumService = refreshAlbumService;
            _refreshTrackService = refreshTrackService;
            _eventAggregator = eventAggregator;
            _diskScanService = diskScanService;
            _checkIfArtistShouldBeRefreshed = checkIfArtistShouldBeRefreshed;
            _logger = logger;
        }

        private void RefreshArtistInfo(Artist artist, bool forceAlbumRefresh)
        {
            _logger.ProgressInfo("Updating Info for {0}", artist.Name);

            Tuple<Artist, List<Album>> tuple;

            try
            {
                tuple = _artistInfo.GetArtistInfo(artist.ForeignArtistId, artist.MetadataProfileId);
            }
            catch (ArtistNotFoundException)
            {
                _logger.Error("Artist '{0}' (LidarrAPI {1}) was not found, it may have been removed from Metadata sources.", artist.Name, artist.ForeignArtistId);
                return;
            }

            var artistInfo = tuple.Item1;

            if (artist.ForeignArtistId != artistInfo.ForeignArtistId)
            {
                _logger.Warn("Artist '{0}' (Artist {1}) was replaced with '{2}' (LidarrAPI {3}), because the original was a duplicate.", artist.Name, artist.ForeignArtistId, artistInfo.Name, artistInfo.ForeignArtistId);
                artist.ForeignArtistId = artistInfo.ForeignArtistId;
            }

            artist.Name = artistInfo.Name;
            artist.Overview = artistInfo.Overview;
            artist.Status = artistInfo.Status;
            artist.CleanName = artistInfo.CleanName;
            artist.SortName = artistInfo.SortName;
            artist.LastInfoSync = DateTime.UtcNow;
            artist.Images = artistInfo.Images;
            artist.Genres = artistInfo.Genres;
            artist.Links = artistInfo.Links;
            artist.Ratings = artistInfo.Ratings;
            artist.Disambiguation = artistInfo.Disambiguation;
            artist.ArtistType = artistInfo.ArtistType;

            try
            {
                artist.Path = new DirectoryInfo(artist.Path).FullName;
                artist.Path = artist.Path.GetActualCasing();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Couldn't update artist path for " + artist.Path);
            }

            var remoteAlbums = tuple.Item2.DistinctBy(m => new { m.ForeignAlbumId, m.ReleaseDate }).ToList();

            // Get list of DB current db albums for artist
            var existingAlbums = _albumService.GetAlbumsByArtist(artist.Id);

            var newAlbumsList = new List<Album>();
            var updateAlbumsList = new List<Album>();

            // Cycle thru albums
            foreach (var album in remoteAlbums)
            {
                // Check for album in existing albums, if not set properties and add to new list
                var albumToRefresh = existingAlbums.FirstOrDefault(s => s.ForeignAlbumId == album.ForeignAlbumId);

                if (albumToRefresh != null)
                {
                    existingAlbums.Remove(albumToRefresh);
                    updateAlbumsList.Add(albumToRefresh);
                }
                else
                {
                    newAlbumsList.Add(album);
                }
            }

            // Update new albums with artist info and correct monitored status
            newAlbumsList = UpdateAlbums(artist, newAlbumsList);

            _artistService.UpdateArtist(artist);

            _addAlbumService.AddAlbums(newAlbumsList);

            _refreshAlbumService.RefreshAlbumInfo(updateAlbumsList, forceAlbumRefresh);

            _albumService.DeleteMany(existingAlbums);

            _eventAggregator.PublishEvent(new AlbumInfoRefreshedEvent(artist, newAlbumsList, updateAlbumsList));

            _logger.Debug("Finished artist refresh for {0}", artist.Name);
            _eventAggregator.PublishEvent(new ArtistUpdatedEvent(artist));
        }

        private List<Album> UpdateAlbums(Artist artist, List<Album> albumsToUpdate)
        {
            foreach (var album in albumsToUpdate)
            {
                album.ArtistId = artist.Id;
                album.ProfileId = artist.ProfileId;
                album.Monitored = artist.Monitored;
            }

            return albumsToUpdate;
        }

        private void RescanArtist(Artist artist)
        {
            try
            {
                _diskScanService.Scan(artist);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't rescan artist {0}", artist);
            }
        }

        public void Execute(RefreshArtistCommand message)
        {
            _eventAggregator.PublishEvent(new ArtistRefreshStartingEvent(message.Trigger == CommandTrigger.Manual));

            if (message.ArtistId.HasValue)
            {
                var artist = _artistService.GetArtist(message.ArtistId.Value);

                try
                {
                    RefreshArtistInfo(artist, true);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't refresh info for {0}", artist);
                    RescanArtist(artist);
                    throw;
                }
            }
            else
            {
                var allArtists = _artistService.GetAllArtists().OrderBy(c => c.Name).ToList();

                foreach (var artist in allArtists)
                {
                    var manualTrigger = message.Trigger == CommandTrigger.Manual;
                    if (manualTrigger || _checkIfArtistShouldBeRefreshed.ShouldRefresh(artist))
                    {
                        try
                        {
                            RefreshArtistInfo(artist, manualTrigger);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Couldn't refresh info for {0}", artist);
                            RescanArtist(artist);
                        }
                    }

                    else
                    {
                        _logger.Info("Skipping refresh of artist: {0}", artist.Name);
                        RescanArtist(artist);
                    }
                }
            }
        }
    }
}
