using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class AlbumAndSongDownloadFixture : CoreTest<TrackedDownloadService>
    {
        private List<EntityHistory> _history;
        private DownloadClientDefinition _client;

        [SetUp]
        public void Setup()
        {
            _history = new List<EntityHistory>
            {
                new EntityHistory
                {
                    EventType = EntityHistoryEventType.Grabbed, ArtistId = 1, AlbumId = 2,
                    SourceTitle = "Audio Artist - Audio Album [2018 - FLAC]",
                    Artist = new Artist { Id = 1, Name = "Audio Artist" },
                    Album = new Album { Id = 2, Title = "Audio Album" }
                }
            };
            _client = new DownloadClientDefinition { Id = 1, Protocol = "TorrentDownloadProtocol" };
            Mocker.GetMock<IHistoryService>().Setup(service => service.FindByDownloadId(It.IsAny<string>())).Returns(_history);
            Mocker.GetMock<IParsingService>().Setup(service => service.Map(It.IsAny<ParsedAlbumInfo>(), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                .Returns(new RemoteAlbum { Artist = new Artist { Id = 1 }, Albums = new List<Album> { new Album { Id = 2 } } });
        }

        private DownloadClientItem Download()
        {
            return new DownloadClientItem
            {
                DownloadId = "album-or-song",
                Title = "Audio Artist - Audio Album [2018 - FLAC]",
                DownloadClientInfo = new DownloadClientItemClientInfo { Id = _client.Id, Protocol = _client.Protocol, Name = "Test client" }
            };
        }

        [Test]
        public void should_recognize_a_whole_album_grab_without_recording_targets()
        {
            Subject.TrackDownload(_client, Download()).DownloadItem.IsFullAlbumRequest.Should().BeTrue();
        }

        [Test]
        public void should_keep_recording_targets_for_an_album_used_to_download_one_song()
        {
            _history[0].Data["TargetRecordingIds"] = "[\"wanted\"]";

            var download = Subject.TrackDownload(_client, Download()).DownloadItem;

            download.IsFullAlbumRequest.Should().BeFalse();
            download.TargetRecordingIds.Should().Equal("wanted");
        }

        [Test]
        public void should_keep_requested_recordings_after_history_json_roundtrip()
        {
            _history[0].Data["TargetRecordingIds"] = "[\"wanted\"]";
            _history[0].Data = Json.Deserialize<Dictionary<string, string>>(_history[0].Data.ToJson());
            _history[0].Data.Should().ContainKey("targetRecordingIds");

            var download = Subject.TrackDownload(_client, Download()).DownloadItem;

            download.IsFullAlbumRequest.Should().BeFalse();
            download.TargetRecordingIds.Should().Equal("wanted");
        }

        [Test]
        public void should_not_assume_unknown_downloads_request_a_whole_album()
        {
            _history.Clear();

            Subject.TrackDownload(_client, Download()).DownloadItem.IsFullAlbumRequest.Should().BeFalse();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_keep_request_intent_when_a_client_refreshes_an_import_pending_item(bool wholeAlbum)
        {
            if (!wholeAlbum)
            {
                _history[0].Data["TargetRecordingIds"] = "[\"wanted\"]";
            }

            var tracked = Subject.TrackDownload(_client, Download());
            tracked.State = TrackedDownloadState.ImportPending;
            var refreshed = Subject.TrackDownload(_client, Download());

            refreshed.DownloadItem.IsFullAlbumRequest.Should().Be(wholeAlbum);
            if (!wholeAlbum)
            {
                refreshed.DownloadItem.TargetRecordingIds.Should().Equal("wanted");
            }
        }
    }
}
