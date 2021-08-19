using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource
{
    [TestFixture]
    public class SearchArtistComparerFixture : CoreTest
    {
        private List<Artist> _artist;

        [SetUp]
        public void Setup()
        {
            _artist = new List<Artist>();
        }

        private void WithSeries(string name)
        {
            _artist.Add(new Artist { Name = name });
        }

        [Test]
        public void should_prefer_the_walking_dead_over_talking_dead_when_searching_for_the_walking_dead()
        {
            WithSeries("Talking Dead");
            WithSeries("The Walking Dead");

            _artist.Sort(new SearchArtistComparer("the walking dead"));

            _artist.First().Name.Should().Be("The Walking Dead");
        }

        [Test]
        public void should_prefer_the_walking_dead_over_talking_dead_when_searching_for_walking_dead()
        {
            WithSeries("Talking Dead");
            WithSeries("The Walking Dead");

            _artist.Sort(new SearchArtistComparer("walking dead"));

            _artist.First().Name.Should().Be("The Walking Dead");
        }

        [Test]
        public void should_prefer_blocklist_over_the_blocklist_when_searching_for_blocklist()
        {
            WithSeries("The Blocklist");
            WithSeries("Blocklist");

            _artist.Sort(new SearchArtistComparer("blocklist"));

            _artist.First().Name.Should().Be("Blocklist");
        }

        [Test]
        public void should_prefer_the_blocklist_over_blocklist_when_searching_for_the_blocklist()
        {
            WithSeries("Blocklist");
            WithSeries("The Blocklist");

            _artist.Sort(new SearchArtistComparer("the blocklist"));

            _artist.First().Name.Should().Be("The Blocklist");
        }
    }
}
