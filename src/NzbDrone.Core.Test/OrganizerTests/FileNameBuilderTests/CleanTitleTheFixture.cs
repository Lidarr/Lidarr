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
    public class CleanTitleTheFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private AlbumRelease _release;
        private Track _track;
        private TrackFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .With(s => s.Name = "Avenged Sevenfold")
                    .Build();

            _album = Builder<Album>
                    .CreateNew()
                    .With(s => s.Title = "Hail to the King")
                    .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { new Medium { Number = 1 } })
                .Build();

            _track = Builder<Track>.CreateNew()
                            .With(e => e.Title = "Doing Time")
                            .With(e => e.AbsoluteTrackNumber = 3)
                            .With(e => e.AlbumRelease = _release)
                            .Build();

            _trackFile = new TrackFile { Quality = new QualityModel(Quality.MP3_256), ReleaseGroup = "LidarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [TestCase("The Mist", "Mist, The")]
        [TestCase("A Place to Call Home", "Place to Call Home, A")]
        [TestCase("An Adventure in Space and Time", "Adventure in Space and Time, An")]
        [TestCase("The Flash (2010)", "Flash, The 2010")]
        [TestCase("A League Of Their Own (AU)", "League Of Their Own, A AU")]
        [TestCase("The Fixer (ZH) (2015)", "Fixer, The ZH 2015")]
        [TestCase("The Sixth Sense 2 (Thai)", "Sixth Sense 2, The Thai")]
        [TestCase("The Amazing Race (Latin America)", "Amazing Race, The Latin America")]
        [TestCase("The Rat Pack (A&E)", "Rat Pack, The AandE")]
        [TestCase("The Climax: I (Almost) Got Away With It (2016)", "Climax I Almost Got Away With It, The 2016")]
        public void should_get_expected_title_back(string title, string expected)
        {
            _artist.Name = title;
            _namingConfig.StandardTrackFormat = "{Artist CleanNameThe}";

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile)
                   .Should().Be(expected);
        }

        [TestCase("A")]
        [TestCase("Anne")]
        [TestCase("Theodore")]
        [TestCase("3%")]
        public void should_not_change_title(string title)
        {
            _artist.Name = title;
            _namingConfig.StandardTrackFormat = "{Artist CleanNameThe}";

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile)
                   .Should().Be(title);
        }
    }
}
