using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IDownloadService
    {
        Task DownloadReport(RemoteAlbum remoteAlbum, int? downloadClientId);
    }

    public class DownloadService : IDownloadService
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISeedConfigProvider _seedConfigProvider;
        private readonly Logger _logger;

        public DownloadService(IProvideDownloadClient downloadClientProvider,
                               IDownloadClientStatusService downloadClientStatusService,
                               IIndexerFactory indexerFactory,
                               IIndexerStatusService indexerStatusService,
                               IRateLimitService rateLimitService,
                               IEventAggregator eventAggregator,
                               ISeedConfigProvider seedConfigProvider,
                               Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _downloadClientStatusService = downloadClientStatusService;
            _indexerFactory = indexerFactory;
            _indexerStatusService = indexerStatusService;
            _rateLimitService = rateLimitService;
            _eventAggregator = eventAggregator;
            _seedConfigProvider = seedConfigProvider;
            _logger = logger;
        }

        public async Task DownloadReport(RemoteAlbum remoteAlbum, int? downloadClientId)
        {
            var filterBlockedClients = remoteAlbum.Release.PendingReleaseReason == PendingReleaseReason.DownloadClientUnavailable;

            var tags = remoteAlbum.Artist?.Tags;

            var downloadClient = downloadClientId.HasValue
                ? _downloadClientProvider.Get(downloadClientId.Value)
                : _downloadClientProvider.GetDownloadClient(remoteAlbum.Release.DownloadProtocol, remoteAlbum.Release.IndexerId, filterBlockedClients, tags);

            await DownloadReport(remoteAlbum, downloadClient);
        }

        private async Task DownloadReport(RemoteAlbum remoteAlbum, IDownloadClient downloadClient)
        {
            Ensure.That(remoteAlbum.Artist, () => remoteAlbum.Artist).IsNotNull();
            Ensure.That(remoteAlbum.Albums, () => remoteAlbum.Albums).HasItems();

            var downloadTitle = remoteAlbum.Release.Title;

            if (downloadClient == null)
            {
                throw new DownloadClientUnavailableException($"{remoteAlbum.Release.DownloadProtocol} Download client isn't configured yet");
            }

            // Get the seed configuration for this release.
            remoteAlbum.SeedConfiguration = _seedConfigProvider.GetSeedConfiguration(remoteAlbum);

            // Limit grabs to 2 per second.
            if (remoteAlbum.Release.DownloadUrl.IsNotNullOrWhiteSpace() && !remoteAlbum.Release.DownloadUrl.StartsWith("magnet:"))
            {
                var url = new HttpUri(remoteAlbum.Release.DownloadUrl);
                await _rateLimitService.WaitAndPulseAsync(url.Host, TimeSpan.FromSeconds(2));
            }

            IIndexer indexer = null;

            if (remoteAlbum.Release.IndexerId > 0)
            {
                indexer = _indexerFactory.GetInstance(_indexerFactory.Get(remoteAlbum.Release.IndexerId));
            }

            string downloadClientId;
            try
            {
                downloadClientId = await downloadClient.Download(remoteAlbum, indexer);
                _downloadClientStatusService.RecordSuccess(downloadClient.Definition.Id);
                _indexerStatusService.RecordSuccess(remoteAlbum.Release.IndexerId);
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", remoteAlbum);
                throw;
            }
            catch (ReleaseBlockedException)
            {
                _logger.Trace("Release {0} previously added to blocklist, not sending to download client again.", remoteAlbum);
                throw;
            }
            catch (DownloadClientRejectedReleaseException)
            {
                _logger.Trace("Release {0} rejected by download client, possible duplicate.", remoteAlbum);
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                if (ex.InnerException is TooManyRequestsException http429)
                {
                    _indexerStatusService.RecordFailure(remoteAlbum.Release.IndexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(remoteAlbum.Release.IndexerId);
                }

                throw;
            }

            var albumGrabbedEvent = new AlbumGrabbedEvent(remoteAlbum);
            albumGrabbedEvent.DownloadClient = downloadClient.Name;
            albumGrabbedEvent.DownloadClientId = downloadClient.Definition.Id;
            albumGrabbedEvent.DownloadClientName = downloadClient.Definition.Name;

            if (downloadClientId.IsNotNullOrWhiteSpace())
            {
                albumGrabbedEvent.DownloadId = downloadClientId;
            }

            _logger.ProgressInfo("Report sent to {0} from indexer {1}. {2}", downloadClient.Definition.Name, remoteAlbum.Release.Indexer, downloadTitle);
            _eventAggregator.PublishEvent(albumGrabbedEvent);
        }
    }
}
