using System.Collections.Generic;
using Lidarr.Http;
using Lidarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;

namespace Lidarr.Api.V1.Tags
{
    [V1ApiController("tag/detail")]
    public class TagDetailsController : RestController<TagDetailsResource>
    {
        private readonly ITagService _tagService;

        public TagDetailsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        public override TagDetailsResource GetResourceById(int id)
        {
            return _tagService.Details(id).ToResource();
        }

        [HttpGet]
        public List<TagDetailsResource> GetAll()
        {
            var tags = _tagService.Details().ToResource();

            return tags;
        }
    }
}
