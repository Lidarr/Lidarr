using System;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public interface IDeemixProxyManager
    {
        DeemixProxy GetProxy(DeemixSettings settings);
        DeemixProxy GetProxy(string url);
    }

    public class DeemixProxyManager : IDeemixProxyManager
    {
        private readonly ICached<DeemixProxy> _store;
        private readonly Logger _logger;

        public DeemixProxyManager(ICacheManager cacheManager,
                                  Logger logger)
        {
            _store = cacheManager.GetRollingCache<DeemixProxy>(GetType(), "deemix", TimeSpan.FromMinutes(2));
            _logger = logger;
        }

        public DeemixProxy GetProxy(DeemixSettings settings)
        {
            return GetProxy(GetUrl(settings));
        }

        public DeemixProxy GetProxy(string url)
        {
            var proxy = _store.Get(url, () => new DeemixProxy(url, _logger));

            _store.ClearExpired();

            return proxy;
        }

        private static string GetUrl(DeemixSettings settings)
        {
            return HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase);
        }
    }
}
