using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class TrackRepositorySongModeFixture : DbTest<TrackRepository, Track>
    {
        [Test]
        public void should_clear_album_defaults_across_releases_without_changing_files_or_other_artists()
        {
            var albumRepository = Mocker.Resolve<AlbumRepository>();
            var releaseRepository = Mocker.Resolve<ReleaseRepository>();
            var tracks = new List<Track>();
            foreach (var owner in new[] { 100, 100, 200 })
            {
                var album = Builder<Album>.CreateNew().With(item => item.Id = 0)
                    .With(item => item.ForeignAlbumId = Guid.NewGuid().ToString())
                    .With(item => item.ArtistMetadataId = owner).Build();
                albumRepository.Insert(album);
                foreach (var monitored in new[] { true, false })
                {
                    var release = Builder<AlbumRelease>.CreateNew().With(item => item.Id = 0)
                        .With(item => item.ForeignReleaseId = Guid.NewGuid().ToString())
                        .With(item => item.AlbumId = album.Id).With(item => item.Monitored = monitored).Build();
                    releaseRepository.Insert(release);
                    var track = Builder<Track>.CreateNew().With(item => item.Id = 0)
                        .With(item => item.ForeignTrackId = Guid.NewGuid().ToString())
                        .With(item => item.AlbumReleaseId = release.Id).With(item => item.Monitored = true)
                        .With(item => item.TrackFileId = 123).Build();
                    Subject.Insert(track);
                    tracks.Add(track);
                }
            }

            Subject.UnmonitorArtist(100);

            Subject.All().Should().HaveCount(6).And.OnlyContain(track => track.TrackFileId == 123);
            Subject.Get(tracks[0].Id).Monitored.Should().BeFalse();
            Subject.Get(tracks[1].Id).Monitored.Should().BeFalse();
            Subject.Get(tracks[2].Id).Monitored.Should().BeFalse();
            Subject.Get(tracks[3].Id).Monitored.Should().BeFalse();
            Subject.Get(tracks[4].Id).Monitored.Should().BeTrue();
            Subject.Get(tracks[5].Id).Monitored.Should().BeTrue();
        }
    }
}
