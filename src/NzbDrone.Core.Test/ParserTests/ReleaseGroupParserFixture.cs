using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseGroupParserFixture : CoreTest
    {
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-ENTiTLED", "ENTiTLED")]
        [TestCase("[ www.Torrenting.com ] - Olafur.Arnalds-Remember-WEB-2018-ENTiTLED", "ENTiTLED")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-ENTiTLED [eztv]-[rarbg.com]", "ENTiTLED")]
        [TestCase("7s-atlantis-128.mp3", null)]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-ENTiTLED-Pre", "ENTiTLED")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-ENTiTLED-postbot", "ENTiTLED")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-ENTiTLED-xpost", "ENTiTLED")]
        [TestCase("[TR24][OF] Good Charlotte - Generation Rx - 2018", null)]
        [TestCase("The.Good.Series.S05E03.Series.of.Intelligence.1080p.10bit.AMZN.WEB-DL.DDP5.1.HEVC-Vyndros", "Vyndros")]
        [TestCase("Artist.Title-Album.Title.1080p.DSNP.WEB-DL.DDP2.0.H.264-VARYG", "VARYG")]
        [TestCase("Artist Title - Album Title (Showtime) (1080p.BD.DD5.1.x265-TheSickle[TAoE])", "TheSickle")]

        // [TestCase("", "")]
        public void should_parse_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Show.Name.2009.S01.1080p.BluRay.DTS5.1.x264-D-Z0N3", "D-Z0N3")]
        [TestCase("Show.Name.S01E01.1080p.WEB-DL.H264.Fight-BB.mkv", "Fight-BB")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 5.1 Tigole) [QxR]", "Tigole")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 2.0 afm72) [QxR]", "afm72")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p DSNP WEB-DL x265 HEVC 10bit EAC3 5.1 Silence) [QxR]", "Silence")]
        [TestCase("Show Name (2021) Season 1 S01 (1080p BluRay x265 HEVC 10bit AAC 2.0 Panda) [QxR]", "Panda")]
        [TestCase("Show Name (2020) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 2.0 Ghost) [QxR]", "Ghost")]
        [TestCase("Show Name (2020) Season 1 S01 (1080p WEB-DL x265 HEVC 10bit AC3 5.1 MONOLITH) [QxR]", "MONOLITH")]
        [TestCase("The Show S08E09 The Series.1080p.AMZN.WEB-DL.x265.10bit.EAC3.6.0-Qman[UTR]", "UTR")]
        [TestCase("The Show S03E07 Fire and Series[1080p x265 10bit S87 Joy]", "Joy")]
        [TestCase("The Show (2016) - S02E01 - Soul Series #1 (1080p NF WEBRip x265 ImE)", "ImE")]
        [TestCase("The Show (2020) - S02E03 - Fighting His Series(1080p ATVP WEB-DL x265 t3nzin)", "t3nzin")]
        [TestCase("[Anime Time] A Show [BD][Dual Audio][1080p][HEVC 10bit x265][AAC][Eng Sub] [Batch] Title)", "Anime Time")]
        [TestCase("[Project Angel] Anime Series [DVD 480p] [10-bit x265 HEVC | Opus]", "Project Angel")]
        [TestCase("[Hakata Ramen] Show Title - Season 2 - Revival of The Commandments", "Hakata Ramen")]
        [TestCase("Show Name (2022) S01 (2160p DSNP WEB-DL H265 DV HDR DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Show Title (2021) S01 (2160p ATVP WEB-DL Hybrid H265 DV HDR10+ DDP Atmos 5.1 English - HONE)", "HONE")]
        [TestCase("Series.Title.S01E09.1080p.DSNP.WEB-DL.DDP2.0.H.264-VARYG (Blue Lock, Multi-Subs)", "VARYG")]
        [TestCase("Series.Title (2014) S09E10 (1080p AMZN WEB-DL x265 HEVC 10bit DDP 5.1 Vyndros)", "Vyndros")]
        [TestCase("Series Title S02E03 Title 4k to 1080p DSNP WEBrip x265 DDP 5 1 Releaser[SEV]", "SEV")]
        [TestCase("Series Title Season 01 S01 1080p AMZN UHD WebRip x265 DDP 5.1 Atmos Releaser-SEV", "SEV")]
        [TestCase("Series Title - S01.E06 - Title 1080p AMZN WebRip x265 DDP 5.1 Atmos Releaser [SEV]", "SEV")]
        [TestCase("Grey's Anatomy (2005) - S01E01 - A Hard Day's Night (1080p DSNP WEB-DL x265 Garshasp).mkv", "Garshasp")]
        [TestCase("Marvel's Agent Carter (2015) - S02E04 - Smoke & Mirrors (1080p BluRay x265 Kappa).mkv", "Kappa")]
        [TestCase("Snowpiercer (2020) - S02E03 - A Great Odyssey (1080p BluRay x265 Kappa).mkv", "Kappa")]
        [TestCase("Enaaya (2019) - S01E01 - Episode 1 (1080p WEB-DL x265 Natty).mkv", "Natty")]
        [TestCase("SpongeBob SquarePants (1999) - S03E01-E02 - Mermaid Man and Barnacle Boy IV & Doing Time (1080p AMZN WEB-DL x265 RCVR).mkv", "RCVR")]
        [TestCase("Invincible (2021) - S01E02 - Here Goes Nothing (1080p WEB-DL x265 SAMPA).mkv", "SAMPA")]
        [TestCase("The Bad Batch (2021) - S01E01 - Aftermath (1080p DSNP WEB-DL x265 YOGI).mkv", "YOGI")]
        [TestCase("Line of Duty (2012) - S01E01 - Episode 1 (1080p BluRay x265 r00t).mkv", "r00t")]
        [TestCase("Rich & Shameless - S01E01 - Girls Gone Wild Exposed (720p x265 EDGE2020).mkv", "EDGE2020")]
        [TestCase("Show Name (2016) Season 1 S01 (1080p AMZN WEB-DL x265 HEVC 10bit EAC3 5 1 RZeroX) QxR", "RZeroX")]
        public void should_parse_exception_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [Test]
        [Ignore("Track name parsing needs to be worked on")]
        public void should_not_include_extension_in_release_group()
        {
            const string path = @"C:\Test\Doctor.Who.2005.s01e01.internal.bdrip.x264-archivist.mkv";

            Parser.Parser.ParseMusicPath(path).ReleaseGroup.Should().Be("archivist");
        }

        [TestCase("Olafur.Arnalds-Remember-WEB-2018-SKGTV English", "SKGTV")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-SKGTV_English", "SKGTV")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-SKGTV.English", "SKGTV")]

        // [TestCase("", "")]
        public void should_not_include_language_in_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("Olafur.Arnalds-Remember-WEB-2018-EVL-RP", "EVL")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-EVL-RP-RP", "EVL")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-EVL-Obfuscated", "EVL")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-xHD-NZBgeek", "xHD")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-DIMENSION-NZBgeek", "DIMENSION")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-xHD-1", "xHD")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-DIMENSION-1", "DIMENSION")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-EVL-Scrambled", "EVL")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-EVL-AlteZachen", "EVL")]
        [TestCase("Olafur.Arnalds-Remember-WEB-2018-HarrHD-RePACKPOST", "HarrHD")]
        public void should_not_include_repost_in_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("[FFF] Invaders of the Rokujouma!! - S01E11 - Someday, With Them", "FFF")]
        [TestCase("[HorribleSubs] Invaders of the Rokujouma!! - S01E12 - Invasion Going Well!!", "HorribleSubs")]
        [TestCase("[Anime-Koi] Barakamon - S01E06 - Guys From Tokyo", "Anime-Koi")]
        [TestCase("[Anime-Koi] Barakamon - S01E07 - A High-Grade Fish", "Anime-Koi")]
        [TestCase("[Anime-Koi] Kami-sama Hajimemashita 2 - 01 [h264-720p][28D54E2C]", "Anime-Koi")]

        // [TestCase("Tokyo.Ghoul.02x01.013.HDTV-720p-Anime-Koi", "Anime-Koi")]
        // [TestCase("", "")]
        public void should_parse_anime_release_groups(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }
    }
}
