using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;
using RestSharp;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class download_clients_rename_tv_to_musicFixture : MigrationTest<download_clients_rename_tv_to_music>
    {
        [Test]
        public void should_rename_settings_for_deluge()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("DownloadClients").Row(new
                {
                    Enable = true,
                    Name = "Deluge",
                    Implementation = "Deluge",
                    Priority = 1,
                    Settings = new
                    {
                        Host = "127.0.0.1",
                        UrlBase = "/my/",
                        TvDirectory = "abc",
                        RecentTvPriority = 1,
                        OlderTvPriority = 1
                    }.ToJson(),
                    ConfigContract = "DelugeSettings"
                });
            });

            var items = db.Query<DownloadClientDefinition67>("SELECT * FROM \"DownloadClients\"");

            items.Should().HaveCount(1);

            items.First().Settings.Should().NotContainKey("tvDirectory");
            items.First().Settings.Should().ContainKey("musicDirectory");
            items.First().Settings["musicDirectory"].Should().Be("abc");

            items.First().Settings.Should().NotContainKey("recentTvPriority");
            items.First().Settings.Should().ContainKey("recentMusicPriority");
            items.First().Settings["recentMusicPriority"].Should().Be(1);

            items.First().Settings.Should().NotContainKey("olderTvPriority");
            items.First().Settings.Should().ContainKey("olderMusicPriority");
            items.First().Settings["olderMusicPriority"].Should().Be(1);
        }

        [Test]
        public void should_rename_settings_for_qbittorrent()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("DownloadClients").Row(new
                {
                    Enable = true,
                    Name = "QBittorrent",
                    Implementation = "QBittorrent",
                    Priority = 1,
                    Settings = new
                    {
                        Host = "127.0.0.1",
                        UrlBase = "/my/",
                        TvDirectory = "abc",
                        RecentTvPriority = 1,
                        OlderTvPriority = 1
                    }.ToJson(),
                    ConfigContract = "QBittorrentSettings"
                });
            });

            var items = db.Query<DownloadClientDefinition67>("SELECT * FROM \"DownloadClients\"");

            items.Should().HaveCount(1);

            items.First().Settings.Should().NotContainKey("tvDirectory");
            items.First().Settings.Should().ContainKey("musicDirectory");
            items.First().Settings["musicDirectory"].Should().Be("abc");

            items.First().Settings.Should().NotContainKey("recentTvPriority");
            items.First().Settings.Should().ContainKey("recentMusicPriority");
            items.First().Settings["recentMusicPriority"].Should().Be(1);

            items.First().Settings.Should().NotContainKey("olderTvPriority");
            items.First().Settings.Should().ContainKey("olderMusicPriority");
            items.First().Settings["olderMusicPriority"].Should().Be(1);
        }
    }

    public class DownloadClientDefinition67
    {
        public int Id { get; set; }
        public bool Enable { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string Implementation { get; set; }
        public JsonObject Settings { get; set; }
        public string ConfigContract { get; set; }
        public bool RemoveCompletedDownloads { get; set; }
        public bool RemoveFailedDownloads { get; set; }
    }
}
