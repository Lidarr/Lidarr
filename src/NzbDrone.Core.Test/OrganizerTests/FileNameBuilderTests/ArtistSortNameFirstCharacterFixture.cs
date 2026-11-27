using System.IO;
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
    public class ArtistSortNameFirstCharacterFixture : CoreTest<FileNameBuilder>
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

        [TestCase("Beatles, The", "B", "Beatles, The")]
        [TestCase("1975, The", "1", "1975, The")]
        [TestCase("Smith, Elliott", "S", "Smith, Elliott")]
        [TestCase("Madonna", "M", "Madonna")]
        [TestCase("Perfect Circle, A", "P", "Perfect Circle, A")]
        [TestCase("Rolling Stones, The", "R", "Rolling Stones, The")]
        [TestCase("50 Cent", "5", "50 Cent")]
        [TestCase("¡Forward, Russia!", "F", "¡Forward, Russia!")]
        [TestCase(".hack//SIGN", "H", "hack++SIGN")]
        public void should_get_expected_folder_name_back(string sortName, string parent, string child)
        {
            _artist.SortName = sortName;
            _namingConfig.ArtistFolderFormat = "{Artist SortNameFirstCharacter}\\{Artist SortName}";

            Subject.GetArtistFolder(_artist).Should().Be(Path.Combine(parent, child));
        }

        [Test]
        public void should_be_able_to_use_lower_case_first_character()
        {
            _artist.SortName = "Beatles, The";
            _namingConfig.ArtistFolderFormat = "{artist sortnamefirstcharacter}\\{artist sortname}";

            Subject.GetArtistFolder(_artist).Should().Be(Path.Combine("b", "beatles, the"));
        }
    }
}
