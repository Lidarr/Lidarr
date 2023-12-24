using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Blocklisting
{
    [TestFixture]
    public class BlocklistServiceFixture : CoreTest<BlocklistService>
    {
        private DownloadFailedEvent _event;

        [SetUp]
        public void Setup()
        {
            _event = new DownloadFailedEvent
            {
                ArtistId = 12345,
                AlbumIds = new List<int> { 1 },
                Quality = new QualityModel(Quality.MP3_320),
                SourceTitle = "artist.name.album.title",
                DownloadClient = "SabnzbdClient",
                DownloadId = "Sabnzbd_nzo_2dfh73k"
            };

            _event.Data.Add("publishedDate", DateTime.UtcNow.ToString("s") + "Z");
            _event.Data.Add("size", "1000");
            _event.Data.Add("indexer", "nzbs.org");
            _event.Data.Add("protocol", nameof(UsenetDownloadProtocol));
            _event.Data.Add("message", "Marked as failed");

            Mocker.SetConstant<IBlocklistForProtocol>(Mocker.Resolve<UsenetBlocklist>());
        }

        [Test]
        public void should_add_to_repository()
        {
            Subject.Handle(_event);

            Mocker.GetMock<IBlocklistRepository>()
                .Verify(v => v.Insert(It.Is<Blocklist>(b => b.AlbumIds == _event.AlbumIds)), Times.Once());
        }

        [Test]
        public void should_add_to_repository_missing_size_and_protocol()
        {
            Subject.Handle(_event);

            _event.Data.Remove("size");
            _event.Data.Remove("protocol");

            Mocker.GetMock<IBlocklistRepository>()
                .Verify(v => v.Insert(It.Is<Blocklist>(b => b.AlbumIds == _event.AlbumIds)), Times.Once());
        }
    }
}
