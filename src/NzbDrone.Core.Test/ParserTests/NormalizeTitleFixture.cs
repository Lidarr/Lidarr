using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class NormalizeTitleFixture : CoreTest
    {
        [TestCase("Conan", "conan")]
        [TestCase("Castle (2009)", "castle2009")]
        [TestCase("Parenthood.2010", "parenthood2010")]
        [TestCase("Law_and_Order_SVU", "lawordersvu")]
        public void should_normalize_artist_title(string parsedArtistName, string artistName)
        {
            var result = parsedArtistName.CleanArtistName();
            result.Should().Be(artistName);
        }

        [TestCase("CaPitAl", "capital")]
        [TestCase("peri.od", "period")]
        [TestCase("this.^&%^**$%@#$!That", "thisthat")]
        [TestCase("test/test", "testtest")]
        [TestCase("90210", "90210")]
        [TestCase("24", "24")]
        public void should_remove_special_characters_and_casing(string dirty, string clean)
        {
            var result = dirty.CleanArtistName();
            result.Should().Be(clean);
        }

        [TestCase("the")]
        [TestCase("and")]
        [TestCase("or")]
        [TestCase("an")]
        [TestCase("of")]
        public void should_remove_common_words(string word)
        {
            var dirtyFormat = new[]
                            {
                                "word.{0}.word",
                                "word {0} word",
                                "word-{0}-word",
                                "word.word.{0}",
                                "word-word-{0}",
                                "word-word {0}",
                            };

            foreach (var s in dirtyFormat)
            {
                var dirty = string.Format(s, word);
                dirty.CleanArtistName().Should().Be("wordword");
            }
        }

        [Test]
        public void should_remove_a_from_middle_of_title()
        {
            var dirtyFormat = new[]
                            {
                                "word.{0}.word",
                                "word {0} word",
                                "word-{0}-word",
                            };

            foreach (var s in dirtyFormat)
            {
                var dirty = string.Format(s, "a");
                dirty.CleanArtistName().Should().Be("wordword");
            }
        }

        [TestCase("the")]
        [TestCase("and")]
        [TestCase("or")]
        [TestCase("a")]
        [TestCase("an")]
        [TestCase("of")]
        public void should_not_remove_common_words_in_the_middle_of_word(string word)
        {
            var dirtyFormat = new[]
                            {
                                "word.{0}word",
                                "word {0}word",
                                "word-{0}word",
                                "word{0}.word",
                                "word{0}-word",
                                "word{0}-word",
                            };

            foreach (var s in dirtyFormat)
            {
                var dirty = string.Format(s, word);
                dirty.CleanArtistName().Should().Be("word" + word.ToLower() + "word");
            }
        }

        [TestCase("The Office", "theoffice")]
        [TestCase("The Tonight Show With Jay Leno", "thetonightshowwithjayleno")]
        [TestCase("The.Daily.Show", "thedailyshow")]
        public void should_not_remove_from_the_beginning_of_the_title(string parsedArtistName, string artistName)
        {
            var result = parsedArtistName.CleanArtistName();
            result.Should().Be(artistName);
        }

        [TestCase("the")]
        [TestCase("and")]
        [TestCase("or")]
        [TestCase("a")]
        [TestCase("an")]
        [TestCase("of")]
        public void should_not_clean_word_from_beginning_of_string(string word)
        {
            var dirtyFormat = new[]
                            {
                                "{0}.word.word",
                                "{0}-word-word",
                                "{0} word word"
                            };

            foreach (var s in dirtyFormat)
            {
                var dirty = string.Format(s, word);
                dirty.CleanArtistName().Should().Be(word + "wordword");
            }
        }

        [Test]
        public void should_not_clean_trailing_a()
        {
            "Tokyo Ghoul A".CleanArtistName().Should().Be("tokyoghoula");
        }

        [TestCase("3%", "3percent")]
        [TestCase("Teen Top & 100% Outing Brothers", "teentop100percentoutingbrothers")]
        [TestCase("Big Jay Oakerson's What's Your F@%king Deal?!", "bigjayoakersonswhatsyourfkingdeal")]
        public void should_replace_percent_sign_with_percent_following_numbers(string input, string expected)
        {
            input.CleanArtistName().Should().Be(expected);
        }
    }
}
