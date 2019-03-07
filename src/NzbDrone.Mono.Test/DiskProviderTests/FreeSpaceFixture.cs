using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Test.DiskTests;
using NzbDrone.Mono.Disk;

namespace NzbDrone.Mono.Test.DiskProviderTests
{
    [TestFixture]
    [Platform("Mono")]
    public class FreeSpaceFixture : FreeSpaceFixtureBase<DiskProvider>
    {
        public FreeSpaceFixture()
        {
            MonoOnly();
        }

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IProcMountProvider>().Setup(x => x.GetMounts()).Returns(new List<IMount>());
            Mocker.GetMock<ISymbolicLinkResolver>().Setup(x => x.GetCompleteRealPath(It.IsAny<string>())).Returns((string x) => x);
        }
    }
}
