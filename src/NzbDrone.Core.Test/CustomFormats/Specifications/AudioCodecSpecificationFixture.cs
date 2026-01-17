using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class AudioCodecSpecificationFixture : CoreTest
    {
        private CustomFormatInput _input;

        [SetUp]
        public void Setup()
        {
            _input = new CustomFormatInput
            {
                AlbumInfo = new ParsedAlbumInfo(),
                MediaInfo = new MediaInfoModel()
            };
        }

        [TestCase("FLAC", "FLAC", true)]
        [TestCase("MP3", "MP3", true)]
        [TestCase("AAC", "AAC", true)]
        [TestCase("ALAC", "ALAC", true)]
        [TestCase("OPUS", "OPUS", true)]
        [TestCase("WavPack", "WavPack", true)]
        [TestCase("APE", "APE", true)]
        [TestCase("FLAC", "MP3", false)]
        [TestCase("MP3", "FLAC", false)]
        public void should_match_codec_exact(string actualCodec, string pattern, bool expected)
        {
            _input.MediaInfo.AudioFormat = actualCodec;

            var spec = new AudioCodecSpecification
            {
                Value = pattern
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [TestCase("FLAC", "flac", true)]
        [TestCase("mp3", "MP3", true)]
        [TestCase("AAC", "aac", true)]
        public void should_match_codec_case_insensitive(string actualCodec, string pattern, bool expected)
        {
            _input.MediaInfo.AudioFormat = actualCodec;

            var spec = new AudioCodecSpecification
            {
                Value = pattern
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [TestCase("FLAC", "FLAC|MP3", true)]
        [TestCase("MP3", "FLAC|MP3", true)]
        [TestCase("AAC", "FLAC|MP3", false)]
        [TestCase("ALAC", "ALAC|OPUS|WavPack", true)]
        public void should_match_codec_regex(string actualCodec, string pattern, bool expected)
        {
            _input.MediaInfo.AudioFormat = actualCodec;

            var spec = new AudioCodecSpecification
            {
                Value = pattern
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [Test]
        public void should_not_match_when_media_info_is_null()
        {
            _input.MediaInfo = null;

            var spec = new AudioCodecSpecification
            {
                Value = "FLAC"
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_not_match_when_audio_format_is_null()
        {
            _input.MediaInfo.AudioFormat = null;

            var spec = new AudioCodecSpecification
            {
                Value = "FLAC"
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_not_match_when_audio_format_is_empty()
        {
            _input.MediaInfo.AudioFormat = string.Empty;

            var spec = new AudioCodecSpecification
            {
                Value = "FLAC"
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [TestCase("FLAC", "FLAC", false)]
        [TestCase("MP3", "MP3", false)]
        [TestCase("FLAC", "MP3", true)]
        public void should_match_negated_codec(string actualCodec, string pattern, bool expected)
        {
            _input.MediaInfo.AudioFormat = actualCodec;

            var spec = new AudioCodecSpecification
            {
                Value = pattern,
                Negate = true
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [Test]
        public void should_be_valid_when_value_is_set()
        {
            var spec = new AudioCodecSpecification
            {
                Value = "FLAC"
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_value_is_empty()
        {
            var spec = new AudioCodecSpecification
            {
                Value = string.Empty
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_value_is_whitespace()
        {
            var spec = new AudioCodecSpecification
            {
                Value = "   "
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_value_is_null()
        {
            var spec = new AudioCodecSpecification
            {
                Value = null
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }
    }
}
