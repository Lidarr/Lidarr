using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class SingleAlbumSearchMatchSpecificationFixture : CoreTest<SingleAlbumSearchMatchSpecification>
    {
        private RemoteAlbum _remoteAlbum;
        private AlbumSearchCriteria _searchCriteria;

        [SetUp]
        public void Setup()
        {
            _remoteAlbum = new RemoteAlbum
            {
                Albums = new List<Album> { Builder<Album>.CreateNew().Build() },
                ParsedAlbumInfo = new ParsedAlbumInfo { AlbumTitle = "1989" }
            };

            _searchCriteria = new AlbumSearchCriteria { AlbumTitle = "1989" };
        }

        [Test]
        public void should_accept_when_album_title_matches()
        {
            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_no_album_title_matches()
        {
            _searchCriteria.AlbumTitle = "Red";

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_album_title_with_qualifier_matches()
        {
            _searchCriteria.AlbumTitle = "1989 (Taylor’s Version)";
            _remoteAlbum.ParsedAlbumInfo.AlbumTitleWithQualifier = "1989 (Taylor's Version)";

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_album_title_with_year_matches()
        {
            _searchCriteria.AlbumTitle = "Woodstock 1969";
            _remoteAlbum.ParsedAlbumInfo.AlbumTitle = "Woodstock";
            _remoteAlbum.ParsedAlbumInfo.AlbumTitleWithYear = "Woodstock 1969";

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_truncated_title_when_searching_qualified_album()
        {
            _searchCriteria.AlbumTitle = "1989 (Taylor’s Version)";

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeFalse();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void should_reject_without_throwing_when_searched_album_title_is_empty(string albumTitle)
        {
            _searchCriteria.AlbumTitle = albumTitle;

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_without_throwing_when_all_parsed_titles_are_null()
        {
            _remoteAlbum.ParsedAlbumInfo.AlbumTitle = null;

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_as_discography_when_only_alternate_parsed_title_matches_and_album_title_is_null()
        {
            _searchCriteria.AlbumTitle = "Woodstock 1969";
            _remoteAlbum.ParsedAlbumInfo.AlbumTitle = null;
            _remoteAlbum.ParsedAlbumInfo.AlbumTitleWithYear = "Woodstock 1969";

            Subject.IsSatisfiedBy(_remoteAlbum, _searchCriteria).Accepted.Should().BeFalse();
        }
    }
}
