using System.Threading.Tasks;
using Lidarr.Http.Extensions;
using Microsoft.AspNetCore.Http;

namespace Lidarr.Http.Middleware
{
    public class CacheHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ICacheableSpecification _cacheableSpecification;

        public CacheHeaderMiddleware(RequestDelegate next, ICacheableSpecification cacheableSpecification)
        {
            _next = next;
            _cacheableSpecification = cacheableSpecification;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method != "OPTIONS")
            {
                if (_cacheableSpecification.IsCacheable(context))
                {
                    context.Response.Headers.EnableCache();
                }
                else
                {
                    context.Response.Headers.DisableCache();
                }
            }

            await _next(context);
        }
    }
}
