using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Lidarr.Http
{
    public class ApiInfoController : Controller
    {
        [HttpGet("/api")]
        [Produces("application/json")]
        public object GetApiInfo()
        {
            return new ApiInfoResource
            {
                Current = "v1",
                Deprecated = new List<string>()
            };
        }
    }
}
