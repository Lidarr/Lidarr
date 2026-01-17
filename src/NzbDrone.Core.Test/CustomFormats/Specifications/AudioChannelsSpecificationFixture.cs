using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications
{
    [TestFixture]
    public class AudioChannelsSpecificationFixture : CoreTest
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

        [TestCase(2, 2, 2, true)]
        [TestCase(2, 1, 2, true)]
        [TestCase(6, 2, 8, true)]
        [TestCase(8, 6, 8, true)]
        [TestCase(1, 2, 6, false)]
        [TestCase(8, 0, 6, false)]
        [TestCase(0, 2, 6, false)]
        public void should_match_channels_range(int channels, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioChannels = channels;

            var spec = new AudioChannelsSpecification
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

            var spec = new AudioChannelsSpecification
            {
                Min = 2,
                Max = 6
            };

            spec.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [TestCase(2, 2, 2, false)]
        [TestCase(2, 1, 2, false)]
        [TestCase(1, 2, 6, true)]
        public void should_match_negated_channels_range(int channels, int min, int max, bool expected)
        {
            _input.MediaInfo.AudioChannels = channels;

            var spec = new AudioChannelsSpecification
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
            var spec = new AudioChannelsSpecification
            {
                Min = 0,
                Max = 6
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_max_equals_min()
        {
            var spec = new AudioChannelsSpecification
            {
                Min = 2,
                Max = 2
            };

            var result = spec.Validate();
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_max_is_less_than_min()
        {
            var spec = new AudioChannelsSpecification
            {
                Min = 6,
                Max = 2
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_min_is_negative()
        {
            var spec = new AudioChannelsSpecification
            {
                Min = -1,
                Max = 6
            };

            var result = spec.Validate();
            result.IsValid.Should().BeFalse();
        }
    }
}
