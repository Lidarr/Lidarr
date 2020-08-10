using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    public class BuildFilePathFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_clean_artist_folder_when_it_contains_illegal_characters_in_album_or_artist_title()
        {
            var filename = @"02 - Track Title";
            var expectedPath = @"C:\Test\Fake- The Artist\02 - Track Title.flac";

            var fakeTracks = Builder<Track>.CreateListOfSize(1)
                .All()
                .With(s => s.Title = "Episode Title")
                .With(s => s.AbsoluteTrackNumber = 5)
                .Build().ToList();
            var fakeArtist = Builder<Artist>.CreateNew()
                .With(s => s.Name = "Fake: The Artist")
                .With(s => s.Path = @"C:\Test\Fake- The Artist".AsOsAgnostic())
                .Build();
            var fakeAlbum = Builder<Album>.CreateNew()
                .With(e => e.ArtistId = fakeArtist.Id)
                .Build();
            var fakeTrackFile = Builder<TrackFile>.CreateNew()
                .With(s => s.SceneName = filename)
                .With(f => f.Artist = fakeArtist)
                .Build();

            Subject.BuildTrackFilePath(fakeTracks, fakeArtist, fakeAlbum, fakeTrackFile, ".flac").Should().Be(expectedPath.AsOsAgnostic());
        }
    }
}
