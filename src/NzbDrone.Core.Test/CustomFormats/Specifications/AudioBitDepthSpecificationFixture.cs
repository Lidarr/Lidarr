using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class AudioBitDepthSpecificationFixture : CoreTest
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

        [TestCase(16, 16, 24, true)]
        [TestCase(24, 16, 24, true)]
        [TestCase(24, 24, 32, true)]
        [TestCase(32, 24, 32, true)]
        [TestCase(16, 24, 32, false)]
        [TestCase(32, 0, 24, false)]
        [TestCase(0, 16, 24, false)]
        public void should_match_bit_depth_range(int bitDepth, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioBits = bitDepth;

            var spec = new AudioBitDepthSpecification
            {
                Min = min,
                Max = max
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [Test]
        public void should_not_match_when_media_info_is_null()
        {
            _input.MediaInfo = null;

            var spec = new AudioBitDepthSpecification
            {
                Min = 16,
                Max = 24
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [TestCase(16, 16, 24, false)]
        [TestCase(24, 16, 24, false)]
        [TestCase(16, 24, 32, true)]
        public void should_match_negated_bit_depth_range(int bitDepth, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioBits = bitDepth;

            var spec = new AudioBitDepthSpecification
            {
                Min = min,
                Max = max,
                Negate = true
            };

            spec.IsSatisfiedBy(_input).Should().Be(expected);
        }

        [Test]
        public void should_be_valid_when_min_is_zero()
        {
            var spec = new AudioBitDepthSpecification
            {
                Min = 0,
                Max = 24
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_max_equals_min()
        {
            var spec = new AudioBitDepthSpecification
            {
                Min = 24,
                Max = 24
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_max_is_less_than_min()
        {
            var spec = new AudioBitDepthSpecification
            {
                Min = 24,
                Max = 16
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_min_is_negative()
        {
            var spec = new AudioBitDepthSpecification
            {
                Min = -1,
                Max = 24
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }
    }
}
