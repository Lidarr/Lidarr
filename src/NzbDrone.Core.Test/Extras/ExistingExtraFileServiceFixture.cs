using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Extras
{
    [TestFixture]
    public class ExistingExtraFileServiceFixture : CoreTest<ExistingExtraFileService>
    {
        private const string ArtistPath = @"C:\Music\Artist";
        private const string IncludedFile = @"C:\Music\Artist\cover.jpg";
        private const string ExcludedFile = @"C:\Music\Artist\excluded\cover.jpg";

        private Artist _artist;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist
            {
                Path = ArtistPath
            };

            Mocker.SetConstant<IEnumerable<IImportExistingExtraFiles>>(
                new[] { Mocker.GetMock<IImportExistingExtraFiles>().Object });

            Mocker.GetMock<IDiskProvider>()
                .Setup(x => x.FolderExists(ArtistPath))
                .Returns(true);

            Mocker.GetMock<IDiskScanService>()
                .Setup(x => x.GetNonAudioFiles(ArtistPath, true))
                .Returns(new[] { IncludedFile, ExcludedFile });

            Mocker.GetMock<IDiskScanService>()
                .Setup(x => x.FilterPaths(ArtistPath, It.IsAny<IEnumerable<string>>()))
                .Returns(new List<string> { IncludedFile, ExcludedFile });

            Mocker.GetMock<ITrackFileFilter>()
                .Setup(x => x.GetExclusionPredicate(ArtistPath))
                .Returns((string basePath) => fullPath => fullPath == ExcludedFile);
        }

        [Test]
        public void should_not_pass_excluded_files_to_extra_file_importer()
        {
            Mocker.GetMock<IImportExistingExtraFiles>()
                .Setup(x => x.ProcessFiles(
                    It.IsAny<Artist>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<List<string>>()))
                .Returns(Enumerable.Empty<ExtraFile>());

            Subject.Handle(new ArtistScannedEvent(_artist));

            Mocker.GetMock<IImportExistingExtraFiles>()
                .Verify(x => x.ProcessFiles(
                    _artist,
                    It.Is<List<string>>(files =>
                        files.SequenceEqual(new[] { IncludedFile })),
                    It.IsAny<List<string>>()),
                    Times.Once());
        }
    }
}
