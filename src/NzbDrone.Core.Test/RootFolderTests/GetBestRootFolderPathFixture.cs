using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RootFolderTests
{
    [TestFixture]
    public class GetBestRootFolderPathFixture : CoreTest<RootFolderService>
    {
        private void GivenRootFolders(params string[] paths)
        {
            Mocker.GetMock<IRootFolderRepository>()
                .Setup(s => s.All())
                .Returns(paths.Select(p => new RootFolder { Path = p }));
        }

        [Test]
        public void should_return_root_folder_that_is_parent_path()
        {
            GivenRootFolders(@"C:\Test\Music".AsOsAgnostic(), @"D:\Test\Music".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Music\Series Title".AsOsAgnostic()).Should().Be(@"C:\Test\Music".AsOsAgnostic());
        }

        [Test]
        public void should_return_root_folder_that_is_grandparent_path()
        {
            GivenRootFolders(@"C:\Test\Music".AsOsAgnostic(), @"D:\Test\Music".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Music\S\Series Title".AsOsAgnostic()).Should().Be(@"C:\Test\Music".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_diskProvider_if_matching_root_folder_is_not_found()
        {
            var seriesPath = @"T:\Test\Music\Series Title".AsOsAgnostic();

            GivenRootFolders(@"C:\Test\Music".AsOsAgnostic(), @"D:\Test\Music".AsOsAgnostic());
            Subject.GetBestRootFolderPath(seriesPath);

            Mocker.GetMock<IDiskProvider>()
                .Verify(v => v.GetParentFolder(seriesPath), Times.Once);
        }
    }
}
