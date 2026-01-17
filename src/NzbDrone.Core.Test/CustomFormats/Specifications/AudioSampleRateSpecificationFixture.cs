using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class AudioSampleRateSpecificationFixture : CoreTest
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

        [TestCase(44100, 40000, 48000, true)]
        [TestCase(48000, 40000, 50000, true)]
        [TestCase(96000, 88200, 192000, true)]
        [TestCase(192000, 176400, 192000, true)]
        [TestCase(44100, 48000, 96000, false)]
        [TestCase(192000, 0, 96000, false)]
        [TestCase(0, 44100, 96000, false)]
        public void should_match_sample_rate_range(int sampleRate, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioSampleRate = sampleRate;

            var spec = new AudioSampleRateSpecification
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

            var spec = new AudioSampleRateSpecification
            {
                Min = 44100,
                Max = 48000
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [TestCase(44100, 40000, 48000, false)]
        [TestCase(48000, 40000, 50000, false)]
        [TestCase(44100, 48000, 96000, true)]
        public void should_match_negated_sample_rate_range(int sampleRate, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioSampleRate = sampleRate;

            var spec = new AudioSampleRateSpecification
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
            var spec = new AudioSampleRateSpecification
            {
                Min = 0,
                Max = 48000
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_max_equals_min()
        {
            var spec = new AudioSampleRateSpecification
            {
                Min = 44100,
                Max = 44100
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_max_is_less_than_min()
        {
            var spec = new AudioSampleRateSpecification
            {
                Min = 48000,
                Max = 44100
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_min_is_negative()
        {
            var spec = new AudioSampleRateSpecification
            {
                Min = -1,
                Max = 48000
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }
    }
}
