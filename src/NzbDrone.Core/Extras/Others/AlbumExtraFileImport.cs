using System.IO;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Extras.Others
{
    public class AlbumExtraFileImport
    {
        public AlbumExtraFileImport(string sourceFilePath, string destinationFilePath)
        {
            SourcePath = sourceFilePath;
            DestinationPath = destinationFilePath;
        }

        public string SourcePath { get; }

        public string DestinationPath { get; }

        public static AlbumExtraFileImport AtDestinationDir(string sourceFilePath, string destinationDir)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var destinationPath = Path.Join(destinationDir, fileName);

            return new AlbumExtraFileImport(sourceFilePath, destinationPath);
        }

        public static AlbumExtraFileImport AtRelativePathFromSource(string sourceFilePath, string sourceRootDir, string destinationRootDir)
        {
            var relative = sourceRootDir.GetRelativePath(sourceFilePath);
            var destinationPath = Path.Join(destinationRootDir, relative);

            return new AlbumExtraFileImport(sourceFilePath, destinationPath);
        }
    }
}
