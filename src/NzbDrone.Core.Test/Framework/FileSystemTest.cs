using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Core.Test.Framework
{
    public abstract class FileSystemTest<TSubject> : CoreTest<TSubject>
        where TSubject : class
    {
        protected MockFileSystem FileSystem { get; private set; }
        protected IDiskProvider DiskProvider { get; private set; }

        [SetUp]
        public void FileSystemTestSetup()
        {
            FileSystem = (MockFileSystem)Mocker.Resolve<IFileSystem>(FileSystemType.Mock);

            DiskProvider = Mocker.Resolve<IDiskProvider>(FileSystemType.Mock);
        }
    }
}
