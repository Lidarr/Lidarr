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
    public class ColonReplacementFixture : CoreTest<FileNameBuilder>
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
                .With(s => s.Name = "Nu:Tone")
                .Build();

            _album = Builder<Album>
                .CreateNew()
                .With(s => s.Title = "Medical History")
                .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { new () { Number = 14 } })
                .Build();

            _track = Builder<Track>.CreateNew()
                .With(e => e.Title = "System: Accapella")
                .With(e => e.AbsoluteTrackNumber = 14)
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

        [Test]
        public void should_replace_colon_followed_by_space_with_space_dash_space_by_default()
        {
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {Track Title}";

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile)
                   .Should().Be("Nu-Tone - Medical History - System - Accapella");
        }

        [TestCase("System: Accapella", ColonReplacementFormat.Smart, "Nu-Tone - Medical History - System - Accapella")]
        [TestCase("System: Accapella", ColonReplacementFormat.Dash, "Nu-Tone - Medical History - System- Accapella")]
        [TestCase("System: Accapella", ColonReplacementFormat.Delete, "NuTone - Medical History - System Accapella")]
        [TestCase("System: Accapella", ColonReplacementFormat.SpaceDash, "Nu -Tone - Medical History - System - Accapella")]
        [TestCase("System: Accapella", ColonReplacementFormat.SpaceDashSpace, "Nu - Tone - Medical History - System - Accapella")]
        public void should_replace_colon_followed_by_space_with_expected_result(string trackTitle, ColonReplacementFormat replacementFormat, string expected)
        {
            _track.Title = trackTitle;
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {Track Title}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile)
                .Should().Be(expected);
        }

        [TestCase("Artist:Name", ColonReplacementFormat.Smart, "Artist-Name")]
        [TestCase("Artist:Name", ColonReplacementFormat.Dash, "Artist-Name")]
        [TestCase("Artist:Name", ColonReplacementFormat.Delete, "ArtistName")]
        [TestCase("Artist:Name", ColonReplacementFormat.SpaceDash, "Artist -Name")]
        [TestCase("Artist:Name", ColonReplacementFormat.SpaceDashSpace, "Artist - Name")]
        public void should_replace_colon_with_expected_result(string artistName, ColonReplacementFormat replacementFormat, string expected)
        {
            _artist.Name = artistName;
            _namingConfig.StandardTrackFormat = "{Artist Name}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildTrackFileName(new List<Track> { _track }, _artist, _album, _trackFile)
                .Should().Be(expected);
        }
    }
}
