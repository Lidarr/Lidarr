using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class TruncatedArtistNameFixture : CoreTest<FileNameBuilder>
    {
        private Artist _artist;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                .CreateNew()
                .With(s => s.Name = "Artist Name")
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameTracks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [TestCase("{Artist Name:16}", "The Fantastic...")]
        [TestCase("{Artist NameThe:17}", "Fantastic Life...")]
        [TestCase("{Artist CleanName:-13}", "...Mr. Sisko")]
        public void should_truncate_artist_name(string format, string expected)
        {
            _artist.Name = "The Fantastic Life of Mr. Sisko";
            _namingConfig.ArtistFolderFormat = format;

            var result = Subject.GetArtistFolder(_artist, _namingConfig);
            result.Should().Be(expected);
        }
    }
}
