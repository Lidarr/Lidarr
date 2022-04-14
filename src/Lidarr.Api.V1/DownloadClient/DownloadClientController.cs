using Lidarr.Http;
using NzbDrone.Core.Download;

namespace Lidarr.Api.V1.DownloadClient
{
    [V1ApiController]
    public class DownloadClientController : ProviderControllerBase<DownloadClientResource, IDownloadClient, DownloadClientDefinition>
    {
        public static readonly DownloadClientResourceMapper ResourceMapper = new DownloadClientResourceMapper();

        public DownloadClientController(IDownloadClientFactory downloadClientFactory)
            : base(downloadClientFactory, "downloadclient", ResourceMapper)
        {
        }
    }
}
