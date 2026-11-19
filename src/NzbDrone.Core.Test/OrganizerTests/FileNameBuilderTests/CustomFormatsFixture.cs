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
    public class CustomFormatsFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private AlbumRelease _release;
        private Track _track;
        private TrackFile _trackFile;
        private NamingConfig _namingConfig;

        private List<CustomFormat> _customFormats;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .With(s => s.Name = "Alien Ant Farm")
                    .Build();

            _album = Builder<Album>
                    .CreateNew()
                    .With(s => s.Title = "Anthology")
                    .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { new Medium { Number = 1 } })
                .Build();

            _track = Builder<Track>.CreateNew()
                            .With(e => e.Title = "City Sushi")
                            .With(e => e.AbsoluteTrackNumber = 6)
                            .With(e => e.AlbumRelease = _release)
                            .Build();

            _trackFile = new TrackFile { Quality = new QualityModel(Quality.MP3_320), ReleaseGroup = "LidarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _customFormats = new List<CustomFormat>()
            {
                new CustomFormat()
                {
                    Name = "INTERNAL",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat()
                {
                    Name = "AMZN",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat()
                {
                    Name = "NAME WITH SPACES",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat()
                {
                    Name = "NotIncludedFormat",
                    IncludeCustomFormatWhenRenaming = false
                }
            };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [TestCase("{Custom Formats}", "INTERNAL AMZN NAME WITH SPACES")]
        public void should_replace_custom_formats(string format, string expected)
        {
            _namingConfig.StandardTrackFormat = format;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile, customFormats: _customFormats)
                   .Should().Be(expected);
        }

        [TestCase("{Custom Formats}", "")]
        public void should_replace_custom_formats_with_no_custom_formats(string format, string expected)
        {
            _namingConfig.StandardTrackFormat = format;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile, customFormats: new List<CustomFormat>())
                   .Should().Be(expected);
        }

        [TestCase("{Custom Format}", "")]
        [TestCase("{Custom Format:INTERNAL}", "INTERNAL")]
        [TestCase("{Custom Format:AMZN}", "AMZN")]
        [TestCase("{Custom Format:NAME WITH SPACES}", "NAME WITH SPACES")]
        [TestCase("{Custom Format:DOESNOTEXIST}", "")]
        [TestCase("{Custom Format:INTERNAL} - {Custom Format:AMZN}", "INTERNAL - AMZN")]
        [TestCase("{Custom Format:AMZN} - {Custom Format:INTERNAL}", "AMZN - INTERNAL")]
        public void should_replace_custom_format(string format, string expected)
        {
            _namingConfig.StandardTrackFormat = format;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile, customFormats: _customFormats)
                   .Should().Be(expected);
        }

        [TestCase("{Custom Format}", "")]
        [TestCase("{Custom Format:INTERNAL}", "")]
        [TestCase("{Custom Format:AMZN}", "")]
        public void should_replace_custom_format_with_no_custom_formats(string format, string expected)
        {
            _namingConfig.StandardTrackFormat = format;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile, customFormats: new List<CustomFormat>())
                   .Should().Be(expected);
        }
    }
}
