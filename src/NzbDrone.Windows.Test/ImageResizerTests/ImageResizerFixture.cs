using NUnit.Framework;
using NzbDrone.Common.Test.ImageResizerTests;

namespace NzbDrone.Windows.Test.ImageResizerTests
{
    [TestFixture]
    [Platform("Win")]
    public class ImageResizerFixture : ImageResizerFixtureBase<NzbDrone.Windows.ImageResizer.ImageResizer>
    {
        public ImageResizerFixture()
        {
            WindowsOnly();
        }
    }
}
