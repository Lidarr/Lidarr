using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class AudioTagServicePreferredNameFixture : CoreTest<AudioTagService>
    {
        [Test]
        public void should_write_preferred_name_into_artist_and_album_artist_tags()
        {
            var metadata = new ArtistMetadata
            {
                Name = "刘柏辛",
                ForeignArtistId = "96fc9b47-377d-4706-ae8a-cf0d83bcaf7a",
                Genres = new List<string>()
            };
            var artist = new Artist
            {
                Name = metadata.Name,
                ForeignArtistId = metadata.ForeignArtistId,
                PreferredName = "Lexie Liu",
                Metadata = metadata
            };
            var album = new Album
            {
                Title = "2030",
                AlbumType = "Album",
                Artist = artist,
                ForeignAlbumId = "album"
            };
            var release = new AlbumRelease
            {
                Status = "Official",
                Album = album,
                ForeignReleaseId = "release",
                Media = new List<Medium> { new Medium { Number = 1, Format = "Digital Media" } }
            };
            var track = new Track
            {
                Title = "Song",
                AbsoluteTrackNumber = 1,
                MediumNumber = 1,
                ArtistMetadata = metadata,
                AlbumRelease = release,
                ForeignRecordingId = "recording",
                ForeignTrackId = "track"
            };

            release.Tracks = new List<Track> { track };

            Mocker.GetMock<IConfigService>()
                .Setup(x => x.EmbedCoverArt)
                .Returns(false);
            Mocker.GetMock<IArtistService>()
                .Setup(x => x.FindById(metadata.ForeignArtistId))
                .Returns(artist);

            var tags = Subject.GetTrackMetadata(new TrackFile
            {
                Path = "song.flac",
                Tracks = new List<Track> { track }
            });

            tags.Performers.Should().Equal("Lexie Liu");
            tags.AlbumArtists.Should().Equal("Lexie Liu");
        }
    }
}
