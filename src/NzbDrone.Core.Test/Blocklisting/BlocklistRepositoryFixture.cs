using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Blocklisting
{
    [TestFixture]
    public class BlocklistRepositoryFixture : DbTest<BlocklistRepository, Blocklist>
    {
        private Blocklist _blocklist;

        [SetUp]
        public void Setup()
        {
            _blocklist = new Blocklist
            {
                ArtistId = 12345,
                AlbumIds = new List<int> { 1 },
                Quality = new QualityModel(Quality.FLAC),
                SourceTitle = "artist.name.album.title",
                Date = DateTime.UtcNow
            };
        }

        [Test]
        public void should_be_able_to_write_to_database()
        {
            Subject.Insert(_blocklist);
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_should_have_album_ids()
        {
            Subject.Insert(_blocklist);

            Subject.All().First().AlbumIds.Should().Contain(_blocklist.AlbumIds);
        }

        [Test]
        public void should_check_for_blocklisted_title_case_insensative()
        {
            Subject.Insert(_blocklist);

            Subject.BlocklistedByTitle(_blocklist.ArtistId, _blocklist.SourceTitle.ToUpperInvariant()).Should().HaveCount(1);
        }
    }
}
