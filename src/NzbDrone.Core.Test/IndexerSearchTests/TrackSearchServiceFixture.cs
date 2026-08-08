using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class TrackSearchServiceFixture : CoreTest<AlbumSearchService>
    {
        [Test]
        public void should_target_only_requested_recordings()
        {
            var album = new Album { Id = 10, AlbumType = PrimaryAlbumType.Album.Name };
            var release = new AlbumRelease { Id = 11, AlbumId = album.Id, Album = album };
            var track = new Track
            {
                Id = 20,
                ForeignRecordingId = "recording",
                AlbumReleaseId = release.Id,
                AlbumRelease = release
            };
            var decision = new DownloadDecision(new RemoteAlbum());
            var decisions = new List<DownloadDecision> { decision };

            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { track });
            Mocker.GetMock<IReleaseService>()
                  .Setup(service => service.GetReleasesByRecordingIds(It.IsAny<List<string>>()))
                  .Returns(new List<AlbumRelease> { release });
            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracksByReleases(It.IsAny<List<int>>()))
                  .Returns(new List<Track> { track });
            Mocker.GetMock<IAlbumService>()
                  .Setup(service => service.GetAlbums(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Album> { album });
            Mocker.GetMock<ISearchForReleases>()
                  .Setup(service => service.AlbumSearch(album.Id, false, true, false))
                  .Returns(Task.FromResult(decisions));
            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(service => service.ProcessDecisions(decisions, true))
                  .Returns(Task.FromResult(new ProcessedDecisions(decisions, new List<DownloadDecision>(), new List<DownloadDecision>())));

            Subject.Execute(new TrackSearchCommand
            {
                TrackIds = new List<int> { track.Id },
                Trigger = CommandTrigger.Manual
            });

            decision.RemoteAlbum.TargetRecordingIds.Should().Equal(track.ForeignRecordingId);
            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Verify(service => service.ProcessDecisions(decisions, true), Times.Once());
        }

        [Test]
        public void should_search_every_album_type_containing_the_requested_recording()
        {
            var originalAlbum = new Album { Id = 10, AlbumType = PrimaryAlbumType.Album.Name };
            var compilationAlbum = new Album
            {
                Id = 20,
                AlbumType = PrimaryAlbumType.Album.Name,
                SecondaryTypes = new List<SecondaryAlbumType> { SecondaryAlbumType.Compilation }
            };
            var epAlbum = new Album { Id = 30, AlbumType = PrimaryAlbumType.EP.Name };
            var singleAlbum = new Album { Id = 40, AlbumType = PrimaryAlbumType.Single.Name };
            var albums = new List<Album> { originalAlbum, compilationAlbum, epAlbum, singleAlbum };
            var releases = albums.Select((album, index) => new AlbumRelease
            {
                Id = 100 + index,
                AlbumId = album.Id,
                Album = album
            }).ToList();
            var tracks = releases.Select((release, index) => new Track
            {
                Id = 200 + index,
                ForeignRecordingId = "recording",
                AlbumReleaseId = release.Id,
                AlbumRelease = release
            }).ToList();
            var decisions = albums.ToDictionary(
                album => album.Id,
                album => new List<DownloadDecision> { new DownloadDecision(new RemoteAlbum()) });

            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { tracks[0] });
            Mocker.GetMock<IReleaseService>()
                  .Setup(service => service.GetReleasesByRecordingIds(It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "recording" }))))
                  .Returns(releases);
            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracksByReleases(It.Is<List<int>>(ids => ids.SequenceEqual(releases.Select(release => release.Id)))))
                  .Returns(tracks);
            Mocker.GetMock<IAlbumService>()
                  .Setup(service => service.GetAlbums(It.IsAny<IEnumerable<int>>()))
                  .Returns(albums);

            foreach (var album in albums)
            {
                var albumDecisions = decisions[album.Id];
                Mocker.GetMock<ISearchForReleases>()
                      .Setup(service => service.AlbumSearch(album.Id, false, true, false))
                      .Returns(Task.FromResult(albumDecisions));
            }

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(service => service.ProcessDecisions(It.IsAny<List<DownloadDecision>>(), true))
                  .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>())));

            Subject.Execute(new TrackSearchCommand
            {
                TrackIds = new List<int> { tracks[0].Id },
                Trigger = CommandTrigger.Manual
            });

            foreach (var album in albums)
            {
                Mocker.GetMock<ISearchForReleases>()
                      .Verify(service => service.AlbumSearch(album.Id, false, true, false), Times.Once());
                decisions[album.Id].Single().RemoteAlbum.TargetRecordingIds.Should().Equal("recording");
            }

            decisions[compilationAlbum.Id].Single().RemoteAlbum.TrackSearchPriority.Should().Be(1);
            decisions[epAlbum.Id].Single().RemoteAlbum.TrackSearchPriority.Should().Be(2);
            decisions[singleAlbum.Id].Single().RemoteAlbum.TrackSearchPriority.Should().Be(3);
            decisions[originalAlbum.Id].Single().RemoteAlbum.TrackSearchPriority.Should().Be(4);

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Verify(service => service.ProcessDecisions(
                      It.Is<List<DownloadDecision>>(items => items.Count == albums.Count),
                      true), Times.Once());
        }

        [Test]
        public async Task should_give_discographies_the_highest_track_search_priority()
        {
            var album = new Album { Id = 10, AlbumType = PrimaryAlbumType.Album.Name };
            var release = new AlbumRelease { Id = 11, AlbumId = album.Id, Album = album };
            var track = new Track
            {
                Id = 20,
                ForeignRecordingId = "recording",
                AlbumReleaseId = release.Id,
                AlbumRelease = release
            };
            var decision = new DownloadDecision(new RemoteAlbum
            {
                ParsedAlbumInfo = new ParsedAlbumInfo { Discography = true }
            });

            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { track });
            Mocker.GetMock<IReleaseService>()
                  .Setup(service => service.GetReleasesByRecordingIds(It.IsAny<List<string>>()))
                  .Returns(new List<AlbumRelease> { release });
            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracksByReleases(It.IsAny<List<int>>()))
                  .Returns(new List<Track> { track });
            Mocker.GetMock<IAlbumService>()
                  .Setup(service => service.GetAlbums(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Album> { album });
            Mocker.GetMock<ISearchForReleases>()
                  .Setup(service => service.AlbumSearch(album.Id, false, true, false))
                  .Returns(Task.FromResult(new List<DownloadDecision> { decision }));

            var decisions = await Subject.TrackSearch(new List<int> { track.Id }, true, false);

            decisions.Single().RemoteAlbum.TrackSearchPriority.Should().Be(0);
        }

        [Test]
        public async Task should_merge_the_same_release_found_for_multiple_requested_recordings()
        {
            var album1 = new Album { Id = 10, AlbumType = PrimaryAlbumType.Album.Name };
            var album2 = new Album
            {
                Id = 20,
                AlbumType = PrimaryAlbumType.Album.Name,
                SecondaryTypes = new List<SecondaryAlbumType> { SecondaryAlbumType.Compilation }
            };
            var release1 = new AlbumRelease { Id = 100, AlbumId = album1.Id, Album = album1 };
            var release2 = new AlbumRelease { Id = 200, AlbumId = album2.Id, Album = album2 };
            var track1 = new Track
            {
                Id = 1000,
                ForeignRecordingId = "recording-1",
                AlbumReleaseId = release1.Id,
                AlbumRelease = release1
            };
            var track2 = new Track
            {
                Id = 2000,
                ForeignRecordingId = "recording-2",
                AlbumReleaseId = release2.Id,
                AlbumRelease = release2
            };

            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { track1, track2 });
            Mocker.GetMock<IReleaseService>()
                  .Setup(service => service.GetReleasesByRecordingIds(It.IsAny<List<string>>()))
                  .Returns(new List<AlbumRelease> { release1, release2 });
            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracksByReleases(It.IsAny<List<int>>()))
                  .Returns(new List<Track> { track1, track2 });
            Mocker.GetMock<IAlbumService>()
                  .Setup(service => service.GetAlbums(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Album> { album1, album2 });

            foreach (var album in new[] { album1, album2 })
            {
                var decision = new DownloadDecision(new RemoteAlbum
                {
                    Release = new ReleaseInfo { Guid = "same-release" }
                });
                Mocker.GetMock<ISearchForReleases>()
                      .Setup(service => service.AlbumSearch(album.Id, false, true, false))
                      .Returns(Task.FromResult(new List<DownloadDecision> { decision }));
            }

            var decisions = await Subject.TrackSearch(new List<int> { track1.Id, track2.Id }, true, false);

            decisions.Should().ContainSingle();
            decisions.Single().RemoteAlbum.TargetRecordingIds.Should().BeEquivalentTo("recording-1", "recording-2");
            decisions.Single().RemoteAlbum.TrackSearchPriority.Should().Be(1);
        }
    }
}
