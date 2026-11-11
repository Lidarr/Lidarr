using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HistoryTests
{
    [TestFixture]
    public class HistoryRepositoryFixture : DbTest<EntityHistoryRepository, EntityHistory>
    {
        [Test]
        public void should_read_write_dictionary()
        {
            var history = Builder<EntityHistory>.CreateNew()
                .With(c => c.Quality = new QualityModel())
                .BuildNew();

            history.Data.Add("key1", "value1");
            history.Data.Add("key2", "value2");

            Subject.Insert(history);

            StoredModel.Data.Should().HaveCount(2);
        }

        [Test]
        public void should_roundtrip_song_request_metadata_using_persisted_camel_case_keys()
        {
            var history = Builder<EntityHistory>.CreateNew()
                .With(item => item.Quality = new QualityModel())
                .BuildNew();
            history.Data.Add("TargetRecordingIds", "[\"wanted\"]");
            history.Data.Add("RecordingId", "wanted");

            Subject.Insert(history);

            StoredModel.Data.Should().ContainKey("targetRecordingIds").WhoseValue.Should().Be("[\"wanted\"]");
            StoredModel.Data.Should().ContainKey("recordingId").WhoseValue.Should().Be("wanted");
        }

        [Test]
        public void should_get_download_history()
        {
            var historyBluray = Builder<EntityHistory>.CreateNew()
                .With(c => c.Quality = new QualityModel(Quality.MP3_320))
                .With(c => c.ArtistId = 12)
                .With(c => c.EventType = EntityHistoryEventType.Grabbed)
                .BuildNew();

            var historyDvd = Builder<EntityHistory>.CreateNew()
                .With(c => c.Quality = new QualityModel(Quality.MP3_192))
                .With(c => c.ArtistId = 12)
                .With(c => c.EventType = EntityHistoryEventType.Grabbed)
             .BuildNew();

            Subject.Insert(historyBluray);
            Subject.Insert(historyDvd);

            var downloadHistory = Subject.FindDownloadHistory(12, new QualityModel(Quality.MP3_320));

            downloadHistory.Should().HaveCount(1);
        }
    }
}
