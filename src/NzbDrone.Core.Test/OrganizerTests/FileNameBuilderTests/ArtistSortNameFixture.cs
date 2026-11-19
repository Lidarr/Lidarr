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
    public class ArtistSortNameFixture : CoreTest<FileNameBuilder>
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

        [TestCase("The Beatles", "Beatles, The")]
        [TestCase("Elliott Smith", "Smith, Elliott")]
        [TestCase("Madonna", "Madonna")]
        [TestCase("A Perfect Circle", "Perfect Circle, A")]
        [TestCase("The Rolling Stones", "Rolling Stones, The")]
        public void should_get_expected_folder_name_back(string name, string sortName)
        {
            _artist.Name = name;
            _artist.SortName = sortName;
            _namingConfig.ArtistFolderFormat = "{Artist SortName}";

            Subject.GetArtistFolder(_artist).Should().Be(sortName);
        }

        [Test]
        public void should_be_able_to_use_lower_case_sort_name()
        {
            _artist.Name = "The Beatles";
            _artist.SortName = "Beatles, The";
            _namingConfig.ArtistFolderFormat = "{artist sortname}";

            Subject.GetArtistFolder(_artist).Should().Be("beatles, the");
        }
    }
}
