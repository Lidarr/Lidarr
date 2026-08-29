using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Backup;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Housekeeping;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Update.Commands;

namespace NzbDrone.Core.Jobs
{
    public interface ITaskManager
    {
        IList<ScheduledTask> GetPending();
        List<ScheduledTask> GetAll();
        DateTime GetNextExecution(Type type);
        int GetDefaultInterval(int id);
        void SetInterval(int id, int interval);
        void ResetInterval(int id);
    }

    public class TaskManager : ITaskManager, IHandle<ApplicationStartedEvent>, IHandle<CommandExecutedEvent>
    {
        private readonly IScheduledTaskRepository _scheduledTaskRepository;
        private readonly IConfigService _configService;
        private readonly Logger _logger;
        private readonly ICached<ScheduledTask> _cache;

        public TaskManager(IScheduledTaskRepository scheduledTaskRepository, IConfigService configService, ICacheManager cacheManager, Logger logger)
        {
            _scheduledTaskRepository = scheduledTaskRepository;
            _configService = configService;
            _cache = cacheManager.GetCache<ScheduledTask>(GetType());
            _logger = logger;
        }

        public IList<ScheduledTask> GetPending()
        {
            return _cache.Values
                         .Where(c => c.Interval > 0 && c.LastExecution.AddMinutes(c.Interval) < DateTime.UtcNow)
                         .ToList();
        }

        public List<ScheduledTask> GetAll()
        {
            return _cache.Values.ToList();
        }

        public DateTime GetNextExecution(Type type)
        {
            var scheduledTask = _cache.Find(type.FullName);

            return scheduledTask.LastExecution.AddMinutes(scheduledTask.Interval);
        }

        public int GetDefaultInterval(int id)
        {
            var scheduledTask = _cache.Values.SingleOrDefault(c => c.Id == id);

            if (scheduledTask == null)
            {
                throw new ModelNotFoundException(typeof(ScheduledTask), id);
            }

            return GetDefaultTasks().Single(c => c.TypeName == scheduledTask.TypeName).Interval;
        }

        public void SetInterval(int id, int interval)
        {
            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            var scheduledTask = _cache.Values.SingleOrDefault(c => c.Id == id);

            if (scheduledTask == null)
            {
                throw new ModelNotFoundException(typeof(ScheduledTask), id);
            }

            _scheduledTaskRepository.SetInterval(id, interval, true);
            scheduledTask.Interval = interval;
            scheduledTask.IsUserConfigured = true;

            if (scheduledTask.TypeName == typeof(RssSyncCommand).FullName)
            {
                _configService.RssSyncInterval = interval;
            }
        }

        public void ResetInterval(int id)
        {
            var scheduledTask = _cache.Values.SingleOrDefault(c => c.Id == id);

            if (scheduledTask == null)
            {
                throw new ModelNotFoundException(typeof(ScheduledTask), id);
            }

            var defaultInterval = GetDefaultInterval(id);

            _scheduledTaskRepository.SetInterval(id, defaultInterval, false);
            scheduledTask.Interval = defaultInterval;
            scheduledTask.IsUserConfigured = false;

            if (scheduledTask.TypeName == typeof(RssSyncCommand).FullName)
            {
                _configService.RssSyncInterval = defaultInterval;
            }
        }

        public void Handle(ApplicationStartedEvent message)
        {
            var defaultTasks = GetDefaultTasks();

            var currentTasks = _scheduledTaskRepository.All().ToList();

            _logger.Trace("Initializing jobs. Available: {0} Existing: {1}", defaultTasks.Count, currentTasks.Count);

            foreach (var job in currentTasks)
            {
                if (!defaultTasks.Any(c => c.TypeName == job.TypeName))
                {
                    _logger.Trace("Removing job from database '{0}'", job.TypeName);
                    _scheduledTaskRepository.Delete(job.Id);
                }
            }

            foreach (var defaultTask in defaultTasks)
            {
                var currentDefinition = currentTasks.SingleOrDefault(c => c.TypeName == defaultTask.TypeName) ?? defaultTask;

                if (!currentDefinition.IsUserConfigured)
                {
                    currentDefinition.Interval = defaultTask.Interval;
                }

                if (currentDefinition.Id == 0)
                {
                    currentDefinition.LastExecution = DateTime.UtcNow;
                }

                currentDefinition.Priority = defaultTask.Priority;

                if (currentDefinition.TypeName == typeof(RssSyncCommand).FullName)
                {
                    _configService.RssSyncInterval = currentDefinition.Interval;
                }

                _cache.Set(currentDefinition.TypeName, currentDefinition);
                _scheduledTaskRepository.Upsert(currentDefinition);
            }
        }

        private List<ScheduledTask> GetDefaultTasks()
        {
            return new List<ScheduledTask>
            {
                new ScheduledTask { Interval = 1, TypeName = typeof(RefreshMonitoredDownloadsCommand).FullName, Priority = CommandPriority.High },
                new ScheduledTask { Interval = 5, TypeName = typeof(MessagingCleanupCommand).FullName },
                new ScheduledTask { Interval = 6 * 60, TypeName = typeof(ApplicationUpdateCheckCommand).FullName },
                new ScheduledTask { Interval = 6 * 60, TypeName = typeof(CheckHealthCommand).FullName },
                new ScheduledTask { Interval = 24 * 60, TypeName = typeof(RefreshArtistCommand).FullName },
                new ScheduledTask { Interval = 24 * 60, TypeName = typeof(RescanFoldersCommand).FullName },
                new ScheduledTask { Interval = 24 * 60, TypeName = typeof(HousekeepingCommand).FullName },
                new ScheduledTask { Interval = 7 * 24 * 60, TypeName = typeof(BackupCommand).FullName },
                new ScheduledTask { Interval = 5, TypeName = typeof(ImportListSyncCommand).FullName },
                new ScheduledTask { Interval = 15, TypeName = typeof(RssSyncCommand).FullName }
            };
        }

        public void Handle(CommandExecutedEvent message)
        {
            var scheduledTask = _scheduledTaskRepository.All().SingleOrDefault(c => c.TypeName == message.Command.Body.GetType().FullName);

            if (scheduledTask != null && message.Command.Body.UpdateScheduledTask)
            {
                _logger.Trace("Updating last run time for: {0}", scheduledTask.TypeName);

                var lastExecution = DateTime.UtcNow;
                var startTime = message.Command.StartedAt.Value;

                _scheduledTaskRepository.SetLastExecutionTime(scheduledTask.Id, lastExecution, startTime);

                var cached = _cache.Find(scheduledTask.TypeName);

                cached.LastExecution = lastExecution;
                cached.LastStartTime = startTime;
            }
        }
    }
}
