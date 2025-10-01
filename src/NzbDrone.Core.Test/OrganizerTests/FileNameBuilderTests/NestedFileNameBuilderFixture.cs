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
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    public class NestedFileNameBuilderFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private Album _album;
        private Medium _medium;
        private Medium _medium2;
        private AlbumRelease _release;
        private Track _track1;
        private TrackFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .With(s => s.Name = "Metallica")
                    .With(s => s.Metadata = new ArtistMetadata
                    {
                        Disambiguation = "US Metal Band",
                        Name = "Metallica"
                    })
                    .Build();

            _medium = Builder<Medium>
                .CreateNew()
                .With(m => m.Number = 3)
                .With(m => m.Name = "Hybrid Theory")
                .Build();

            _medium2 = Builder<Medium>
                .CreateNew()
                .With(m => m.Number = 4)
                .With(m => m.Name = "Reanimation")
                .Build();

            _release = Builder<AlbumRelease>
                .CreateNew()
                .With(s => s.Media = new List<Medium> { _medium })
                .With(s => s.Monitored = true)
                .Build();

            _album = Builder<Album>
                .CreateNew()
                .With(s => s.Title = "...And Justice For All")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .With(s => s.AlbumType = "Album")
                .With(s => s.Disambiguation = "The Best Album")
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _track1 = Builder<Track>.CreateNew()
                            .With(e => e.Title = "City Sushi")
                            .With(e => e.AbsoluteTrackNumber = 6)
                            .With(e => e.AlbumRelease = _release)
                            .With(e => e.MediumNumber = _medium.Number)
                            .Build();

            _trackFile = Builder<TrackFile>.CreateNew()
                .With(e => e.Quality = new QualityModel(Quality.MP3_256))
                .With(e => e.ReleaseGroup = "LidarrTest")
                .With(e => e.MediaInfo = new Parser.Model.MediaInfoModel
                {
                    AudioBitrate = 320,
                    AudioBits = 16,
                    AudioChannels = 2,
                    AudioFormat = "Flac Audio",
                    AudioSampleRate = 44100
                }).Build();

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_forward_slash()
        {
            _namingConfig.StandardTrackFormat = "{Album Title} {(Release Year)}/{Artist Name} - {track:00} [{Quality Title}] {[Quality Proper]}";

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile)
                   .Should().Be("And Justice For All (2020)\\Metallica - 06 [MP3-256]".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_back_slash()
        {
            _namingConfig.StandardTrackFormat = "{Album Title} {(Release Year)}\\{Artist Name} - {track:00} [{Quality Title}] {[Quality Proper]}";

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile)
                   .Should().Be("And Justice For All (2020)\\Metallica - 06 [MP3-256]".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_multi_track_filename_with_forward_slash()
        {
            _namingConfig.MultiDiscTrackFormat = "{Album Title} {(Release Year)}/CD {medium:00}/{Artist Name} - {track:00} [{Quality Title}] {[Quality Proper]}";

            _release.Media.Add(_medium2);

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile)
                   .Should().Be("And Justice For All (2020)\\CD 03\\Metallica - 06 [MP3-256]".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_multi_track_filename_with_back_slash()
        {
            _namingConfig.MultiDiscTrackFormat = "{Album Title} {(Release Year)}\\CD {medium:00}\\{Artist Name} - {track:00} [{Quality Title}] {[Quality Proper]}";

            _release.Media.Add(_medium2);

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile)
                   .Should().Be("And Justice For All (2020)\\CD 03\\Metallica - 06 [MP3-256]".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_multi_track_filename_with_medium_name()
        {
            _namingConfig.MultiDiscTrackFormat = "{Album Title} {(Release Year)}/CD {medium:00} - {Medium Name}/{Artist Name} - {track:00} [{Quality Title}] {[Quality Proper]}";

            _release.Media.Add(_medium2);

            Subject.BuildTrackFileName(new List<Track> { _track1 }, _artist, _album, _trackFile)
                .Should().Be("Hybrid Theory (2020)\\CD 03 - Hybrid Theory\\Linkin Park - 06 [MP3-256]".AsOsAgnostic());
        }
    }
}
