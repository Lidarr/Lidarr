using Nancy;
using NzbDrone.Api.Extensions;
using NzbDrone.Core.Music;

namespace NzbDrone.Api.AlbumPass
{
    public class AlbumStudioModule : NzbDroneApiModule
    {
        private readonly ITrackMonitoredService _trackMonitoredService;

        public AlbumStudioModule(ITrackMonitoredService trackMonitoredService)
            : base("/albumstudio")
        {
            _trackMonitoredService = trackMonitoredService;
            Post["/"] = artist => UpdateAll();
        }

        private Response UpdateAll()
        {
            //Read from request
            var request = Request.Body.FromJson<AlbumStudioResource>();

            foreach (var s in request.Artist)
            {
                _trackMonitoredService.SetTrackMonitoredStatus(s, request.MonitoringOptions);
            }

            return "ok".AsResponse(HttpStatusCode.Accepted);
        }
    }
}
