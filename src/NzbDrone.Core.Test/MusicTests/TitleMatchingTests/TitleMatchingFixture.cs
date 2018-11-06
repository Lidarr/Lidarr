using System.Linq;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;
using System.Collections.Generic;

namespace NzbDrone.Core.Test.MusicTests.TitleMatchingTests
{
    [TestFixture]
    public class TitleMatchingFixture : DbTest<TrackService, Track>
    {
        private TrackRepository _trackRepository;
        private TrackService _trackService;

        [SetUp]
        public void Setup()
        {
            _trackRepository = Mocker.Resolve<TrackRepository>();
            _trackService =
                new TrackService(_trackRepository, Mocker.Resolve<ConfigService>(), Mocker.Resolve<Logger>());

            var trackNames = new List<string> {
                "Courage",
                "Movies",
                "Flesh and Bone",
                "Whisper",
                "Summer",
                "Sticks and Stones",
                "Attitude",
                "Stranded",
                "Wish",
                "Calico",
                "(Happy) Death Day",
                "Smooth Criminal",
                "Universe / Orange Appeal"
            };

            for (int i = 0; i < trackNames.Count; i++) {
                _trackRepository.Insert(new Track
                        {
                            ArtistId = 1234,
                            Title = trackNames[i],
                            ForeignTrackId = (i+1).ToString(),
                            AlbumId = 4321,
                            AbsoluteTrackNumber = i+1,
                            MediumNumber = 1
                        });
            }
        }

        private void GivenSecondDisc()
        {
            var trackNames = new List<string> {
                "first track",
                "another entry",
                "random name"
            };

            for (int i = 0; i < trackNames.Count; i++) {
                _trackRepository.Insert(new Track
                        {
                            ArtistId = 1234,
                            Title = trackNames[i],
                            ForeignTrackId = (100+i+1).ToString(),
                            AlbumId = 4321,
                            AbsoluteTrackNumber = i+1,
                            MediumNumber = 2
                        });
            }
        }

        [Test]
        public void should_find_track_in_db_by_tracktitle_longer_then_releasetitle()
        {
            var track = _trackService.FindTrackByTitle(1234, 4321, 1, 1, "Courage with some bla");

            track.Should().NotBeNull();
            track.Title.Should().Be(_trackRepository.Find(1234, 4321, 1, 1).Title);
        }

        [Test]
        public void should_find_track_in_db_by_tracktitle_shorter_then_releasetitle()
        {
            var track = _trackService.FindTrackByTitle(1234, 4321, 1, 3, "and Bone");

            track.Should().NotBeNull();
            track.Title.Should().Be(_trackRepository.Find(1234, 4321, 1, 3).Title);
        }

        [Test]
        public void should_not_find_track_in_db_by_wrong_title()
        {
            var track = _trackService.FindTrackByTitle(1234, 4321, 1, 1, "Not a track");

            track.Should().BeNull();
        }

        [TestCase("Courage", 1, 1)]
        [TestCase("first track", 2, 1)]
        [TestCase("another entry", 2, 2)]
        [TestCase("random name", 2, 3)]
        public void should_find_track_on_second_disc_when_disc_tag_missing(string title, int discNumber, int trackNumber)
        {
            GivenSecondDisc();
            var track = _trackService.FindTrackByTitle(1234, 4321, 0, trackNumber, title);
            var expected = _trackRepository.Find(1234, 4321, discNumber, trackNumber);

            track.Should().NotBeNull();
            expected.Should().NotBeNull();

            track.Title.Should().Be(expected.Title);
        }

        [Test]
        public void should_find_track_from_earlier_disc_if_title_identical_and_disc_tag_missing()
        {
            GivenSecondDisc();
            _trackRepository.Insert(new Track
                {
                    ArtistId = 1234,
                    Title = "Courage",
                    ForeignTrackId = "999",
                    AlbumId = 4321,
                    AbsoluteTrackNumber = 5,
                    MediumNumber = 2,
                });

            var track = _trackService.FindTrackByTitle(1234, 4321, 0, 1, "Courage");
            track.Should().NotBeNull();
            track.Title.Should().Be("Courage");
            track.MediumNumber.Should().Be(1);

            var track2 = _trackService.FindTrackByTitle(1234, 4321, 2, 5, "Courage");
            track2.Should().NotBeNull();
            track2.Title.Should().Be("Courage");
            track2.MediumNumber.Should().Be(2);
        }

        [TestCase("Fesh and Bone", 3)]
        [TestCase("Atitude", 7)]
        [TestCase("Smoth cRimnal", 12)]
        [TestCase("Sticks and Stones (live)", 6)]
        public void should_find_track_in_db_by_inexact_title(string title, int trackNumber)
        {
            var track = _trackService.FindTrackByTitleInexact(1234, 4321, 1, trackNumber, title);
            var expected = _trackRepository.Find(1234, 4321, 1, trackNumber);

            track.Should().NotBeNull();
            expected.Should().NotBeNull();

            track.Title.Should().Be(expected.Title);
        }

        [TestCase("Courage!", 1, 1)]
        [TestCase("first trakc", 2, 1)]
        [TestCase("anoth entry", 2, 2)]
        [TestCase("random.name", 2, 3)]
        public void should_find_track_in_db_by_inexact_title_when_disc_tag_missing(string title, int discNumber, int trackNumber)
        {
            GivenSecondDisc();
            var track = _trackService.FindTrackByTitleInexact(1234, 4321, 0, trackNumber, title);
            var expected = _trackRepository.Find(1234, 4321, discNumber, trackNumber);

            track.Should().NotBeNull();
            expected.Should().NotBeNull();

            track.Title.Should().Be(expected.Title);
        }

        [TestCase("A random title", 1)]
        [TestCase("Stones and Sticks", 6)]
        public void should_not_find_track_in_db_by_different_inexact_title(string title, int trackId)
        {
            var track = _trackService.FindTrackByTitleInexact(1234, 4321, 1, trackId, title);

            track.Should().BeNull();
        }


    }
}
