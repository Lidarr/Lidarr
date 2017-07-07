﻿using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles
{
    [TestFixture]
    public class ProfileRepositoryFixture : DbTest<ProfileRepository, Profile>
    {
        [Test]
        public void should_be_able_to_read_and_write()
        {
            var profile = new Profile
                {
                    Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3_320, Quality.MP3_192, Quality.MP3_256),
                    Cutoff = Quality.MP3_320,
                    Name = "TestProfile"
                };

            Subject.Insert(profile);

            StoredModel.Name.Should().Be(profile.Name);
            StoredModel.Cutoff.Should().Be(profile.Cutoff);

            StoredModel.Items.Should().Equal(profile.Items, (a, b) => a.Quality == b.Quality && a.Allowed == b.Allowed);


        }
    }
}