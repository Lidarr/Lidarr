using System.Linq;
using FluentAssertions;
using Lidarr.Api.V1.RootFolders;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Integration.Test.ApiTests.WantedTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2025-09-01 00:00:00Z")]
    public class CutoffUnmetFixture : IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            // Add a root folder
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = ArtistRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });
        }

        [Test]
        [Order(1)]
        public void cutoff_should_have_monitored_items()
        {
            EnsureQualityProfileCutoff(1, "Lossless", true);
            var artist = EnsureArtist("8ac6cc32-8ddf-43b1-9ac4-4b04f9053176", "Alien Ant Farm", true);
            EnsureTrackFile(artist, 1, 1, 1, Quality.MP3_192);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().NotBeEmpty();
        }

        [Test]
        [Order(1)]
        public void cutoff_should_not_have_unmonitored_items()
        {
            EnsureQualityProfileCutoff(1, "Lossless", true);
            var artist = EnsureArtist("8ac6cc32-8ddf-43b1-9ac4-4b04f9053176", "Alien Ant Farm", false);
            EnsureTrackFile(artist, 1, 1, 1, Quality.MP3_192);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(1)]
        public void cutoff_should_have_artist()
        {
            EnsureQualityProfileCutoff(1, "Lossless", true);
            var artist = EnsureArtist("8ac6cc32-8ddf-43b1-9ac4-4b04f9053176", "Alien Ant Farm", true);
            EnsureTrackFile(artist, 1, 1, 1, Quality.MP3_192);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.First().Artist.Should().NotBeNull();
            result.Records.First().Artist.ArtistName.Should().Be("Alien Ant Farm");
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_unmonitored_items()
        {
            EnsureQualityProfileCutoff(1, "Lossless", true);
            var artist = EnsureArtist("8ac6cc32-8ddf-43b1-9ac4-4b04f9053176", "Alien Ant Farm", false);
            EnsureTrackFile(artist, 1, 1, 1, Quality.MP3_192);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc", "monitored", false);

            result.Records.Should().NotBeEmpty();
        }
    }
}
