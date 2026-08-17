using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_tagging_profilesFixture : MigrationTest<add_tagging_profiles>
    {
        [Test]
        public void should_seed_default_profile_with_defaults_when_no_config()
        {
            var db = WithMigrationTestDb();

            var items = db.Query<TaggingProfile082>("SELECT * FROM \"TaggingProfiles\"");

            items.Should().HaveCount(1);
            items.First().Name.Should().Be("Default");
            items.First().Order.Should().Be(int.MaxValue);
            items.First().Tags.Should().Be("[]");
            items.First().WriteAudioTags.Should().Be(0);
            items.First().ScrubAudioTags.Should().BeFalse();
            items.First().EmbedCoverArt.Should().BeTrue();
            items.First().DownloadClientIds.Should().Be("[]");
            items.First().SkipHardlinkedFiles.Should().BeFalse();
        }

        [Test]
        public void should_seed_default_profile_from_existing_config()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "writeaudiotags",
                    Value = "sync"
                });

                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "scrubaudiotags",
                    Value = "true"
                });

                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "embedcoverart",
                    Value = "false"
                });
            });

            var items = db.Query<TaggingProfile082>("SELECT * FROM \"TaggingProfiles\"");

            items.Should().HaveCount(1);
            items.First().WriteAudioTags.Should().Be(3);
            items.First().ScrubAudioTags.Should().BeTrue();
            items.First().EmbedCoverArt.Should().BeFalse();
        }

        [Test]
        public void should_remove_metadata_config_keys()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "writeaudiotags",
                    Value = "allfiles"
                });

                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "metadatasource",
                    Value = "abc"
                });
            });

            var config = db.Query<Config082>("SELECT \"Key\", \"Value\" FROM \"Config\"");

            config.Should().HaveCount(1);
            config.First().Key.Should().Be("metadatasource");
        }
    }

    public class TaggingProfile082
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tags { get; set; }
        public int Order { get; set; }
        public int WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public bool EmbedCoverArt { get; set; }
        public string DownloadClientIds { get; set; }
        public bool SkipHardlinkedFiles { get; set; }
    }

    public class Config082
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
