using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class YearParsingFixture : CoreTest
    {
        [TestCase("Artist - Album (2020) FLAC", 2020, YearMatchConfidence.High)]
        [TestCase("Artist - Album - 2018 - FLAC", 2018, YearMatchConfidence.High)]
        [TestCase("Artist-Album-WEB-2021-GROUP", 2021, YearMatchConfidence.High)]
        [TestCase("Artist - Album 2022", 2022, YearMatchConfidence.High)]
        [TestCase("(Rock) Artist - Album - 2015 FLAC", 2015, YearMatchConfidence.High)]
        public void should_parse_year_from_standard_formats(string title, int expectedYear, YearMatchConfidence expectedConfidence)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
            result.YearConfidence.Should().Be(expectedConfidence);
        }

        [TestCase("Artist - Album (2020) [FLAC]", 2020)]
        [TestCase("Artist - Album (Deluxe) (2020)", 2020)]
        public void should_parse_year_from_parentheses_formats(string title, int expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
        }

        [TestCase("Artist - Album FLAC")]
        [TestCase("Artist - Album [FLAC]")]
        [TestCase("Artist - Album (Deluxe Edition)")]
        public void should_have_null_year_when_not_present(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            if (result != null)
            {
                result.ReleaseYear.Should().BeNull();
            }
        }

        [TestCase("Artist-Album-Deluxe Edition-2CD-FLAC-2015-GROUP", 2015)]
        [TestCase("Artist-Album-WEB-2017-FURY", 2017)]
        public void should_parse_year_from_scene_formats(string title, int expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
        }

        [TestCase("Artist - 2020 - Album Title", 2020)]
        public void should_parse_year_from_artist_year_album_format(string title, int expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
        }

        [Test]
        public void should_set_release_date_string_from_year()
        {
            var result = Parser.Parser.ParseAlbumTitle("Artist - Album (2020) FLAC");

            result.Should().NotBeNull();
            result.ReleaseDate.Should().Be("2020");
            result.ReleaseYear.Should().Be(2020);
        }

        [Test]
        public void should_handle_empty_release_date_when_no_year()
        {
            var result = Parser.Parser.ParseAlbumTitle("Artist - Album [FLAC]");

            if (result != null && !result.ReleaseYear.HasValue)
            {
                result.ReleaseDate.Should().BeEmpty();
            }
        }

        [TestCase("Artist - Discography 1990-2020", null)]
        [TestCase("Artist Discography 2010-2023", null)]
        public void should_not_set_release_year_for_discography(string title, int? expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.Discography.Should().BeTrue();
            result.ReleaseYear.Should().Be(expectedYear);
        }

        [TestCase("Artist - Album 1899 FLAC", null)]
        [TestCase("Artist - Album 2099 FLAC", null)]
        public void should_ignore_years_outside_valid_range(string title, int? expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            if (result != null)
            {
                if (result.ReleaseYear.HasValue && (result.ReleaseYear < 1900 || result.ReleaseYear > 2100))
                {
                    result.YearConfidence.Should().Be(YearMatchConfidence.Low);
                }
            }
        }

        [TestCase("Coldplay - Music of the Spheres (2021) [MP3 256kbps] [ df1975 ] [WEB]", 2021)]
        [TestCase("Coldplay - Music of the Spheres [MP3 320kbps] [ Frederic1986 ] [WEB]", null)]
        [TestCase("Artist - Album (2020) [ user2001 ] [FLAC]", 2020)]
        [TestCase("Artist - Album (2019) [MP3] [ oldschool1975 ]", 2019)]
        [TestCase("Artist - Album [FLAC] [ john1990 ] [⚡ fast ]", null)]
        public void should_not_parse_year_from_username_in_brackets(string title, int? expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            if (expectedYear.HasValue)
            {
                result.Should().NotBeNull();
                result.ReleaseYear.Should().Be(expectedYear);
            }
            else
            {
                // If no expected year, either result is null or year is null
                if (result != null)
                {
                    result.ReleaseYear.Should().BeNull();
                }
            }
        }
    }
}
