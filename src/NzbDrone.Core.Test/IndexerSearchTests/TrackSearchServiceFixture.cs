using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
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
            var album = new Album { Id = 10 };
            var release = new AlbumRelease { AlbumId = album.Id, Album = album };
            var track = new Track
            {
                Id = 20,
                ForeignRecordingId = "recording",
                AlbumRelease = release
            };
            var decision = new DownloadDecision(new RemoteAlbum());
            var decisions = new List<DownloadDecision> { decision };

            Mocker.GetMock<ITrackService>()
                  .Setup(service => service.GetTracks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Track> { track });
            Mocker.GetMock<ISearchForReleases>()
                  .Setup(service => service.AlbumSearch(album.Id, false, true, false))
                  .Returns(Task.FromResult(decisions));
            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(service => service.ProcessDecisions(decisions))
                  .Returns(Task.FromResult(new ProcessedDecisions(decisions, new List<DownloadDecision>(), new List<DownloadDecision>())));

            Subject.Execute(new TrackSearchCommand
            {
                TrackIds = new List<int> { track.Id },
                Trigger = CommandTrigger.Manual
            });

            decision.RemoteAlbum.TargetRecordingIds.Should().Equal(track.ForeignRecordingId);
            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Verify(service => service.ProcessDecisions(decisions), Times.Once());
        }
    }
}
