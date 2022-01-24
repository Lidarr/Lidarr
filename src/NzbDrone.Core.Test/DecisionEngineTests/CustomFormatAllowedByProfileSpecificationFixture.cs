using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class CustomFormatAllowedByProfileSpecificationFixture : CoreTest<CustomFormatAllowedbyProfileSpecification>
    {
        private RemoteAlbum _remoteAlbum;

        private CustomFormat _format1;
        private CustomFormat _format2;

        [SetUp]
        public void Setup()
        {
            _format1 = new CustomFormat("Awesome Format");
            _format1.Id = 1;

            _format2 = new CustomFormat("Cool Format");
            _format2.Id = 2;

            var fakeArtist = Builder<Artist>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile
                {
                    Cutoff = Quality.MP3_320.Id,
                    MinFormatScore = 1
                })
                .Build();

            _remoteAlbum = new RemoteAlbum
            {
                Artist = fakeArtist,
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_016, new Revision(version: 2)) },
            };

            CustomFormatsTestHelpers.GivenCustomFormats(_format1, _format2);
        }

        [Test]
        public void should_allow_if_format_score_greater_than_min()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { _format1 };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_deny_if_format_score_not_greater_than_min()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { _format2 };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Console.WriteLine(_remoteAlbum.CustomFormatScore);
            Console.WriteLine(_remoteAlbum.Artist.QualityProfile.Value.MinFormatScore);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_deny_if_format_score_not_greater_than_min_2()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { _format2, _format1 };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_if_all_format_is_defined_in_profile()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { _format2, _format1 };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_deny_if_no_format_was_parsed_and_min_score_positive()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_if_no_format_was_parsed_min_score_is_zero()
        {
            _remoteAlbum.CustomFormats = new List<CustomFormat> { };
            _remoteAlbum.Artist.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteAlbum.Artist.QualityProfile.Value.MinFormatScore = 0;
            _remoteAlbum.CustomFormatScore = _remoteAlbum.Artist.QualityProfile.Value.CalculateCustomFormatScore(_remoteAlbum.CustomFormats);

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }
    }
}
