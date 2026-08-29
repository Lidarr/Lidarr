using System;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Jobs
{
    public interface IScheduledTaskRepository : IBasicRepository<ScheduledTask>
    {
        ScheduledTask GetDefinition(Type type);
        void SetLastExecutionTime(int id, DateTime executionTime, DateTime startTime);
        void SetInterval(int id, int interval, bool isUserConfigured);
    }

    public class ScheduledTaskRepository : BasicRepository<ScheduledTask>, IScheduledTaskRepository
    {
        public ScheduledTaskRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public ScheduledTask GetDefinition(Type type)
        {
            return Query(c => c.TypeName == type.FullName).Single();
        }

        public void SetLastExecutionTime(int id, DateTime executionTime, DateTime startTime)
        {
            var task = new ScheduledTask
            {
                Id = id,
                LastExecution = executionTime,
                LastStartTime = startTime
            };

            SetFields(task, scheduledTask => scheduledTask.LastExecution, scheduledTask => scheduledTask.LastStartTime);
        }

        public void SetInterval(int id, int interval, bool isUserConfigured)
        {
            var task = new ScheduledTask
            {
                Id = id,
                Interval = interval,
                IsUserConfigured = isUserConfigured
            };

            SetFields(task, scheduledTask => scheduledTask.Interval, scheduledTask => scheduledTask.IsUserConfigured);
        }
    }
}
