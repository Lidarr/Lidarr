using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.IndexerSearch;

namespace Lidarr.Api.V1.Config
{
    [V1ApiController("config/searchformat")]
    public class SearchFormatConfigController : RestController<SearchFormatConfigResource>
    {
        private readonly ISearchFormatConfigService _configService;
        private readonly ISearchFormatSampleService _sampleService;

        public SearchFormatConfigController(ISearchFormatConfigService configService, ISearchFormatSampleService sampleService)
        {
            _configService = configService;
            _sampleService = sampleService;
        }

        public override SearchFormatConfigResource GetResourceById(int id)
        {
            return GetSearchFormatConfig();
        }

        [HttpGet]
        public SearchFormatConfigResource GetSearchFormatConfig()
        {
            return _configService.GetConfig().ToResource();
        }

        [RestPutById]
        public ActionResult<SearchFormatConfigResource> UpdateSearchFormatConfig([FromBody] SearchFormatConfigResource resource)
        {
            _configService.Save(resource.ToModel());
            return Accepted(resource.Id);
        }

        [HttpGet("examples")]
        public SearchFormatExampleResource GetExamples([FromQuery] SearchFormatConfigResource config)
        {
            if (config.Id == 0)
            {
                config = GetSearchFormatConfig();
            }

            var sampleResult = _sampleService.GetExamples(config.AlbumSearchFormat, config.ArtistSearchFormat);

            return new SearchFormatExampleResource
            {
                AlbumSearchExample = sampleResult.AlbumSearchExample,
                ArtistSearchExample = sampleResult.ArtistSearchExample
            };
        }
    }
}
