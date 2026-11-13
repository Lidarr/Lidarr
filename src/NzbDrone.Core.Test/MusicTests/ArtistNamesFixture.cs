using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class ArtistNamesFixture : CoreTest
    {
        private Artist _artist;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist
            {
                Metadata = new ArtistMetadata
                {
                    Name = "刘柏辛",
                    Aliases = new List<string> { "Lexie Liu", "刘柏辛" },
                    SortName = "Liu, Lexie"
                }
            };
        }

        [Test]
        public void should_include_sort_name_when_it_is_not_already_a_name_or_alias()
        {
            ArtistNames.Choices(_artist).Should().Equal("刘柏辛", "Lexie Liu", "Liu, Lexie");
        }

        [Test]
        public void should_omit_sort_name_when_it_repeats_an_existing_name()
        {
            _artist.Metadata.Value.SortName = "lexie liu";

            ArtistNames.Choices(_artist).Should().Equal("刘柏辛", "Lexie Liu");
        }

        [Test]
        public void should_clear_preferred_name_when_it_is_the_metadata_name()
        {
            ArtistNames.NormalizePreferredName(_artist, " 刘柏辛 ").Should().BeNull();
            ArtistNames.NormalizePreferredName(_artist, "Lexie Liu").Should().Be("Lexie Liu");
            ArtistNames.NormalizePreferredName(_artist, " ").Should().BeNull();
        }

        [Test]
        public void should_allow_a_saved_name_that_is_no_longer_in_the_metadata()
        {
            _artist.PreferredName = "Lexie";

            ArtistNames.IsAllowed(_artist, "Lexie").Should().BeTrue();
            ArtistNames.IsAllowed(_artist, "Someone Else").Should().BeFalse();
            ArtistNames.IsAllowed(_artist, "Liu, Lexie").Should().BeTrue();
        }

        [Test]
        public void should_use_preferred_name_for_library_and_track_metadata()
        {
            _artist.PreferredName = "Lexie Liu";

            ArtistNames.ForLibraryArtist(_artist).Should().Be("Lexie Liu");
            ArtistNames.ForMetadata(_artist.Metadata.Value, _artist).Should().Be("Lexie Liu");
        }

        [Test]
        public void should_fall_back_to_metadata_name_when_no_preferred_name_is_set()
        {
            ArtistNames.ForLibraryArtist(_artist).Should().Be("刘柏辛");
            ArtistNames.ForMetadata(_artist.Metadata.Value, null).Should().Be("刘柏辛");
        }
    }
}
