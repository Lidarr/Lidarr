using System.IO;
using System.Linq;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TrackImport.Identification;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.SkyHook;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.TrackImport.Identification
{
    [TestFixture]
    public class IdentificationServiceFixture : DbTest
    {
        private ArtistService _artistService;
        private AddArtistService _addArtistService;
        private RefreshArtistService _refreshArtistService;

        private IdentificationService Subject;
        
        [SetUp]
        public void SetUp()
        {
            UseRealHttp();
            
            // Resolve all the parts we need
            Mocker.SetConstant<IArtistRepository>(Mocker.Resolve<ArtistRepository>());
            Mocker.SetConstant<IArtistMetadataRepository>(Mocker.Resolve<ArtistMetadataRepository>());
            Mocker.SetConstant<IAlbumRepository>(Mocker.Resolve<AlbumRepository>());
            Mocker.SetConstant<IReleaseRepository>(Mocker.Resolve<ReleaseRepository>());
            Mocker.SetConstant<ITrackRepository>(Mocker.Resolve<TrackRepository>());

            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Exists(It.IsAny<int>())).Returns(true);

            _artistService = Mocker.Resolve<ArtistService>();
            Mocker.SetConstant<IArtistService>(_artistService);
            Mocker.SetConstant<IAlbumService>(Mocker.Resolve<AlbumService>());
            Mocker.SetConstant<IReleaseService>(Mocker.Resolve<ReleaseService>());
            Mocker.SetConstant<ITrackService>(Mocker.Resolve<TrackService>());

            Mocker.SetConstant<IConfigService>(Mocker.Resolve<IConfigService>());
            Mocker.SetConstant<IProvideArtistInfo>(Mocker.Resolve<SkyHookProxy>());
            Mocker.SetConstant<IProvideAlbumInfo>(Mocker.Resolve<SkyHookProxy>());
            
            _addArtistService = Mocker.Resolve<AddArtistService>();

            Mocker.SetConstant<IRefreshTrackService>(Mocker.Resolve<RefreshTrackService>());
            Mocker.SetConstant<IAddAlbumService>(Mocker.Resolve<AddAlbumService>());
            _refreshArtistService = Mocker.Resolve<RefreshArtistService>();

            Mocker.GetMock<IAddArtistValidator>().Setup(x => x.Validate(It.IsAny<Artist>())).Returns(new ValidationResult());

            Mocker.SetConstant<ITrackGroupingService>(Mocker.Resolve<TrackGroupingService>());
            // Mocker.SetConstant<IAugmentingService>(Mocker.Resolve<AugmentingService>());
            Subject = Mocker.Resolve<IdentificationService>();

        }

        private void GivenMetadataProfile(MetadataProfile profile)
        {
            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Get(It.IsAny<int>())).Returns(profile);            
        }

        private Artist GivenArtist(string foreignArtistId)
        {
            var artist = _addArtistService.AddArtist(new Artist {
                    Metadata = new ArtistMetadata {
                        ForeignArtistId = foreignArtistId
                    },
                    Path = @"c:\test".AsOsAgnostic(),
                    MetadataProfileId = 1
                });

            var command = new RefreshArtistCommand{
                ArtistId = artist.Id,
                Trigger = CommandTrigger.Unspecified
            };

            _refreshArtistService.Execute(command);

            return _artistService.FindById(foreignArtistId);
        }

        // these are slow to run so only do so manually
        [Explicit]
        [TestCase("FilesWithMBIds.json")]
        [TestCase("PreferMissingToBadMatch.json")]
        [TestCase("InconsistentTyposInAlbum.json")]
        [TestCase("SucceedWhenManyAlbumsHaveSameTitle.json")]
        public void should_match_tracks(string file)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Identification", file);
            var testcase = JsonConvert.DeserializeObject<IdTestCase>(File.ReadAllText(path));

            GivenMetadataProfile(testcase.MetadataProfile);
            
            var artist = GivenArtist(testcase.Artist);

            var tracks = testcase.Tracks.Select(x => new LocalTrack {
                    Path = x.Path.AsOsAgnostic(),
                    FileTrackInfo = x.FileTrackInfo
                }).ToList();

            var result = Subject.Identify(tracks, artist, null, null, testcase.NewDownload, testcase.SingleRelease);

            result.Should().HaveCount(testcase.ExpectedMusicBrainzReleaseIds.Count);
            result.Select(x => x.AlbumRelease.ForeignReleaseId).ShouldBeEquivalentTo(testcase.ExpectedMusicBrainzReleaseIds);
        }
    }
}
