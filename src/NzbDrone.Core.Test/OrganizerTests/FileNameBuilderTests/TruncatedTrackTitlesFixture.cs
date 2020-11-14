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
    public class TruncatedTrackTitlesFixture : CoreTest<FileNameBuilder>
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
                .With(s => s.Name = "Avenged Sevenfold")
                .Build();

            _album = Builder<Album>
                .CreateNew()
                .With(s => s.Title = "Hail to the King")
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
                                            .With(e => e.Title = "First Track Title 1")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 1)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "Another Track Title")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 2)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "Yet Another Track Title")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 3)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "Yet Another Track Title Take 2")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 4)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "Yet Another Track Title Take 3")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 5)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "Yet Another Track Title Take 4")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 6)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build(),

                            Builder<Track>.CreateNew()
                                            .With(e => e.Title = "A Really Really Really Really Long Track Title")
                                            .With(e => e.MediumNumber = 1)
                                            .With(e => e.AbsoluteTrackNumber = 7)
                                            .With(e => e.AlbumRelease = _release)
                                            .Build()
                        };

            _trackFile = new TrackFile { Quality = new QualityModel(Quality.MP3_320), ReleaseGroup = "LidarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        private void GivenProper()
        {
            _trackFile.Quality.Revision.Version = 2;
        }

        [Test]
        public void should_truncate_with_extension()
        {
            _artist.Name = "The Fantastic Life of Mr. Sisko";

            _tracks[0].AbsoluteTrackNumber = 18;
            _tracks[0].Title = "This title has to be 197 characters in length, combined with the series title, quality and episode number it becomes 254ish and the extension puts it above the 255 limit and triggers the truncation";
            _trackFile.Quality.Quality = Quality.FLAC;
            _tracks = _tracks.Take(1).ToList();
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            var result = Subject.BuildTrackFileName(_tracks, _artist, _album, _trackFile, ".flac");
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("The Fantastic Life of Mr. Sisko - Hail to the King - 18 - This title has to be 197 characters in length, combined with the series title, quality and episode number it becomes 254ish and the extension puts it above the 255 limit and triggers... [FLAC].flac");
        }

        [Test]
        public void should_truncate_with_ellipsis_between_first_and_last_episode_titles()
        {
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            var result = Subject.BuildTrackFileName(_tracks, _artist, _album, _trackFile);
            result.Length.Should().BeLessOrEqualTo(255);
            result.Should().Be("Avenged Sevenfold - Hail to the King - 01 - First Track Title 1...A Really Really Really Really Long Track Title [MP3-320]");
        }

        [Test]
        public void should_truncate_with_ellipsis_if_only_first_episode_title_fits()
        {
            _artist.Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes";
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            var result = Subject.BuildTrackFileName(_tracks, _artist, _album, _trackFile);
            result.Should().Be("Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes - Hail to the King - 01 - First Track Title 1... [MP3-320]");
            result.Length.Should().BeLessOrEqualTo(255);
        }

        [Test]
        public void should_truncate_first_episode_title_with_ellipsis_if_only_partially_fits()
        {
            _artist.Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu Cras";
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            var result = Subject.BuildTrackFileName(new List<Track> { _tracks.First() }, _artist, _album, _trackFile);
            result.Should().Be("Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu Cras - Hail to the King - 01 - First... [MP3-320]");
            result.Length.Should().BeLessOrEqualTo(255);
        }

        [Test]
        public void should_truncate_titles_measuring_artist_title_bytes()
        {
            _artist.Name = "Lor\u00E9m ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu";
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            var result = Subject.BuildTrackFileName(new List<Track> { _tracks.First() }, _artist, _album, _trackFile);
            result.GetByteCount().Should().BeLessOrEqualTo(255);

            result.Should().Be("Lor\u00E9m ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu - Hail to the King - 01 - First Trac... [MP3-320]");
        }

        [Test]
        public void should_truncate_titles_measuring_episode_title_bytes()
        {
            _artist.Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu";
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            _tracks.First().Title = "Episod\u00E9 Track Title";

            var result = Subject.BuildTrackFileName(new List<Track> { _tracks.First() }, _artist, _album, _trackFile);
            result.GetByteCount().Should().BeLessOrEqualTo(255);

            result.Should().Be("Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu - Hail to the King - 01 - Episod\u00E9 Tr... [MP3-320]");
        }

        [Test]
        public void should_truncate_titles_measuring_episode_title_bytes_middle()
        {
            _artist.Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu";
            _namingConfig.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title} [{Quality Title}]";

            _tracks.First().Title = "Episode Track T\u00E9tle";

            var result = Subject.BuildTrackFileName(new List<Track> { _tracks.First() }, _artist, _album, _trackFile);
            result.GetByteCount().Should().BeLessOrEqualTo(255);

            result.Should().Be("Lorem ipsum dolor sit amet, consectetur adipiscing elit Maecenas et magna sem Morbi vitae volutpat quam, id porta arcu Orci varius natoque penatibus et magnis dis parturient montes nascetur ridiculus musu - Hail to the King - 01 - Episode Tra... [MP3-320]");
        }
    }
}
