using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class ArtistCleanSortNameFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                    .CreateNew()
                    .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [TestCase("AC/DC", "AC DC")]
        [TestCase("Guns N' Roses", "Guns N Roses")]
        [TestCase("Twenty Øne Piløts", "Twenty One Pilots")]
        public void should_get_expected_folder_name_back(string sortName, string cleanSortName)
        {
            _artist.SortName = sortName;
            _namingConfig.ArtistFolderFormat = "{Artist CleanSortName}";

            Subject.GetArtistFolder(_artist).Should().Be(cleanSortName);
        }

        [Test]
        public void should_be_able_to_use_lower_case_clean_sort_name()
        {
            _artist.SortName = "AC/DC";
            _namingConfig.ArtistFolderFormat = "{artist cleansortname}";

            Subject.GetArtistFolder(_artist).Should().Be("ac dc");
        }
    }
}
