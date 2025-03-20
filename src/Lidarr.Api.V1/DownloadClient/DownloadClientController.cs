using FluentValidation;
using Lidarr.Http;
using NzbDrone.Core.Download;

namespace Lidarr.Api.V1.DownloadClient
{
    [V1ApiController]
    public class DownloadClientController : ProviderControllerBase<DownloadClientResource, DownloadClientBulkResource, IDownloadClient, DownloadClientDefinition>
    {
        public static readonly DownloadClientResourceMapper ResourceMapper = new ();
        public static readonly DownloadClientBulkResourceMapper BulkResourceMapper = new ();

        public DownloadClientController(IDownloadClientFactory downloadClientFactory)
            : base(downloadClientFactory, "downloadclient", ResourceMapper, BulkResourceMapper)
        {
            SharedValidator.RuleFor(c => c.Priority).InclusiveBetween(1, 50);
        }
    }
}
