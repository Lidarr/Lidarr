using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Profiles.Tagging;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles.Tagging
{
    [TestFixture]
    public class TaggingProfileServiceFixture : CoreTest<TaggingProfileService>
    {
        private TaggingProfile _defaultProfile;
        private TaggingProfile _taggedProfile;
        private TaggingProfile _untaggedProfile;
        private List<TaggingProfile> _profiles;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(new CacheManager());

            _defaultProfile = new TaggingProfile
            {
                Id = 1,
                Name = "Default",
                Order = int.MaxValue,
                WriteAudioTags = WriteAudioTagsType.No
            };

            _taggedProfile = new TaggingProfile
            {
                Id = 2,
                Name = "Tagged",
                Order = 1,
                WriteAudioTags = WriteAudioTagsType.AllFiles,
                Tags = new HashSet<int> { 7 }
            };

            _untaggedProfile = new TaggingProfile
            {
                Id = 3,
                Name = "Catch all",
                Order = 2,
                WriteAudioTags = WriteAudioTagsType.NewFiles
            };

            _profiles = new List<TaggingProfile> { _defaultProfile, _taggedProfile, _untaggedProfile };

            Mocker.GetMock<ITaggingProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(_profiles);
        }

        [Test]
        public void best_for_tags_should_return_first_matching_profile_by_order()
        {
            var result = Subject.BestForTags(new HashSet<int> { 7 });

            result.Id.Should().Be(_taggedProfile.Id);
        }

        [Test]
        public void best_for_tags_should_match_untagged_profile_for_no_tags()
        {
            var result = Subject.BestForTags(new HashSet<int>());

            result.Id.Should().Be(_untaggedProfile.Id);
        }

        [Test]
        public void best_for_tags_should_match_untagged_profile_for_other_tags()
        {
            var result = Subject.BestForTags(new HashSet<int> { 9 });

            result.Id.Should().Be(_untaggedProfile.Id);
        }

        [Test]
        public void best_for_tags_should_fall_through_to_default_when_no_other_profile_matches()
        {
            _profiles.Remove(_untaggedProfile);

            var result = Subject.BestForTags(new HashSet<int> { 9 });

            result.Id.Should().Be(_defaultProfile.Id);
        }

        [Test]
        public void best_for_tags_should_return_null_when_no_profile_matches()
        {
            _profiles.Remove(_defaultProfile);
            _profiles.Remove(_untaggedProfile);

            var result = Subject.BestForTags(new HashSet<int> { 9 });

            result.Should().BeNull();
        }

        [Test]
        public void delete_should_renumber_orders_and_skip_default()
        {
            Subject.Delete(_taggedProfile.Id);

            _defaultProfile.Order.Should().Be(int.MaxValue);
        }

        [Test]
        public void reorder_should_move_profile_to_first_when_after_id_is_null()
        {
            var result = Subject.Reorder(_untaggedProfile.Id, null).OrderBy(d => d.Order).ToList();

            result.First().Id.Should().Be(_untaggedProfile.Id);
            result.First().Order.Should().Be(1);
        }

        [Test]
        public void get_seeded_default_profile_should_return_seeded_profile()
        {
            Subject.GetSeededDefaultProfile().Should().BeSameAs(_defaultProfile);
        }

        [Test]
        public void get_seeded_default_profile_should_fall_back_when_seeded_profile_is_missing()
        {
            _profiles.Remove(_defaultProfile);

            var result = Subject.GetSeededDefaultProfile();

            result.Id.Should().Be(0);
            result.WriteAudioTags.Should().Be(WriteAudioTagsType.No);
        }
    }
}
