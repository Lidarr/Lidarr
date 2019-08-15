using NUnit.Framework;
using NzbDrone.Common.Test.ImageResizerTests;

namespace NzbDrone.Mono.Test.ImageResizerTests
{
    [TestFixture]
    [Platform("Mono")]
    public class ImageResizerFixture : ImageResizerFixtureBase<NzbDrone.Mono.ImageResizer.ImageResizer>
    {
        public ImageResizerFixture()
        {
            MonoOnly();
        }
    }
}
