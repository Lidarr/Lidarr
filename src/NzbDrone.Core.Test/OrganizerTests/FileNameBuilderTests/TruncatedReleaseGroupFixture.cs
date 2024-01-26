using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class TruncatedReleaseGroupFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private AlbumRelease _release;
        private List<Track> _tracks;
        private TrackFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                .CreateNew()
                .With(s => s.Name = "Artist Name")
                .Build();

            _album = Builder<Album>
                .CreateNew()
                .With(s => s.Title = "Album Title")
                .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { new () { Number = 14 } })
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _tracks = new List<Track>
            {
                Builder<Track>.CreateNew()
                    .With(e => e.Title = "Track Title 1")
                    .With(e => e.MediumNumber = 1)
                    .With(e => e.AbsoluteTrackNumber = 1)
                    .With(e => e.AlbumRelease = _release)
                    .Build(),
            };

            _trackFile = new TrackFile { Quality = new QualityModel(Quality.MP3_320), ReleaseGroup = "LidarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        private void GivenProper()
        {
            _trackFile.Quality.Revision.Version = 2;
        }

        [Test]
        public void should_truncate_from_beginning()
        {
            _artist.Name = "The Fantastic Life of Mr. Sisko";

            _trackFile.Quality.Quality = Quality.FLAC;
            _trackFile.ReleaseGroup = "IWishIWasALittleBitTallerIWishIWasABallerIWishIHadAGirlWhoLookedGoodIWouldCallHerIWishIHadARabbitInAHatWithABatAndASixFourImpala";
            _tracks = _tracks.Take(1).ToList();
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]-{ReleaseGroup:12}";

            var result = Subject.BuildTrackFileName(_tracks, _artist, _album, _trackFile, ".flac");
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("The Fantastic Life of Mr. Sisko - Album Title - 01 - Track Title 1 [FLAC]-IWishIWas....flac");
        }

        [Test]
        public void should_truncate_from_from_end()
        {
            _artist.Name = "The Fantastic Life of Mr. Sisko";

            _trackFile.Quality.Quality = Quality.FLAC;
            _trackFile.ReleaseGroup = "IWishIWasALittleBitTallerIWishIWasABallerIWishIHadAGirlWhoLookedGoodIWouldCallHerIWishIHadARabbitInAHatWithABatAndASixFourImpala";
            _tracks = _tracks.Take(1).ToList();
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]-{ReleaseGroup:-17}";

            var result = Subject.BuildTrackFileName(_tracks, _artist, _album, _trackFile, ".flac");
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("The Fantastic Life of Mr. Sisko - Album Title - 01 - Track Title 1 [FLAC]-...ASixFourImpala.flac");
        }
    }
}
