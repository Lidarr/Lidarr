using NzbDrone.Common.Disk;

namespace NzbDrone.Common.ImageResizer
{
    public interface IImageResizer
    {
        void Resize(string source, string destination, int height);
    }

    public abstract class ImageResizerBase : IImageResizer
    {
        protected readonly IDiskProvider _diskProvider;

        public ImageResizerBase(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public void Resize(string source, string destination, int height)
        {
            try
            {
                ResizeWithoutCleanup(source, destination, height);
            }
            catch
            {
                if (_diskProvider.FileExists(destination))
                {
                    _diskProvider.DeleteFile(destination);
                }
                throw;
            }
        }

        protected abstract void ResizeWithoutCleanup(string source, string destination, int height);
    }
}
