using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Lidarr.Api.V1.RootFolders;
using Lidarr.Api.V1.Tracks;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.TrackImport.Manual;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;
using NzbDrone.Integration.Test.Client;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class AlbumFixture : IntegrationTest
    {
        private ClientBase<RetagTrackResource> _retag;

        [SetUp]
        public void Setup()
        {
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = ArtistRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });

            _retag = new ClientBase<RetagTrackResource>(RestClient, ApiKey, "retag");
        }

        [Test]
        public void saving_album_should_keep_release_date_in_retag_preview()
        {
            var artist = EnsureArtist("8ac6cc32-8ddf-43b1-9ac4-4b04f9053176", "Alien Ant Farm");
            var album = Albums.GetAlbumsInArtist(artist.Id).Where(a => a.ReleaseDate.HasValue).OrderBy(a => a.ReleaseDate).First();
            var release = album.Releases.Single(r => r.Monitored);
            var track = Tracks.Get<List<TrackResource>>(Tracks.BuildRequest("?albumReleaseId=" + release.Id)).OrderBy(t => t.AbsoluteTrackNumber).First();

            // A real audio file, so the retag preview can read its tags
            var path = Path.Combine(artist.Path, "Track.mp3");
            File.Copy(Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", "nin.mp3"), path, true);

            Commands.PostAndWait(new ManualImportCommand
            {
                Files = new List<ManualImportFile>
                {
                    new ManualImportFile
                    {
                        Path = path,
                        ArtistId = artist.Id,
                        AlbumId = album.Id,
                        AlbumReleaseId = release.Id,
                        TrackIds = new List<int> { track.Id },
                        Quality = new QualityModel(Quality.MP3_320)
                    }
                }
            });
            Commands.WaitAll();

            var before = GetDateChange(artist.Id, album.Id);
            before.Should().NotBeNull();
            before.NewValue.Should().NotBeNullOrWhiteSpace();

            Albums.Put(Albums.Get(album.Id));

            var after = GetDateChange(artist.Id, album.Id);
            (after?.NewValue).Should().Be(before.NewValue);
        }

        private TagDifference GetDateChange(int artistId, int albumId)
        {
            var request = _retag.BuildRequest($"?artistId={artistId}&albumId={albumId}");

            return _retag.Get<List<RetagTrackResource>>(request).SelectMany(r => r.Changes).SingleOrDefault(c => c.Field == "Date");
        }
    }
}
