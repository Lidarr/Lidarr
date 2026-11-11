using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.IndexerSearch
{
    public class ArtistSearchService : IExecute<ArtistSearchCommand>
    {
        private readonly ISearchForReleases _releaseSearchService;
        private readonly ISearchForTracks _trackSearchService;
        private readonly IArtistService _artistService;
        private readonly ITrackService _trackService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public ArtistSearchService(ISearchForReleases nzbSearchService,
            ISearchForTracks trackSearchService,
            IArtistService artistService,
            ITrackService trackService,
            IProcessDownloadDecisions processDownloadDecisions,
            Logger logger)
        {
            _releaseSearchService = nzbSearchService;
            _trackSearchService = trackSearchService;
            _artistService = artistService;
            _trackService = trackService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        public void Execute(ArtistSearchCommand message)
        {
            var artist = _artistService.GetArtist(message.ArtistId);

            if (artist.SongMode)
            {
                var trackIds = _trackService.GetTracksByArtist(artist.Id)
                                            .Where(track => track.Monitored)
                                            .Select(track => track.Id)
                                            .ToList();
                var trackDecisions = _trackSearchService.TrackSearch(trackIds, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
                var trackProcessed = _processDownloadDecisions.ProcessDecisions(trackDecisions, true).GetAwaiter().GetResult();

                _logger.ProgressInfo("Song Mode artist search completed. {0} reports downloaded.", trackProcessed.Grabbed.Count);
                return;
            }

            var decisions = _releaseSearchService.ArtistSearch(message.ArtistId, false, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
            var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

            _logger.ProgressInfo("Artist search completed. {0} reports downloaded.", processed.Grabbed.Count);
        }
    }
}
