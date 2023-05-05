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
    public class ReplaceCharacterFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private AlbumRelease _release;
        private Track _track;
        private TrackFile _trackFiles;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .With(s => s.Name = "South Park")
                    .Build();

            _album = Builder<Album>
                    .CreateNew()
                    .With(s => s.Title = "Some Album")
                    .Build();

            _release = Builder<AlbumRelease>
                    .CreateNew()
                    .With(s => s.Media = new List<Medium> { new Medium { Number = 1 } })
                    .Build();

            _track = Builder<Track>.CreateNew()
                            .With(e => e.Title = "City Sushi")
                            .With(e => e.AbsoluteTrackNumber = 15)
                            .With(e => e.AlbumRelease = _release)
                            .Build();

            _trackFiles = new TrackFile { Quality = new QualityModel(Quality.FLAC), ReleaseGroup = "LidarrTest" };

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

        // { "\\", "/", "<", ">", "?", "*", ":", "|", "\"" };
        //        { "+", "+", "", "", "!", "-", " -", "", "" };
        [TestCase("CSI: Crime Scene Investigation", "CSI - Crime Scene Investigation")]
        [TestCase("Code:Breaker", "Code-Breaker")]
        [TestCase("Back Slash\\", "Back Slash+")]
        [TestCase("Forward Slash/", "Forward Slash+")]
        [TestCase("Greater Than>", "Greater Than")]
        [TestCase("Less Than<", "Less Than")]
        [TestCase("Question Mark?", "Question Mark!")]
        [TestCase("Aster*sk", "Aster-sk")]
        [TestCase("Colon: Two Periods", "Colon - Two Periods")]
        [TestCase("Pipe|", "Pipe")]
        [TestCase("Quotes\"", "Quotes")]
        public void should_replace_illegal_characters(string title, string expected)
        {
            _artist.Name = title;
            _namingConfig.StandardTrackFormat = "{Artist Name}";

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFiles)
                   .Should().Be(expected);
        }
    }
}
