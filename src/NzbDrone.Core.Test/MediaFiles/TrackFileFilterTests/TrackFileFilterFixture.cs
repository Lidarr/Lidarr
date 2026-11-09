using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class TrackFileFilterFixture : CoreTest<TrackFileFilter>
    {
        private const string BasePath = "/music";

        private void GivenExcludedFolders(string value)
        {
            Mocker.GetMock<IConfigService>()
                .SetupGet(x => x.ExcludedScanFolders)
                .Returns(value);
        }

        private bool IsExcluded(string fullPath)
        {
            return Subject.GetExclusionPredicate(BasePath)(fullPath);
        }

        [Test]
        public void should_not_exclude_any_folders_when_config_is_empty()
        {
            GivenExcludedFolders(string.Empty);

            Assert.That(IsExcluded("/music/album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/various/song.flac"), Is.False);
        }

        [Test]
        public void should_exclude_single_configured_folder()
        {
            GivenExcludedFolders("various");

            Assert.That(IsExcluded("/music/album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/various/song.flac"), Is.True);
        }

        [Test]
        public void should_exclude_multiple_configured_folders()
        {
            GivenExcludedFolders("various\n.ignore\ntemp");

            Assert.That(IsExcluded("/music/album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/Various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/.ignore/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/temp/song.flac"), Is.True);
        }

        [Test]
        public void should_exclude_folders_case_insensitively()
        {
            GivenExcludedFolders("various");

            Assert.That(IsExcluded("/music/Album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/Various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/VARIOUS/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/vArIoUs/song.flac"), Is.True);
        }

        [Test]
        public void should_only_match_exact_directory_segment_not_substring()
        {
            GivenExcludedFolders("various");

            Assert.That(IsExcluded("/music/Various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/Various Artists/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/SomeVariousFolder/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/Album/song.flac"), Is.False);
        }

        [Test]
        public void should_not_exclude_file_when_file_name_matches_excluded_folder()
        {
            GivenExcludedFolders("temp");

            Assert.That(IsExcluded("/music/album/temp"), Is.False);
            Assert.That(IsExcluded("/music/album/temp.flac"), Is.False);
            Assert.That(IsExcluded("/music/temp/song.flac"), Is.True);
        }

        [Test]
        public void should_exclude_configured_folders_with_leading_and_trailing_spaces_on_lines()
        {
            GivenExcludedFolders("  various  \n  temp  ");

            Assert.That(IsExcluded("/music/Album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/temp/song.flac"), Is.True);
        }

        [Test]
        public void should_ignore_empty_entries_in_excluded_folders_config()
        {
            GivenExcludedFolders("various\n\ntemp");

            Assert.That(IsExcluded("/music/Album/song.flac"), Is.False);
            Assert.That(IsExcluded("/music/various/song.flac"), Is.True);
            Assert.That(IsExcluded("/music/temp/song.flac"), Is.True);
        }

        [Test]
        public void should_exclude_folder_names_containing_commas()
        {
            GivenExcludedFolders("90s Dance Hits, Volume 4");

            Assert.That(
                IsExcluded("/music/90s Dance Hits, Volume 4/song.flac"),
                Is.True);

            Assert.That(
                IsExcluded("/music/90s Dance Hits/song.flac"),
                Is.False);
        }
    }
}
