﻿using System.Collections.Generic;
using System.Linq;
using Lidarr.Http.REST;
using NzbDrone.Common.Http;
using NzbDrone.Core.HealthCheck;

namespace Lidarr.Api.V1.Health
{
    public class HealthResource : RestResource
    {
        public string Source { get; set; }
        public HealthCheckResult Type { get; set; }
        public string Message { get; set; }
        public HttpUri WikiUrl { get; set; }
    }

    public static class HealthResourceMapper
    {
        public static HealthResource ToResource(this HealthCheck model)
        {
            if (model == null)
            {
                return null;
            }

            return new HealthResource
            {
                Id = model.Id,
                Source = model.Source.Name,
                Type = model.Type,
                Message = model.Message,
                WikiUrl = model.WikiUrl
            };
        }

        public static List<HealthResource> ToResource(this IEnumerable<HealthCheck> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
