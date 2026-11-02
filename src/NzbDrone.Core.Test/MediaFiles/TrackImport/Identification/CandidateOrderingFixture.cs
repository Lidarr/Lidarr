using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.TrackImport.Identification;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.TrackImport.Identification
{
    [TestFixture]
    public class CandidateOrderingFixture : CoreTest
    {
        private static CandidateAlbumRelease Candidate(int id, int trackCount, Album album)
        {
            var release = new AlbumRelease { Id = id, TrackCount = trackCount, Media = new List<Medium> { new Medium { Number = 1 } } };

            if (album != null)
            {
                release.Album = album;
            }

            return new CandidateAlbumRelease(release);
        }

        [Test]
        public void should_put_releases_allowed_by_album_filter_first_keeping_relative_order()
        {
            var album = new Album { PreferredTrackCounts = new List<int> { 11 } };

            var candidates = new List<CandidateAlbumRelease>
            {
                Candidate(1, 22, album),
                Candidate(2, 11, album),
                Candidate(3, 30, album),
                Candidate(4, 11, album)
            };

            var ordered = IdentificationService.OrderByAlbumFilter(candidates);

            ordered.Select(x => x.AlbumRelease.Id).Should().Equal(2, 4, 1, 3);
        }

        [Test]
        public void should_keep_order_when_album_has_no_filter()
        {
            var album = new Album();

            var candidates = new List<CandidateAlbumRelease> { Candidate(1, 22, album), Candidate(2, 11, album) };

            IdentificationService.OrderByAlbumFilter(candidates).Select(x => x.AlbumRelease.Id).Should().Equal(1, 2);
        }

        [Test]
        public void should_treat_release_without_loaded_album_as_allowed()
        {
            var album = new Album { PreferredTrackCounts = new List<int> { 11 } };

            var candidates = new List<CandidateAlbumRelease> { Candidate(1, 22, album), Candidate(2, 22, null) };

            IdentificationService.OrderByAlbumFilter(candidates).Select(x => x.AlbumRelease.Id).Should().Equal(2, 1);
        }
    }
}
