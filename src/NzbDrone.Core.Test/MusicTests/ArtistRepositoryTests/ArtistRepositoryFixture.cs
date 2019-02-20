using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.Test.MusicTests.ArtistRepositoryTests
{
    [TestFixture]

    public class ArtistRepositoryFixture : DbTest<ArtistRepository, Artist>
    {
        private ArtistRepository _artistRepo;
        private ArtistMetadataRepository _artistMetadataRepo;

        private void AddArtist(string name)
        {
            var metadata = Builder<ArtistMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .BuildNew();
            
            var artist = Builder<Artist>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanArtistName(name))
                .With(a => a.ForeignArtistId = name)
                .BuildNew();

            _artistMetadataRepo.Insert(artist);
            _artistRepo.Insert(artist);
        }

        private void GivenArtists()
        {
            _artistRepo = Mocker.Resolve<ArtistRepository>();
            _artistMetadataRepo = Mocker.Resolve<ArtistMetadataRepository>();
            AddArtist("The Black Eyed Peas");
            AddArtist("The Black Keys");
        }

        [Test]
        public void should_lazyload_profiles()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.FLAC, Quality.MP3_192, Quality.MP3_320),

                Cutoff = Quality.FLAC.Id,
                Name = "TestProfile"
            };

            var langProfile = new LanguageProfile
            {
                Name = "TestProfile",
                Languages = Languages.LanguageFixture.GetDefaultLanguages(Language.English),
                Cutoff = Language.English
            };

            var metaProfile = new MetadataProfile
            {
                Name = "TestProfile",
                PrimaryAlbumTypes = new List<ProfilePrimaryAlbumTypeItem>(),
                SecondaryAlbumTypes = new List<ProfileSecondaryAlbumTypeItem>(),
                ReleaseStatuses = new List<ProfileReleaseStatusItem>()
            };


            Mocker.Resolve<QualityProfileRepository>().Insert(profile);
            Mocker.Resolve<LanguageProfileRepository>().Insert(langProfile);
            Mocker.Resolve<MetadataProfileRepository>().Insert(metaProfile);

            var artist = Builder<Artist>.CreateNew().BuildNew();
            artist.QualityProfileId = profile.Id;
            artist.LanguageProfileId = langProfile.Id;
            artist.MetadataProfileId = metaProfile.Id;

            Subject.Insert(artist);


            StoredModel.QualityProfile.Should().NotBeNull();
            StoredModel.LanguageProfile.Should().NotBeNull();
            StoredModel.MetadataProfile.Should().NotBeNull();

        }

        [TestCase("The Black Eyed Peas")]
        [TestCase("The Black Keys")]
        public void should_find_artist_in_db_by_name(string name)
        {
            GivenArtists();
            var artist = _artistRepo.FindByName(Parser.Parser.CleanArtistName(name));

            artist.Should().NotBeNull();
            artist.Name.Should().Be(name);
        }
    }
}
