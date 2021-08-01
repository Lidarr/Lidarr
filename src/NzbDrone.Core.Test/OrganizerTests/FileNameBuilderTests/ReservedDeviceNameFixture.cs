using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class ReservedDeviceNameFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private Track _track1;
        private Medium _medium;
        private AlbumRelease _release;
        private TrackFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .With(s => s.Name = "Tim Park")
                    .Build();

            _medium = Builder<Medium>
                .CreateNew()
                .With(m => m.Number = 3)
                .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { _medium })
                .With(s => s.Monitored = true)
                .Build();

            _album = Builder<Album>
                .CreateNew()
                .With(s => s.Title = "Hybrid Theory")
                .With(s => s.AlbumType = "Album")
                .With(s => s.Disambiguation = "The Best Album")
                .With(s => s.Genres = new List<string> { "Rock" })
                .With(s => s.ForeignAlbumId = Guid.NewGuid().ToString())
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _track1 = Builder<Track>.CreateNew()
                            .With(e => e.Title = "City Sushi")
                            .With(e => e.TrackNumber = "6")
                            .With(e => e.AbsoluteTrackNumber = 6)
                            .With(e => e.AlbumRelease = _release)
                            .With(e => e.MediumNumber = _medium.Number)
                            .With(e => e.ArtistMetadata = _artist.Metadata)
                            .Build();

            _trackFile = new TrackFile { Quality = new QualityModel(Quality.FLAC), ReleaseGroup = "SonarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [TestCase("Con Game", "Con_Game")]
        [TestCase("Com1 Sat", "Com1_Sat")]
        public void should_replace_reserved_device_name_in_artist_folder(string title, string expected)
        {
            _artist.Name = title;
            _namingConfig.ArtistFolderFormat = "{Artist.Name}";

            Subject.GetArtistFolder(_artist).Should().Be(expected);
        }

        [TestCase("Con Game", "Con_Game")]
        [TestCase("Com1 Sat", "Com1_Sat")]
        public void should_replace_reserved_device_name_in_file_name(string title, string expected)
        {
            _artist.Name = title;
            _namingConfig.StandardTrackFormat = "{Artist.Name} - {Track Title}";

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile).Should().Be($"{expected} - City Sushi");
        }
    }
}
