using System.Collections.Generic;
using System.Linq;
using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Update;

namespace Lidarr.Api.V1.Update
{
    [V1ApiController]
    public class UpdateController : Controller
    {
        private readonly IRecentUpdateProvider _recentUpdateProvider;

        public UpdateController(IRecentUpdateProvider recentUpdateProvider)
        {
            _recentUpdateProvider = recentUpdateProvider;
        }

        [HttpGet]
        public List<UpdateResource> GetRecentUpdates()
        {
            var resources = _recentUpdateProvider.GetRecentUpdatePackages()
                                                 .OrderByDescending(u => u.Version)
                                                 .ToResource();

            if (resources.Any())
            {
                var first = resources.First();
                first.Latest = true;

                if (first.Version > BuildInfo.Version)
                {
                    first.Installable = true;
                }

                var installed = resources.SingleOrDefault(r => r.Version == BuildInfo.Version);

                if (installed != null)
                {
                    installed.Installed = true;
                }
            }

            return resources;
        }
    }
}
