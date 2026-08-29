using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.System.Tasks
{
    [V1ApiController("system/task")]
    public class TaskController : RestControllerWithSignalR<TaskResource, ScheduledTask>, IHandle<CommandExecutedEvent>
    {
        private readonly ITaskManager _taskManager;

        public TaskController(ITaskManager taskManager, IBroadcastSignalRMessage broadcastSignalRMessage)
            : base(broadcastSignalRMessage)
        {
            _taskManager = taskManager;
        }

        [HttpGet]
        public List<TaskResource> GetAll()
        {
            return _taskManager.GetAll()
                               .Select(task => ConvertToResource(task, _taskManager.GetDefaultInterval(task.Id)))
                               .OrderBy(t => t.Name)
                               .ToList();
        }

        [RestPutById]
        public ActionResult<TaskResource> Update([FromBody] TaskResource resource)
        {
            if (resource.Interval < 1)
            {
                throw new BadRequestException("Interval must be greater than zero");
            }

            _taskManager.SetInterval(resource.Id, resource.Interval);

            return Accepted(resource.Id);
        }

        [HttpPost("reset/{id:int}")]
        public ActionResult<TaskResource> Reset(int id)
        {
            ValidateId(id);
            _taskManager.ResetInterval(id);

            return Accepted(id);
        }

        public override TaskResource GetResourceById(int id)
        {
            var task = _taskManager.GetAll()
                               .SingleOrDefault(t => t.Id == id);

            if (task == null)
            {
                return null;
            }

            return ConvertToResource(task, _taskManager.GetDefaultInterval(task.Id));
        }

        private static TaskResource ConvertToResource(ScheduledTask scheduledTask, int defaultInterval)
        {
            var taskName = scheduledTask.TypeName.Split('.').Last().Replace("Command", "");

            return new TaskResource
            {
                Id = scheduledTask.Id,
                Name = taskName.SplitCamelCase(),
                TaskName = taskName,
                Interval = scheduledTask.Interval,
                DefaultInterval = defaultInterval,
                IsUserConfigured = scheduledTask.IsUserConfigured,
                LastExecution = scheduledTask.LastExecution,
                LastStartTime = scheduledTask.LastStartTime,
                NextExecution = scheduledTask.LastExecution.AddMinutes(scheduledTask.Interval)
            };
        }

        [NonAction]
        public void Handle(CommandExecutedEvent message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
