using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityModelComparerFixture : CoreTest
    {
        public QualityModelComparer Subject { get; set; }

        private void GivenDefaultProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities() });
        }

        private void GivenCustomProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities(Quality.MP3_320, Quality.MP3_192) });
        }

        private void GivenGroupedProfile()
        {
            var profile = new QualityProfile
            {
                Items = new List<QualityProfileQualityItem>
                                      {
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = false,
                                              Quality = Quality.MP3_192
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Items = new List<QualityProfileQualityItem>
                                                      {
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.MP3_256
                                                          },
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.MP3_320
                                                          }
                                                      }
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Quality = Quality.FLAC
                                          }
                                      }
            };

            Subject = new QualityModelComparer(profile);
        }


        [Test]
        public void should_be_greater_when_first_quality_is_greater_than_second()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.MP3_320);
            var second = new QualityModel(Quality.MP3_192);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_lesser_when_second_quality_is_greater_than_first()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.MP3_192);
            var second = new QualityModel(Quality.MP3_320);

            var compare = Subject.Compare(first, second);

            compare.Should().BeLessThan(0);
        }

        [Test]
        public void should_be_greater_when_first_quality_is_a_proper_for_the_same_quality()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.MP3_320, new Revision(version: 2));
            var second = new QualityModel(Quality.MP3_320, new Revision(version: 1));

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_greater_when_using_a_custom_profile()
        {
            GivenCustomProfile();

            var first = new QualityModel(Quality.MP3_192);
            var second = new QualityModel(Quality.MP3_320);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_ignore_group_order_by_default()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.MP3_256);
            var second = new QualityModel(Quality.MP3_320);

            var compare = Subject.Compare(first, second);

            compare.Should().Be(0);
        }

        [Test]
        public void should_respect_group_order()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.MP3_256);
            var second = new QualityModel(Quality.MP3_320);

            var compare = Subject.Compare(first, second, true);

            compare.Should().BeLessThan(0);
        }
    }
}
