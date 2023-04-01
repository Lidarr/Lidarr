using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Music
{
    public interface IMonitorNewAlbumService
    {
        bool ShouldMonitorNewAlbum(Album addedAlbum, List<Album> existingAlbums, NewItemMonitorTypes monitorNewItems);
    }

    public class MonitorNewAlbumService : IMonitorNewAlbumService
    {
        private readonly Logger _logger;

        public MonitorNewAlbumService(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldMonitorNewAlbum(Album addedAlbum, List<Album> existingAlbums, NewItemMonitorTypes monitorNewItems)
        {
            if (monitorNewItems == NewItemMonitorTypes.None)
            {
                return false;
            }

            if (monitorNewItems == NewItemMonitorTypes.All)
            {
                return true;
            }

            if (monitorNewItems == NewItemMonitorTypes.New)
            {
                var newest = existingAlbums.MaxBy(x => x.ReleaseDate ?? DateTime.MinValue)?.ReleaseDate ?? DateTime.MinValue;

                return (addedAlbum.ReleaseDate ?? DateTime.MinValue) >= newest;
            }

            throw new NotImplementedException($"Unknown new item monitor type {monitorNewItems}");
        }
    }
}
