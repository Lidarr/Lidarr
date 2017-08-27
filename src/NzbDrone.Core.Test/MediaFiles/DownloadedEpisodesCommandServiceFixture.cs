using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class DownloadedTracksCommandServiceFixture : CoreTest<DownloadedTracksCommandService>
    {
        private string _droneFactory = "c:\\drop\\".AsOsAgnostic();
        private string _downloadFolder = "c:\\drop_other\\Artist.Album\\".AsOsAgnostic();
        private string _downloadFile = "c:\\drop_other\\Artist.Album.mp3".AsOsAgnostic();

        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.DownloadedAlbumsFolder)
                  .Returns(_droneFactory);

            Mocker.GetMock<IDownloadedTracksImportService>()
                .Setup(v => v.ProcessRootFolder(It.IsAny<DirectoryInfo>()))
                .Returns(new List<ImportResult>());

            Mocker.GetMock<IDownloadedTracksImportService>()
                .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Artist>(), It.IsAny<DownloadClientItem>()))
                .Returns(new List<ImportResult>());

            var downloadItem = Builder<DownloadClientItem>.CreateNew()
                .With(v => v.DownloadId = "sab1")
                .With(v => v.Status = DownloadItemStatus.Downloading)
                .Build();

            var remoteAlbum = Builder<RemoteAlbum>.CreateNew()
                .With(v => v.Artist = new Artist())
                .Build();

            _trackedDownload = new TrackedDownload
                    {
                        DownloadItem = downloadItem,
                        RemoteAlbum = remoteAlbum,
                        State = TrackedDownloadStage.Downloading
                    };
        }

        private void GivenExistingFolder(string path)
        {
            Mocker.GetMock<IDiskProvider>().Setup(c => c.FolderExists(It.IsAny<string>()))
                    .Returns(true);
        }

        private void GivenExistingFile(string path)
        {
            Mocker.GetMock<IDiskProvider>().Setup(c => c.FileExists(It.IsAny<string>()))
                    .Returns(true);
        }

        private void GivenValidQueueItem()
        {
            Mocker.GetMock<ITrackedDownloadService>()
                  .Setup(s => s.Find("sab1"))
                  .Returns(_trackedDownload);
        }

        [Test]
        public void should_process_dronefactory_if_path_is_not_specified()
        {
            GivenExistingFolder(_droneFactory);

            Subject.Execute(new DownloadedTracksScanCommand());

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessRootFolder(It.IsAny<DirectoryInfo>()), Times.Once());
        }

        [Test]
        public void should_skip_import_if_dronefactory_doesnt_exist()
        {
            Subject.Execute(new DownloadedTracksScanCommand());

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessRootFolder(It.IsAny<DirectoryInfo>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_ignore_downloadclientid_if_path_is_not_specified()
        {
            GivenExistingFolder(_droneFactory);

            Subject.Execute(new DownloadedTracksScanCommand() { DownloadClientId = "sab1" });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessRootFolder(It.IsAny<DirectoryInfo>()), Times.Once());
        }

        [Test]
        public void should_process_folder_if_downloadclientid_is_not_specified()
        {
            GivenExistingFolder(_downloadFolder);

            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFolder });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Once());
        }

        [Test]
        public void should_process_file_if_downloadclientid_is_not_specified()
        {
            GivenExistingFile(_downloadFile);

            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFile });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Once());
        }

        [Test]
        public void should_process_folder_with_downloadclientitem_if_available()
        {
            GivenExistingFolder(_downloadFolder);
            GivenValidQueueItem();

            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFolder, DownloadClientId = "sab1" });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(_downloadFolder, ImportMode.Auto, _trackedDownload.RemoteAlbum.Artist, _trackedDownload.DownloadItem), Times.Once());
        }

        [Test]
        public void should_process_folder_without_downloadclientitem_if_not_available()
        {
            GivenExistingFolder(_downloadFolder);

            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFolder, DownloadClientId = "sab1" });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(_downloadFolder, ImportMode.Auto, null, null), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_warn_if_neither_folder_or_file_exists()
        {
            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFolder });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_override_import_mode()
        {
            GivenExistingFile(_downloadFile);

            Subject.Execute(new DownloadedTracksScanCommand() { Path = _downloadFile, ImportMode = ImportMode.Copy });

            Mocker.GetMock<IDownloadedTracksImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Copy, null, null), Times.Once());
        }
    }
}
