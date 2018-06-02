using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.EnvironmentInfo;

namespace Lidarr.SignalR
{
    public interface IBroadcastSignalRMessage
    {
        Task BroadcastMessage(SignalRMessage message);
    }

    public class SignalRMessageBroadcaster : IBroadcastSignalRMessage
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public SignalRMessageBroadcaster(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastMessage(SignalRMessage message)
        {
            await _hubContext.Clients.All.SendAsync("receiveMessage", message);
        }
    }

    public class MessageHub : Hub
    {
        public async Task BroadcastMessage(SignalRMessage message)
        {
            await Clients.All.SendAsync("receiveMessage", message);
        }

        public override async Task OnConnectedAsync()
        {
            var message = new SignalRMessage
            {
                Name = "version",
                Body = new
                {
                    Version = BuildInfo.Version.ToString()
                }
            };

            await Clients.All.SendAsync("receiveVersion", message);
            await base.OnConnectedAsync();
        }
    }
}
