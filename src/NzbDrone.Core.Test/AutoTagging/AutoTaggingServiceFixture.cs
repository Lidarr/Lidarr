using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.AutoTagging.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AutoTagging
{
    [TestFixture]
    public class AutoTaggingServiceFixture : CoreTest<AutoTaggingService>
    {
        private Artist _artist;
        private AutoTag _tag;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>.CreateNew()
                .With(s => s.Metadata = new ArtistMetadata
                {
                    Genres = new List<string> { "Rock" }
                })
                .Build();

            _tag = new AutoTag
            {
                Name = "Test",
                Specifications = new List<IAutoTaggingSpecification>
                {
                    new GenreSpecification
                    {
                        Name = "Genre",
                        Value = new List<string>
                        {
                            "Rock"
                        }
                    }
                },
                Tags = new HashSet<int> { 1 },
                RemoveTagsAutomatically = false
            };
        }

        private void GivenAutoTags(List<AutoTag> autoTags)
        {
            Mocker.GetMock<IAutoTaggingRepository>()
                  .Setup(s => s.All())
                  .Returns(autoTags);
        }

        [Test]
        public void should_not_have_changes_if_there_are_no_auto_tags()
        {
            GivenAutoTags(new List<AutoTag>());

            var result = Subject.GetTagChanges(_artist);

            result.TagsToAdd.Should().BeEmpty();
            result.TagsToRemove.Should().BeEmpty();
        }

        [Test]
        public void should_have_tags_to_add_if_artist_does_not_have_match_tag()
        {
            GivenAutoTags(new List<AutoTag> { _tag });

            var result = Subject.GetTagChanges(_artist);

            result.TagsToAdd.Should().HaveCount(1);
            result.TagsToAdd.Should().Contain(1);
            result.TagsToRemove.Should().BeEmpty();
        }

        [Test]
        public void should_not_have_tags_to_remove_if_artist_has_matching_tag_but_remove_is_false()
        {
            _artist.Tags = new HashSet<int> { 1 };
            _artist.Metadata.Value.Genres = new List<string> { "NotComedy" };

            GivenAutoTags(new List<AutoTag> { _tag });

            var result = Subject.GetTagChanges(_artist);

            result.TagsToAdd.Should().BeEmpty();
            result.TagsToRemove.Should().BeEmpty();
        }

        [Test]
        public void should_have_tags_to_remove_if_artist_has_matching_tag_and_remove_is_true()
        {
            _artist.Tags = new HashSet<int> { 1 };
            _artist.Metadata.Value.Genres = new List<string> { "NotComedy" };

            _tag.RemoveTagsAutomatically = true;

            GivenAutoTags(new List<AutoTag> { _tag });

            var result = Subject.GetTagChanges(_artist);

            result.TagsToAdd.Should().BeEmpty();
            result.TagsToRemove.Should().HaveCount(1);
            result.TagsToRemove.Should().Contain(1);
        }

        [Test]
        public void should_match_if_specification_is_negated()
        {
            _artist.Metadata.Value.Genres = new List<string> { "NotComedy" };

            _tag.Specifications.First().Negate = true;

            GivenAutoTags(new List<AutoTag> { _tag });

            var result = Subject.GetTagChanges(_artist);

            result.TagsToAdd.Should().HaveCount(1);
            result.TagsToAdd.Should().Contain(1);
            result.TagsToRemove.Should().BeEmpty();
        }
    }
}
