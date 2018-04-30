using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Common.TPL;
using System;
using NzbDrone.Common.Extensions;

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
        private readonly Logger _logger;

        public FetchAndParseImportListService(IImportListFactory importListFactory, Logger logger)
        {
            _importListFactory = importListFactory;
            _logger = logger;
        }

        public List<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            var importLists = _importListFactory.AutomaticAddEnabled();

            if (!importLists.Any())
            {
                _logger.Warn("No available import lists. check your configuration.");
                return result;
            }

            _logger.Debug("Available import lists {0}", importLists.Count);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var importList in importLists)
            {
                var importListLocal = importList;

                var task = taskFactory.StartNew(() =>
                     {
                         try
                         {
                             var importListReports = importListLocal.Fetch();

                             lock (result)
                             {
                                 _logger.Debug("Found {0} from {1}", importListReports.Count, importList.Name);

                                 result.AddRange(importListReports);
                             }
                         }
                         catch (Exception e)
                         {
                             _logger.Error(e, "Error during Import List Sync");
                         }
                     }).LogExceptions();

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            result = result.DistinctBy(r => new {r.Artist, r.Album}).ToList();

            _logger.Debug("Found {0} reports", result.Count);

            return result;
        }

        public List<ImportListItemInfo> FetchSingleList(ImportListDefinition definition)
        {
            var result = new List<ImportListItemInfo>();

            var importList = _importListFactory.GetInstance(definition);

            if (importList == null || !definition.EnableAutomaticAdd)
            {
                _logger.Warn("No available import lists. check your configuration.");
                return result;
            }

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            var importListLocal = importList;

            var task = taskFactory.StartNew(() =>
            {
                try
                {
                    var importListReports = importListLocal.Fetch();

                    lock (result)
                    {
                        _logger.Debug("Found {0} from {1}", importListReports.Count, importList.Name);

                        result.AddRange(importListReports);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during Import List Sync");
                }
            }).LogExceptions();

            taskList.Add(task);


            Task.WaitAll(taskList.ToArray());

            result = result.DistinctBy(r => new { r.Artist, r.Album }).ToList();

            return result;
        }
    }
}
