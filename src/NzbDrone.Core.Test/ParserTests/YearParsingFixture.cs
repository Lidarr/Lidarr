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

            (result == null || result.ReleaseYear == expectedYear).Should().BeTrue();
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

        [TestCase("Artist - Woodstock 1969 FLAC", "Woodstock", 1969, "Woodstock 1969")]
        [TestCase("Artist - Live-1975 FLAC", "Live", 1975, "Live-1975")]
        [TestCase("Artist - Live1975 FLAC", "Live", 1975, "Live1975")]
        [TestCase("Artist - Live * 1975 FLAC", "Live", 1975, "Live 1975")]
        [TestCase("Artist - Live: 1975 FLAC", "Live", 1975, "Live: 1975")]
        [TestCase("Artist - Live, 1975 FLAC", "Live", 1975, "Live, 1975")]
        public void should_keep_album_title_with_year_when_year_directly_follows_album(string title, string album, int year, string albumWithYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.AlbumTitle.Should().Be(album);
            result.ReleaseYear.Should().Be(year);
            result.AlbumTitleWithYear.Should().Be(albumWithYear);
        }

        [TestCase("Artist - Live (1975) FLAC")]
        [TestCase("Artist - Live - 1975 [FLAC]")]
        [TestCase("Artist - Live (1975) [Recorded Live 1975] FLAC")]
        [TestCase("Live 1975 - Live (1975) FLAC")]
        [TestCase("Artist - Live [FLAC]")]
        public void should_not_keep_album_title_with_year_when_year_is_separate(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.AlbumTitleWithYear.Should().BeNull();
        }

        [TestCase("Artist - Album (Label CAT1999) (2020) FLAC", 2020)]
        [TestCase("Artist-Album-WEB-1920kbps-2020-GROUP", 2020)]
        [TestCase("Artist - Album (2016) [FLAC 1411kbps]", 2016)]
        public void should_not_take_catalog_numbers_or_bitrates_as_year(string title, int expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
        }

        [TestCase("Artist - Album (2011 Remaster) [FLAC]", 2011)]
        [TestCase("Artist - Album (Remastered 2011) FLAC", 2011)]
        [TestCase("Artist - Album (Japan Edition SICP-1985) FLAC", 1985)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (Remastered 2011-09-26) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (Japan (Deluxe) Remastered 2011) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (2011 (Deluxe)) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (Remastered (2011)) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (2011-09-26 Remastered) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (Remastered 09-26-2011) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (Remastered 2011_09_26) FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon (2011 Remaster FLAC", 2011)]
        [TestCase("Pink Floyd - The Dark Side of the Moon ((2011) Remastered) FLAC", 2011)]
        public void should_have_medium_confidence_for_year_inside_descriptive_brackets(string title, int expectedYear)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(expectedYear);
            result.YearConfidence.Should().Be(YearMatchConfidence.Medium);
        }

        [TestCase("Artist - Album (2020) FLAC")]
        [TestCase("Artist - Album (Deluxe) (2020) FLAC")]
        [TestCase("Artist - Album ( 2020 ) FLAC")]
        [TestCase("Artist - Album [Deluxe] (2020) FLAC")]
        [TestCase("Artist - Album (2020-06-15) FLAC")]
        [TestCase("Artist - Album (2020_06_15) FLAC")]
        [TestCase("Artist - Album (06-15-2020) FLAC")]
        [TestCase("Artist - Album 2020 FLAC")]
        [TestCase("Artist-Album-WEB-2020-GROUP")]
        public void should_have_high_confidence_for_standalone_year(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(2020);
            result.YearConfidence.Should().Be(YearMatchConfidence.High);
        }

        [Test]
        public void should_have_medium_confidence_for_next_year()
        {
            var nextYear = System.DateTime.UtcNow.Year + 1;

            var result = Parser.Parser.ParseAlbumTitle($"Artist - Album ({nextYear}) FLAC");

            result.Should().NotBeNull();
            result.ReleaseYear.Should().Be(nextYear);
            result.YearConfidence.Should().Be(YearMatchConfidence.Medium);
        }

        [TestCase("Artist - Album 2020123 FLAC")]
        [TestCase("Artist - Album (2020123) FLAC")]
        [TestCase("Artist-Album-2020123-GROUP")]
        [TestCase("Artist - Album 1232020 FLAC")]
        [TestCase("Artist - Album (1232020) FLAC")]
        [TestCase("Artist-Album-1232020-GROUP")]
        public void should_not_take_year_from_end_of_longer_number(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            (result == null || (result.ReleaseYear != 2020 && !result.AlbumTitle.EndsWith("123"))).Should().BeTrue();
        }

        [TestCase("Artist - Album [2020] FLAC", "Album")]
        [TestCase("Artist-Album [2020] FLAC", "Album")]
        [TestCase("Artist - Album (Deluxe) [2020] FLAC", "Album")]
        [TestCase("Artist - Album [FLAC] [2020]", "Album")]
        [TestCase("Artist - Album [2020.06.15] FLAC", "Album")]
        public void should_parse_year_in_square_brackets(string title, string expectedAlbum)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.AlbumTitle.Should().Be(expectedAlbum);
            result.ReleaseYear.Should().Be(2020);
            result.YearConfidence.Should().Be(YearMatchConfidence.High);
        }

        [TestCase("Artist - Album [2011 Remaster] FLAC")]
        [TestCase("Artist - Album [Remastered 2011] FLAC")]
        [TestCase("Artist - Album [FLAC] 1985 MB")]
        public void should_not_take_high_confidence_year_from_descriptive_square_brackets_or_sizes(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            (result.ReleaseYear == null || result.YearConfidence != YearMatchConfidence.High).Should().BeTrue();
        }

        [TestCase("Artist - Album - 1973 [FLAC] [2020 Remaster]")]
        [TestCase("Artist - Album - 1973 [FLAC] [2020]")]
        public void should_keep_dash_separated_year_before_later_square_brackets(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.AlbumTitle.Should().Be("Album");
            result.ReleaseYear.Should().Be(1973);
        }

        [TestCase("Artist - Album [FLAC] WEB [2020]")]
        [TestCase("Artist - Album [FLAC] - WEB [2020]")]
        public void should_not_add_metadata_to_album_title_when_parsing_square_bracket_year(string title)
        {
            var result = Parser.Parser.ParseAlbumTitle(title);

            result.Should().NotBeNull();
            result.ArtistName.Should().Be("Artist");
            result.AlbumTitle.Should().Be("Album");
            result.ReleaseYear.Should().BeNull();
        }

        [Test]
        public void should_parse_many_empty_brackets_quickly()
        {
            var title = "Artist - Album " + string.Concat(System.Linq.Enumerable.Repeat("[]", 40)) + " FLAC";

            var parseTask = System.Threading.Tasks.Task.Run(() => Parser.Parser.ParseAlbumTitle(title));

            parseTask.Wait(System.TimeSpan.FromSeconds(5)).Should().BeTrue();
            (parseTask.Result == null || parseTask.Result.ReleaseYear == null).Should().BeTrue();
        }
    }
}
