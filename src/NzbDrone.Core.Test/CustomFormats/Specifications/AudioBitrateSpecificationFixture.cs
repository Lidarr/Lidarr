using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class AudioBitrateSpecificationFixture : CoreTest
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

        [TestCase(320, 256, 320, true)]
        [TestCase(256, 192, 320, true)]
        [TestCase(128, 128, 256, true)]
        [TestCase(1411, 1000, 1500, true)]
        [TestCase(96, 128, 320, false)]
        [TestCase(320, 0, 256, false)]
        [TestCase(0, 128, 320, false)]
        public void should_match_bitrate_range(int bitrate, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioBitrate = bitrate;

            var spec = new AudioBitrateSpecification
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

            var spec = new AudioBitrateSpecification
            {
                Min = 128,
                Max = 320
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [TestCase(320, 256, 320, false)]
        [TestCase(256, 192, 320, false)]
        [TestCase(96, 128, 320, true)]
        public void should_match_negated_bitrate_range(int bitrate, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioBitrate = bitrate;

            var spec = new AudioBitrateSpecification
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
            var spec = new AudioBitrateSpecification
            {
                Min = 0,
                Max = 320
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_max_equals_min()
        {
            var spec = new AudioBitrateSpecification
            {
                Min = 320,
                Max = 320
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_max_is_less_than_min()
        {
            var spec = new AudioBitrateSpecification
            {
                Min = 320,
                Max = 256
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_min_is_negative()
        {
            var spec = new AudioBitrateSpecification
            {
                Min = -1,
                Max = 320
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }
    }
}
