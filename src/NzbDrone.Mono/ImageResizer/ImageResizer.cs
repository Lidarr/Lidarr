using NzbDrone.Common.Disk;
using NzbDrone.Common.ImageResizer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Memory;

namespace NzbDrone.Mono.ImageResizer
{
    public class ImageResizer : ImageResizerBase
    {
        public ImageResizer(IDiskProvider diskProvider) : base(diskProvider)
        {
            // More conservative memory allocation
            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator = new SimpleGcMemoryAllocator();
        }

        protected override void ResizeWithoutCleanup(string source, string destination, int height)
        {
            using (var image = Image.Load(source))
            {
                image.Mutate(x => x.Resize(0, height, KnownResamplers.Lanczos3));
                image.Save(destination);
            }
        }
    }
}
