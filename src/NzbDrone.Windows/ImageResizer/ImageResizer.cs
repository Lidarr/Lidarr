using ImageResizer;
using NzbDrone.Common.Disk;
using NzbDrone.Common.ImageResizer;

namespace NzbDrone.Windows.ImageResizer
{
    public class ImageResizer : ImageResizerBase
    {
        public ImageResizer(IDiskProvider diskProvider) : base(diskProvider)
        {
        }

        protected override void ResizeWithoutCleanup(string source, string destination, int height)
        {
            using (var sourceStream = _diskProvider.OpenReadStream(source))
            {
                using (var outputStream = _diskProvider.OpenWriteStream(destination))
                {
                    var settings = new Instructions();
                    settings.Height = height;

                    var job = new ImageJob(sourceStream, outputStream, settings);

                    ImageBuilder.Current.Build(job);
                }
            }
        }
    }
}
