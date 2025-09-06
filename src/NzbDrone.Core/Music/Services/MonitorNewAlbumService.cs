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
                _logger.Trace("Album '{0}' will not be monitored: Monitor setting is set to 'None'", addedAlbum.Title);
                return false;
            }

            if (monitorNewItems == NewItemMonitorTypes.All)
            {
                _logger.Trace("Album '{0}' will be monitored: Monitor setting is set to 'All'", addedAlbum.Title);
                return true;
            }

            if (monitorNewItems == NewItemMonitorTypes.New)
            {
                var newestExistingDate = existingAlbums
                    .Where(x => x.ReleaseDate.HasValue)
                    .MaxBy(x => x.ReleaseDate.Value)?.ReleaseDate;

                if (!addedAlbum.ReleaseDate.HasValue)
                {
                    if (!newestExistingDate.HasValue)
                    {
                        _logger.Debug("Album '{0}' will be monitored: Both new and existing albums have no release dates", addedAlbum.Title);
                        return true;
                    }
                    else
                    {
                        _logger.Debug("Album '{0}' will not be monitored: Albums without release dates are skipped when existing albums have dates", addedAlbum.Title);
                        return false;
                    }
                }

                if (!newestExistingDate.HasValue)
                {
                    _logger.Debug("Album '{0}' will be monitored: No existing albums have release dates, so this is considered the first 'new' release", addedAlbum.Title);
                    return true;
                }

                var shouldMonitor = addedAlbum.ReleaseDate.Value >= newestExistingDate.Value;
                _logger.Trace("Album '{0}' ({1}) {2} be monitored: Release date is {3} the most recent existing album ({4})",
                    addedAlbum.Title,
                    addedAlbum.ReleaseDate.Value.ToString("yyyy-MM-dd"),
                    shouldMonitor ? "will" : "will not",
                    shouldMonitor ? "on or after" : "before",
                    newestExistingDate.Value.ToString("yyyy-MM-dd"));

                return shouldMonitor;
            }

            throw new NotImplementedException($"Unknown new item monitor type {monitorNewItems}");
        }
    }
}
