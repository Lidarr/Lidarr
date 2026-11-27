using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class SingleAlbumSearchMatchSpecificationFixture : CoreTest<SingleAlbumSearchMatchSpecification>
    {
        private bool Accepted(string searchedTitle, string parsedTitle)
        {
            var remoteAlbum = new RemoteAlbum { ParsedAlbumInfo = new ParsedAlbumInfo { AlbumTitle = parsedTitle } };
            var searchCriteria = new AlbumSearchCriteria { AlbumTitle = searchedTitle };

            return Subject.IsSatisfiedBy(remoteAlbum, searchCriteria).Accepted;
        }

        [TestCase("Ganz oben", "Ganz Oben")]
        [TestCase("Ganz oben", "Ganz Oben (Mixtape)")]
        [TestCase("Ganz oben", "Ganz Oben Mixtape")]
        [TestCase("Sexismus gegen Rechts", "Sexismus Gegen Rechts (Digital Version)")]
        [TestCase("Billy Talent II", "Billy Talent II (International Version)")]
        [TestCase("Crackstreet Boys 3", "Crackstreet Boys 3 (Bonus Tracks Version)")]
        [TestCase("The Miracle", "The Miracle (Collector's Edition)")]
        [TestCase("Rumours", "Rumours (Remastered 2011)")]
        [TestCase("Rumours", "Rumours (2011 Remaster)")]
        [TestCase("Nevermind", "Nevermind (20th Anniversary Super Deluxe Edition)")]
        [TestCase("Volume 2", "Volume 2 (Deluxe)")]
        [TestCase("Ganz oben (Mixtape)", "Ganz Oben")]
        public void should_accept_the_searched_album_in_another_edition(string searchedTitle, string parsedTitle)
        {
            Accepted(searchedTitle, parsedTitle).Should().BeTrue();
        }

        [TestCase("Ganz oben", "Ganz Oben (Live)")]
        [TestCase("Ganz oben", "Ganz Oben (Instrumental Version)")]
        [TestCase("Ganz oben", "Ganz Oben (Remixes)")]
        [TestCase("Volume", "Volume 2 (Deluxe)")]
        [TestCase("Volume", "Volume 2")]
        [TestCase("Rumours", "Rumours 2011")]
        [TestCase("Ganz oben", "Urlaub fürs Gehirn")]
        public void should_reject_another_album(string searchedTitle, string parsedTitle)
        {
            Accepted(searchedTitle, parsedTitle).Should().BeFalse();
        }
    }
}
