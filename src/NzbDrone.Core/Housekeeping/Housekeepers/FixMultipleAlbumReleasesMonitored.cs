using System.Linq;
using NLog;
using NLog.Fluent;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class FixMultipleAlbumReleasesMonitored : IHousekeepingTask
    {
        private readonly IMainDatabase _database;
        private readonly IReleaseRepository _releaseRepository;
        private readonly Logger _logger;

        public FixMultipleAlbumReleasesMonitored(IMainDatabase database,
                                                 IReleaseRepository releaseRepository,
                                                 Logger logger)
        {
            _database = database;
            _releaseRepository = releaseRepository;
            _logger = logger;
        }

        public void Clean()
        {
            var mapper = _database.GetDataMapper();

            var albumIds = mapper.ExecuteReader(@"SELECT AlbumId
                                                  FROM (
                                                    SELECT AlbumId, Sum(Monitored)
                                                    FROM AlbumReleases
                                                    GROUP BY AlbumId
                                                    HAVING Sum(Monitored) != 1
                                                  )", reader => reader.GetInt32(0));

            foreach (var albumId in albumIds)
            {
                var releases = _releaseRepository.FindByAlbum(albumId);
                var monitored = releases.FirstOrDefault(x => x.Monitored) ?? releases.First();

                // this will make sure that only 'monitored' is monitored and unmonitor the rest
                _releaseRepository.SetMonitored(monitored);

                // log a warning to sentry so we can work out whether this is something
                // that is happening regularly
                _logger.Warn()
                    .Message("Multiple releases were monitored for {0} [{1}], correcting", monitored.Title, albumId)
                    .WriteSentryWarn($"Multiple releases monitored")
                    .Write();

            }
        }
    }
}
