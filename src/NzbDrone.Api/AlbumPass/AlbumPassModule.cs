using Nancy;
using NzbDrone.Api.Extensions;
using NzbDrone.Core.Music;

namespace NzbDrone.Api.AlbumPass
{
    public class AlbumPassModule : NzbDroneApiModule
    {
        private readonly ITrackMonitoredService _trackMonitoredService;

        public AlbumPassModule(ITrackMonitoredService trackMonitoredService)
            : base("/albumpass")
        {
            _trackMonitoredService = trackMonitoredService;
            Post["/"] = series => UpdateAll();
        }

        private Response UpdateAll()
        {
            //Read from request
            var request = Request.Body.FromJson<AlbumPassResource>();

            foreach (var s in request.Artist)
            {
                _trackMonitoredService.SetTrackMonitoredStatus(s, request.MonitoringOptions);
            }

            return "ok".AsResponse(HttpStatusCode.Accepted);
        }
    }
}
