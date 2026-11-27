using System;
using System.Collections.Generic;
using Dapper;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Housekeeping
{
    public class HousekeepingService : IExecute<HousekeepingCommand>
    {
        private readonly IEnumerable<IHousekeepingTask> _housekeepers;
        private readonly Logger _logger;
        private readonly IMainDatabase _mainDb;

        public HousekeepingService(IEnumerable<IHousekeepingTask> housekeepers, IMainDatabase mainDb, Logger logger)
        {
            _housekeepers = housekeepers;
            _logger = logger;
            _mainDb = mainDb;
        }

        private void Clean()
        {
            _logger.Info("Running housecleaning tasks");

            foreach (var housekeeper in _housekeepers)
            {
                try
                {
                    _logger.Debug("Starting {0}", housekeeper.GetType().Name);
                    housekeeper.Clean();
                    _logger.Debug("Completed {0}", housekeeper.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error running housekeeping task: {0}", housekeeper.GetType().Name);
                }
            }

            if (ShouldVacuum())
            {
                // Vacuuming the log db isn't needed since that's done in a separate housekeeping task
                _logger.Debug("Compressing main database after housekeeping");
                _mainDb.Vacuum();
            }
        }

        private bool ShouldVacuum()
        {
            if (_mainDb.DatabaseType != DatabaseType.SQLite)
            {
                return true;
            }

            try
            {
                using var db = _mainDb.OpenConnection();

                var pageCount = db.ExecuteScalar<long>("PRAGMA page_count;");
                var freePageCount = db.ExecuteScalar<long>("PRAGMA freelist_count;");

                var shouldVacuum = pageCount > 0 &&
                                freePageCount * 100 >= pageCount * 15;

                if (!shouldVacuum)
                {
                    _logger.Debug(
                        "Skipping SQLite VACUUM: {0} of {1} pages are free",
                        freePageCount,
                        pageCount);
                }

                return shouldVacuum;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to evaluate SQLite VACUUM requirement");
                return false;
            }
        }

        public void Execute(HousekeepingCommand message)
        {
            Clean();
        }
    }
}
