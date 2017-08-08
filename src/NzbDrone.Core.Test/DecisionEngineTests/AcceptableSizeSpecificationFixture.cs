using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class AcceptableSizeSpecificationFixture : CoreTest<AcceptableSizeSpecification>
    {
        private RemoteAlbum parseResultMultiSet;
        private RemoteAlbum parseResultMulti;
        private RemoteAlbum parseResultSingle;
        private Artist artist;
        private QualityDefinition qualityType;

        [SetUp]
        public void Setup()
        {
            artist = Builder<Artist>.CreateNew()
                .Build();

            parseResultMultiSet = new RemoteAlbum
            {
                                        Artist = artist,
                                        Release = new ReleaseInfo(),
                                        ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_192, new Revision(version: 2)) },
                                        Albums = new List<Album> { new Album(), new Album(), new Album(), new Album(), new Album(), new Album() }
                                    };

            parseResultMulti = new RemoteAlbum
            {
                                        Artist = artist,
                                        Release = new ReleaseInfo(),
                                        ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_192, new Revision(version: 2)) },
                                        Albums = new List<Album> { new Album(), new Album() }
                                    };

            parseResultSingle = new RemoteAlbum
            {
                                        Artist = artist,
                                        Release = new ReleaseInfo(),
                                        ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_192, new Revision(version: 2)) },
                                        Albums = new List<Album> { new Album { Id = 2 } }

                                    };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            qualityType = Builder<QualityDefinition>.CreateNew()
                .With(q => q.MinSize = 2)
                .With(q => q.MaxSize = 10)
                .With(q => q.Quality = Quality.MP3_192)
                .Build();

            Mocker.GetMock<IQualityDefinitionService>().Setup(s => s.Get(Quality.MP3_192)).Returns(qualityType);

            Mocker.GetMock<IAlbumService>().Setup(
                s => s.GetAlbumsByArtist(It.IsAny<int>()))
                .Returns(new List<Album>() {
                    new Album(), new Album(), new Album(), new Album(), new Album(),
                    new Album(), new Album(), new Album(), new Album { Id = 2 }, new Album() });
        }

        private void GivenLastAlbum()
        {
            Mocker.GetMock<IAlbumService>().Setup(
                s => s.GetAlbumsByArtist(It.IsAny<int>()))
                .Returns(new List<Album> {
                    new Album(), new Album(), new Album(), new Album(), new Album(),
                    new Album(), new Album(), new Album(), new Album(), new Album { Id = 2 } });
        }

        // TODO Add Album duration to Test methods

        [TestCase(30, 50, false)]
        [TestCase(30, 250, true)]
        [TestCase(30, 500, false)]
        [TestCase(60, 100, false)]
        [TestCase(60, 500, true)]
        [TestCase(60, 1000, false)]
        public void single_album(int runtime, int sizeInMegaBytes, bool expectedResult)
        {           
            parseResultSingle.Artist = artist;
            parseResultSingle.Release.Size = sizeInMegaBytes.Megabytes();

            Subject.IsSatisfiedBy(parseResultSingle, null).Accepted.Should().Be(expectedResult);
        }

        [TestCase(30, 500, true)]
        [TestCase(30, 1000, false)]
        [TestCase(60, 1000, true)]
        [TestCase(60, 2000, false)]
        public void single_album_first_or_last(int runtime, int sizeInMegaBytes, bool expectedResult)
        {
            GivenLastAlbum();

            parseResultSingle.Artist = artist;
            parseResultSingle.Release.Size = sizeInMegaBytes.Megabytes();

            Subject.IsSatisfiedBy(parseResultSingle, null).Accepted.Should().Be(expectedResult);
        }

        [TestCase(30, 50 * 2, false)]
        [TestCase(30, 250 * 2, true)]
        [TestCase(30, 500 * 2, false)]
        [TestCase(60, 100 * 2, false)]
        [TestCase(60, 500 * 2, true)]
        [TestCase(60, 1000 * 2, false)]
        public void multi_album(int runtime, int sizeInMegaBytes, bool expectedResult)
        {
            parseResultMulti.Artist = artist;
            parseResultMulti.Release.Size = sizeInMegaBytes.Megabytes();

            Subject.IsSatisfiedBy(parseResultMulti, null).Accepted.Should().Be(expectedResult);
        }

        [TestCase(30, 50 * 6, false)]
        [TestCase(30, 250 * 6, true)]
        [TestCase(30, 500 * 6, false)]
        [TestCase(60, 100 * 6, false)]
        [TestCase(60, 500 * 6, true)]
        [TestCase(60, 1000 * 6, false)]
        public void multiset_album(int runtime, int sizeInMegaBytes, bool expectedResult)
        {
            parseResultMultiSet.Artist = artist;
            parseResultMultiSet.Release.Size = sizeInMegaBytes.Megabytes();

            Subject.IsSatisfiedBy(parseResultMultiSet, null).Accepted.Should().Be(expectedResult);
        }

        [Test]
        public void should_return_true_if_size_is_zero()
        {
            GivenLastAlbum();

            parseResultSingle.Artist = artist;
            parseResultSingle.Release.Size = 0;
            qualityType.MinSize = 10;
            qualityType.MaxSize = 20;

            Subject.IsSatisfiedBy(parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_unlimited_30_minute()
        {
            GivenLastAlbum();

            parseResultSingle.Artist = artist;
            parseResultSingle.Release.Size = 18457280000;
            qualityType.MaxSize = null;

            Subject.IsSatisfiedBy(parseResultSingle, null).Accepted.Should().BeTrue();
        }
        
        [Test]
        public void should_return_true_if_unlimited_60_minute()
        {
            GivenLastAlbum();

            parseResultSingle.Artist = artist;
            parseResultSingle.Release.Size = 36857280000;
            qualityType.MaxSize = null;

            Subject.IsSatisfiedBy(parseResultSingle, null).Accepted.Should().BeTrue();
        }

    }
}
