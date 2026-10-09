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
            return Subject.IsExcluded(BasePath, fullPath);
        }

        [Test]
        public void should_not_exclude_any_folders_when_config_is_empty()
        {
            GivenExcludedFolders(string.Empty);

            Assert.IsFalse(IsExcluded("/music/album/song.flac"));
            Assert.IsFalse(IsExcluded("/music/various/song.flac"));
        }

        [Test]
        public void should_exclude_single_configured_folder()
        {
            GivenExcludedFolders("various");

            Assert.IsFalse(IsExcluded("/music/album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
        }

        [Test]
        public void should_exclude_multiple_configured_folders()
        {
            GivenExcludedFolders("various,.ignore,temp");

            Assert.IsFalse(IsExcluded("/music/album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/Various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/.ignore/song.flac"));
            Assert.IsTrue(IsExcluded("/music/temp/song.flac"));
        }

        [Test]
        public void should_exclude_folders_case_insensitively()
        {
            GivenExcludedFolders("various");

            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/Various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/VARIOUS/song.flac"));
            Assert.IsTrue(IsExcluded("/music/vArIoUs/song.flac"));
        }

        [Test]
        public void should_only_match_exact_directory_segment_not_substring()
        {
            GivenExcludedFolders("various");

            Assert.IsTrue(IsExcluded("/music/Various/song.flac"));
            Assert.IsFalse(IsExcluded("/music/Various Artists/song.flac"));
            Assert.IsFalse(IsExcluded("/music/SomeVariousFolder/song.flac"));
            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
        }

        [Test]
        public void should_exclude_configured_folders_with_spaces_around_commas()
        {
            GivenExcludedFolders("various, temp");

            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/temp/song.flac"));
        }

        [Test]
        public void should_exclude_configured_folders_with_multiple_spaces()
        {
            GivenExcludedFolders("various,     temp");

            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/temp/song.flac"));
        }

        [Test]
        public void should_exclude_configured_folders_with_leading_and_trailing_spaces()
        {
            GivenExcludedFolders("  various , temp  ");

            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/temp/song.flac"));
        }

        [Test]
        public void should_ignore_empty_entries_in_excluded_folders_config()
        {
            GivenExcludedFolders("various,,temp");

            Assert.IsFalse(IsExcluded("/music/Album/song.flac"));
            Assert.IsTrue(IsExcluded("/music/various/song.flac"));
            Assert.IsTrue(IsExcluded("/music/temp/song.flac"));
        }
    }
}
