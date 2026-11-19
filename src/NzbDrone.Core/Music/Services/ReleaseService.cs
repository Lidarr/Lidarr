using System.Collections.Generic;
using System.Linq;
using System.Net;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;

namespace NzbDrone.Core.Music
{
    public interface IReleaseService
    {
        AlbumRelease GetRelease(int id);
        AlbumRelease GetReleaseByForeignReleaseId(string foreignReleaseId, bool checkRedirect = false);
        List<AlbumRelease> GetAllReleases();
        void InsertMany(List<AlbumRelease> releases);
        void UpdateMany(List<AlbumRelease> releases);
        void DeleteMany(List<AlbumRelease> releases);
        List<AlbumRelease> GetReleasesForRefresh(int albumId, List<string> foreignReleaseIds);
        List<AlbumRelease> GetReleasesByAlbum(int releaseGroupId);
        List<AlbumRelease> GetReleasesByRecordingIds(List<string> recordingIds);
        List<AlbumRelease> SetMonitored(AlbumRelease release);
        void ApplyReleaseFilter(Album album, int? previouslyMonitoredId);
    }

    public class ReleaseService : IReleaseService,
                                  IHandle<AlbumDeletedEvent>
    {
        private readonly IReleaseRepository _releaseRepository;
        private readonly IEventAggregator _eventAggregator;

        public ReleaseService(IReleaseRepository releaseRepository,
                              IEventAggregator eventAggregator)
        {
            _releaseRepository = releaseRepository;
            _eventAggregator = eventAggregator;
        }

        public AlbumRelease GetRelease(int id)
        {
            return _releaseRepository.Get(id);
        }

        public AlbumRelease GetReleaseByForeignReleaseId(string foreignReleaseId, bool checkRedirect = false)
        {
            return _releaseRepository.FindByForeignReleaseId(foreignReleaseId, checkRedirect);
        }

        public List<AlbumRelease> GetAllReleases()
        {
            return _releaseRepository.All().ToList();
        }

        public void InsertMany(List<AlbumRelease> releases)
        {
            _releaseRepository.InsertMany(releases);
        }

        public void UpdateMany(List<AlbumRelease> releases)
        {
            _releaseRepository.UpdateMany(releases);
        }

        public void DeleteMany(List<AlbumRelease> releases)
        {
            _releaseRepository.DeleteMany(releases);
            foreach (var release in releases)
            {
                _eventAggregator.PublishEvent(new ReleaseDeletedEvent(release));
            }
        }

        public List<AlbumRelease> GetReleasesForRefresh(int albumId, List<string> foreignReleaseIds)
        {
            return _releaseRepository.GetReleasesForRefresh(albumId, foreignReleaseIds);
        }

        public List<AlbumRelease> GetReleasesByAlbum(int releaseGroupId)
        {
            return _releaseRepository.FindByAlbum(releaseGroupId);
        }

        public List<AlbumRelease> GetReleasesByRecordingIds(List<string> recordingIds)
        {
            return _releaseRepository.FindByRecordingId(recordingIds);
        }

        public List<AlbumRelease> SetMonitored(AlbumRelease release)
        {
            return _releaseRepository.SetMonitored(release);
        }

        // Keep the monitored release consistent with the album's track/media count filter.
        // An explicit release choice by the user (monitored release differs from the previously
        // monitored one) always wins. Throws a client error when the filter can match nothing.
        public void ApplyReleaseFilter(Album album, int? previouslyMonitoredId)
        {
            var releases = album.AlbumReleases?.Value;

            if (releases == null || !releases.Any())
            {
                return;
            }

            if (album.HasReleaseFilter() && !releases.Any(album.IsAllowedRelease))
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "No release of this album matches the selected track count and media count");
            }

            var monitored = releases.SingleOrDefault(x => x.Monitored);
            var userPickedRelease = monitored != null && monitored.Id != previouslyMonitoredId;

            if (userPickedRelease || album.IsAllowedRelease(monitored))
            {
                return;
            }

            var toMonitor = album.ChooseMonitoredRelease(releases);
            releases.ForEach(x => x.Monitored = x.Id == toMonitor.Id);
        }

        public void Handle(AlbumDeletedEvent message)
        {
            var releases = GetReleasesByAlbum(message.Album.Id);
            DeleteMany(releases);
        }
    }
}
