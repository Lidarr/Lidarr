using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Music
{
    public interface IAlbumMonitoredService
    {
        void SetAlbumMonitoredStatus(Artist artist, MonitoringOptions monitoringOptions);
    }

    public class AlbumMonitoredService : IAlbumMonitoredService
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly Logger _logger;

        public AlbumMonitoredService(IArtistService artistService, IAlbumService albumService, Logger logger)
        {
            _artistService = artistService;
            _albumService = albumService;
            _logger = logger;
        }

        public void SetAlbumMonitoredStatus(Artist artist, MonitoringOptions monitoringOptions)
        {
            // Update the artist without changing the albums
            if (monitoringOptions == null)
            {
                _artistService.UpdateArtist(artist);
                return;
            }

            var monitoredAlbums = monitoringOptions.AlbumsToMonitor;

            if (monitoringOptions.Monitor == MonitorTypes.Unknown && monitoredAlbums is not { Count: not 0 })
            {
                return;
            }

            _logger.Debug("[{0}] Setting album monitored status.", artist.Name);

            var albums = _albumService.GetAlbumsByArtist(artist.Id);

            // If specific albums are passed use those instead of the monitoring options.
            if (monitoredAlbums.Any())
            {
                ToggleAlbumsMonitoredState(albums.Where(s => monitoredAlbums.Contains(s.ForeignAlbumId)), true);
                ToggleAlbumsMonitoredState(albums.Where(s => !monitoredAlbums.Contains(s.ForeignAlbumId)), false);
            }
            else
            {
                var albumsWithFiles = _albumService.GetArtistAlbumsWithFiles(artist);
                var albumsWithoutFiles = albums.Where(c => !albumsWithFiles.Select(e => e.Id).Contains(c.Id) && c.ReleaseDate <= DateTime.UtcNow).ToList();

                switch (monitoringOptions.Monitor)
                {
                    case MonitorTypes.All:
                        _logger.Debug("Monitoring all albums");
                        ToggleAlbumsMonitoredState(albums, true);
                        break;
                    case MonitorTypes.Future:
                        _logger.Debug("Unmonitoring Albums with Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                        _logger.Debug("Unmonitoring Albums without Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                        break;
                    case MonitorTypes.Missing:
                        _logger.Debug("Unmonitoring Albums with Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                        _logger.Debug("Monitoring Albums without Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithoutFiles.Select(c => c.Id).Contains(e.Id)), true);
                        break;
                    case MonitorTypes.Existing:
                        _logger.Debug("Monitoring Albums with Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithFiles.Select(c => c.Id).Contains(e.Id)), true);
                        _logger.Debug("Unmonitoring Albums without Files");
                        ToggleAlbumsMonitoredState(albums.Where(e => albumsWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                        break;
                    case MonitorTypes.Latest:
                        _logger.Debug("Monitoring latest album");
                        ToggleAlbumsMonitoredState(albums, false);
                        ToggleAlbumsMonitoredState(albums.OrderByDescending(e => e.ReleaseDate).Take(1), true);
                        break;
                    case MonitorTypes.First:
                        _logger.Debug("Monitoring first album");
                        ToggleAlbumsMonitoredState(albums, false);
                        ToggleAlbumsMonitoredState(albums.OrderBy(e => e.ReleaseDate).Take(1), true);
                        break;
                    case MonitorTypes.None:
                        _logger.Debug("Unmonitoring all albums");
                        ToggleAlbumsMonitoredState(albums, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _albumService.UpdateMany(albums);
            _artistService.UpdateArtist(artist);
        }

        private void ToggleAlbumsMonitoredState(IEnumerable<Album> albums, bool monitored)
        {
            foreach (var album in albums)
            {
                album.Monitored = monitored;
            }
        }
    }
}
