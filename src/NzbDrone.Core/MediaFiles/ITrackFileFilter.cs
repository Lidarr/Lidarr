namespace NzbDrone.Core.MediaFiles
{
    public interface ITrackFileFilter
    {
        bool IsExcluded(string basePath, string fullPath);
    }
}
