using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]

    public class GetArtistFolderFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [TestCase("Avenged Sevenfold", "{Artist Name}", "Avenged Sevenfold")]
        [TestCase("Avenged Sevenfold", "{Artist.Name}", "Avenged.Sevenfold")]
        [TestCase("AC/DC", "{Artist Name}", "AC+DC")]
        [TestCase("In the Woods...", "{Artist.Name}", "In.the.Woods")]
        [TestCase("3OH!3", "{Artist.Name}", "3OH!3")]
        [TestCase("Avenged Sevenfold", ".{Artist.Name}.", "Avenged.Sevenfold")]
        public void should_use_artistFolderFormat_to_build_folder_name(string artistName, string format, string expected)
        {
            _namingConfig.ArtistFolderFormat = format;

            var artist = new Artist { Name = artistName };

            Subject.GetArtistFolder(artist).Should().Be(expected);
        }

        [Test]
        public void should_use_preferred_name_for_artist_folder()
        {
            var artist = new Artist
            {
                Name = "刘柏辛",
                PreferredName = "Lexie Liu"
            };

            Subject.GetArtistFolder(artist).Should().Be("Lexie Liu");
        }

        [Test]
        public void should_use_preferred_name_for_track_artist_when_it_is_the_album_artist()
        {
            _namingConfig.RenameTracks = true;
            _namingConfig.StandardTrackFormat = "{Track ArtistName}";

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            var metadata = new ArtistMetadata
            {
                Name = "刘柏辛",
                ForeignArtistId = "96fc9b47-377d-4706-ae8a-cf0d83bcaf7a"
            };
            var artist = new Artist
            {
                Name = metadata.Name,
                ForeignArtistId = metadata.ForeignArtistId,
                PreferredName = "Lexie Liu",
                Metadata = metadata
            };
            var track = new Track
            {
                Title = "Song",
                ArtistMetadata = metadata,
                AlbumRelease = new AlbumRelease
                {
                    Media = new List<Medium> { new Medium { Number = 1 } }
                }
            };

            Subject.BuildTrackFileName(new List<Track> { track }, artist, new Album(), new TrackFile { Quality = new QualityModel(Quality.MP3_320) })
                .Should().Be("Lexie Liu");
        }
    }
}
