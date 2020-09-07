using Lidarr.Http.Frontend;
using Nancy;
using Nancy.Bootstrapper;

namespace Lidarr.Http.Extensions.Pipelines
{
    public class CacheHeaderPipeline : IRegisterNancyPipeline
    {
        private readonly ICacheableSpecification _cacheableSpecification;

        public CacheHeaderPipeline(ICacheableSpecification cacheableSpecification)
        {
            _cacheableSpecification = cacheableSpecification;
        }

        public int Order => 0;

        public void Register(IPipelines pipelines)
        {
            pipelines.AfterRequest.AddItemToStartOfPipeline(Handle);
        }

        private void Handle(NancyContext context)
        {
            if (context.Request.Method == "OPTIONS")
            {
                return;
            }

            if (_cacheableSpecification.IsCacheable(context))
            {
                context.Response.Headers.EnableCache();
            }
            else
            {
                context.Response.Headers.DisableCache();
            }
        }
    }
}
