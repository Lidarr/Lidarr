using Lidarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    [V1ApiController("statistics")]
    public class StatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet]
        [Produces("application/json")]
        public StatisticsResource GetLibraryStatistics([FromQuery] StatisticsFilter filter)
        {
            return _statisticsService.GetLibraryStatistics(filter).MapToResource();
        }
    }
}
