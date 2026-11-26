using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_aiff_quality_in_profilesFixture : MigrationTest<add_aiff_quality_in_profiles>
    {
        private static string GenerateQualityJson(int quality, bool allowed)
        {
            return $"{{ \"quality\": {quality}, \"items\": [], \"allowed\": {allowed.ToString().ToLowerInvariant()} }}";
        }

        private static string GenerateGroupJson(int id, string name, bool allowed, params Quality[] qualities)
        {
            var items = string.Join(", ", qualities.Select(q => GenerateQualityJson(q.Id, allowed)));

            return $"{{ \"id\": {id}, \"name\": \"{name}\", \"items\": [{items}], \"allowed\": {allowed.ToString().ToLowerInvariant()} }}";
        }

        private static string GenerateLosslessGroupJson(bool allowed)
        {
            return GenerateGroupJson(1005, "Lossless", allowed, Quality.FLAC, Quality.ALAC, Quality.APE, Quality.WAVPACK, Quality.FLAC_24, Quality.ALAC_24);
        }

        private QualityProfile82 Migrate(string items, int cutoff = 1005)
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("QualityProfiles").Row(new
                {
                    Name = "Profile",
                    Cutoff = cutoff,
                    Items = items
                });
            });

            return db.Query<QualityProfile82>("SELECT \"Id\", \"Name\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\" LIMIT 1").Single();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_add_aiff_between_lossless_group_and_wav_with_wav_allowed_state(bool wavAllowed)
        {
            var profile = Migrate($"[{GenerateQualityJson(Quality.MP3_320.Id, false)}, {GenerateLosslessGroupJson(true)}, {GenerateQualityJson(Quality.WAV.Id, wavAllowed)}]");

            profile.Items.Select(v => v.Quality).Should().Equal(Quality.MP3_320.Id, null, Quality.AIFF.Id, Quality.WAV.Id);
            profile.Items.Select(v => v.Allowed).Should().Equal(false, true, wavAllowed, wavAllowed);
            profile.Items[1].Items.Select(v => v.Quality).Should().Equal(
                Quality.FLAC.Id, Quality.ALAC.Id, Quality.APE.Id, Quality.WAVPACK.Id, Quality.FLAC_24.Id, Quality.ALAC_24.Id);
            profile.Cutoff.Should().Be(1005);
        }

        [Test]
        public void should_add_aiff_directly_before_wav_when_wav_has_been_moved()
        {
            var profile = Migrate($"[{GenerateQualityJson(Quality.WAV.Id, false)}, {GenerateQualityJson(Quality.MP3_320.Id, true)}, {GenerateLosslessGroupJson(true)}]");

            profile.Items.Select(v => v.Quality).Should().Equal(Quality.AIFF.Id, Quality.WAV.Id, Quality.MP3_320.Id, null);
            profile.Items.Select(v => v.Allowed).Should().Equal(false, false, true, true);
        }

        [Test]
        public void should_add_aiff_inside_custom_group_directly_before_wav()
        {
            var customGroup = GenerateGroupJson(1006, "Lossless and WAV", true, Quality.FLAC, Quality.FLAC_24, Quality.WAV, Quality.ALAC);

            var profile = Migrate($"[{GenerateQualityJson(Quality.MP3_320.Id, false)}, {customGroup}]", 1006);

            profile.Items.Should().HaveCount(2);
            profile.Items[1].Name.Should().Be("Lossless and WAV");
            profile.Items[1].Items.Select(v => v.Quality).Should().Equal(Quality.FLAC.Id, Quality.FLAC_24.Id, Quality.AIFF.Id, Quality.WAV.Id, Quality.ALAC.Id);
            profile.Items[1].Items.Select(v => v.Allowed).Should().AllBeEquivalentTo(true);
            profile.Cutoff.Should().Be(1006);
        }

        [Test]
        public void should_add_aiff_as_not_allowed_after_alac_24_group_when_profile_has_no_wav()
        {
            var profile = Migrate($"[{GenerateQualityJson(Quality.MP3_320.Id, false)}, {GenerateLosslessGroupJson(true)}, {GenerateQualityJson(Quality.Unknown.Id, false)}]");

            profile.Items.Select(v => v.Quality).Should().Equal(Quality.MP3_320.Id, null, Quality.AIFF.Id, Quality.Unknown.Id);
            profile.Items[2].Allowed.Should().BeFalse();
        }

        [Test]
        public void should_append_aiff_as_not_allowed_when_profile_has_no_wav_or_alac_24()
        {
            var profile = Migrate($"[{GenerateQualityJson(Quality.MP3_320.Id, true)}, {GenerateQualityJson(Quality.FLAC.Id, true)}]", Quality.FLAC.Id);

            profile.Items.Select(v => v.Quality).Should().Equal(Quality.MP3_320.Id, Quality.FLAC.Id, Quality.AIFF.Id);
            profile.Items.Last().Allowed.Should().BeFalse();
            profile.Cutoff.Should().Be(Quality.FLAC.Id);
        }

        [Test]
        public void should_not_add_aiff_twice()
        {
            var profile = Migrate($"[{GenerateQualityJson(Quality.FLAC.Id, true)}, {GenerateQualityJson(Quality.AIFF.Id, false)}, {GenerateQualityJson(Quality.WAV.Id, true)}]", Quality.FLAC.Id);

            profile.Items.Select(v => v.Quality).Should().Equal(Quality.FLAC.Id, Quality.AIFF.Id, Quality.WAV.Id);
            profile.Items.Select(v => v.Allowed).Should().Equal(true, false, true);
        }

        [Test]
        public void should_not_add_aiff_twice_when_it_is_in_a_group()
        {
            var group = GenerateGroupJson(1006, "Uncompressed", true, Quality.AIFF, Quality.WAV);

            var profile = Migrate($"[{GenerateLosslessGroupJson(true)}, {group}]");

            profile.Items.Should().HaveCount(2);
            profile.Items[1].Items.Select(v => v.Quality).Should().Equal(Quality.AIFF.Id, Quality.WAV.Id);
        }
    }
}
