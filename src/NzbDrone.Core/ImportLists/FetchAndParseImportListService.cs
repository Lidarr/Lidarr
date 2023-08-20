using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public interface IFetchAndParseImportList
    {
        List<ImportListItemInfo> Fetch();
        List<ImportListItemInfo> FetchSingleList(ImportListDefinition definition);
    }

    public class FetchAndParseImportListService : IFetchAndParseImportList
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListStatusService _importListStatusService;
        private readonly Logger _logger;

        public FetchAndParseImportListService(IImportListFactory importListFactory, IImportListStatusService importListStatusService, Logger logger)
        {
            _importListFactory = importListFactory;
            _importListStatusService = importListStatusService;
            _logger = logger;
        }

        public List<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            var importLists = _importListFactory.AutomaticAddEnabled();

            if (!importLists.Any())
            {
                _logger.Debug("No enabled import lists, skipping.");
                return result;
            }

            _logger.Debug("Available import lists {0}", importLists.Count);

            Parallel.ForEach(importLists, new ParallelOptions { MaxDegreeOfParallelism = 5 }, importList =>
            {
                var importListLocal = importList;
                var importListStatus = _importListStatusService.GetLastSyncListInfo(importListLocal.Definition.Id);

                if (importListStatus.HasValue)
                {
                    var importListNextSync = importListStatus.Value + importListLocal.MinRefreshInterval;

                    if (DateTime.UtcNow < importListNextSync)
                    {
                        _logger.Trace("Skipping refresh of Import List {0} ({1}) due to minimum refresh interval. Next sync after {2}", importList.Name, importListLocal.Definition.Name, importListNextSync);
                        return;
                    }
                }

                try
                {
                    var importListReports = importListLocal.Fetch();

                    lock (result)
                    {
                        _logger.Debug("Found {0} reports from {1} ({2})", importListReports.Count, importList.Name, importListLocal.Definition.Name);

                        result.AddRange(importListReports);
                    }

                    _importListStatusService.UpdateListSyncStatus(importList.Definition.Id);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during Import List Sync of {0} ({1})", importList.Name, importListLocal.Definition.Name);
                }
            });

            result = result.DistinctBy(r => new { r.Artist, r.Album }).ToList();

            _logger.Debug("Found {0} total reports from {1} lists", result.Count, importLists.Count);

            return result;
        }

        public List<ImportListItemInfo> FetchSingleList(ImportListDefinition definition)
        {
            var result = new List<ImportListItemInfo>();

            var importList = _importListFactory.GetInstance(definition);

            if (importList == null || !definition.EnableAutomaticAdd)
            {
                _logger.Debug("Import List {0} ({1}) is not enabled, skipping.", importList.Name, importList.Definition.Name);
                return result;
            }

            var importListLocal = importList;

            try
            {
                var importListReports = importListLocal.Fetch();

                lock (result)
                {
                    _logger.Debug("Found {0} reports from {1} ({2})", importListReports.Count, importList.Name, importListLocal.Definition.Name);

                    result.AddRange(importListReports);
                }

                _importListStatusService.UpdateListSyncStatus(importList.Definition.Id);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during Import List Sync of {0} ({1})", importList.Name, importListLocal.Definition.Name);
            }

            result = result.DistinctBy(r => new { r.Artist, r.Album }).ToList();

            return result;
        }
    }
}
