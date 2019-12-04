using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ArtistUpdatedEvent))]
    [CheckOn(typeof(ArtistsDeletedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(ArtistRefreshCompleteEvent))]
    public class RemovedArtistCheck : HealthCheckBase, ICheckOnCondition<ArtistUpdatedEvent>, ICheckOnCondition<ArtistsDeletedEvent>
    {
        private readonly IArtistService _artistService;
        private readonly Logger _logger;

        public RemovedArtistCheck(ILocalizationService localizationService, IArtistService artistService, Logger logger)
            : base(localizationService)
        {
            _artistService = artistService;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var deletedArtists = _artistService.GetAllArtists().Where(v => v.Metadata.Value.Status == ArtistStatusType.Deleted).ToList();

            if (deletedArtists.Empty())
            {
                return new HealthCheck(GetType());
            }

            var artistText = deletedArtists.Select(s => $"{s.Name} (mbid {s.ForeignArtistId})").Join(", ");

            if (deletedArtists.Count == 1)
            {
                return new HealthCheck(GetType(), HealthCheckResult.Error, $"Artist {artistText} was removed from MusicBrainz");
            }

            return new HealthCheck(GetType(), HealthCheckResult.Error, $"Artists {artistText} were removed from MusicBrainz");
        }

        public bool ShouldCheckOnEvent(ArtistsDeletedEvent deletedEvent)
        {
            return deletedEvent.Artists.Any(artist => artist.Metadata.Value.Status == ArtistStatusType.Deleted);
        }

        public bool ShouldCheckOnEvent(ArtistUpdatedEvent updatedEvent)
        {
            return updatedEvent.Artist.Metadata.Value.Status == ArtistStatusType.Deleted;
        }
    }
}
