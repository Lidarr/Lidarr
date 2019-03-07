using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.RootFolders;
using NzbDrone.Test.Common;
using NzbDrone.Core.Parser.Model;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;

namespace NzbDrone.Core.Test.MediaFiles.DiskScanServiceTests
{
    [TestFixture]
    public class ScanFixture : FileSystemTest<DiskScanService>
    {
        private Artist _artist;
        private string _rootFolder;
        private string _otherArtistFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = @"C:\Test\Music".AsOsAgnostic();
            _otherArtistFolder = @"C:\Test\Music\OtherArtist".AsOsAgnostic();
            var artistFolder = @"C:\Test\Music\Artist".AsOsAgnostic();

            _artist = Builder<Artist>.CreateNew()
                                     .With(s => s.Path = artistFolder)
                                     .Build();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>()))
                .Returns(_rootFolder);

            Mocker.GetMock<IMakeImportDecision>()
                .Setup(v => v.GetImportDecisions(It.IsAny<List<FileInfoBase>>(), It.IsAny<Artist>(), It.IsAny<bool>()))
                .Returns(new List<ImportDecision<LocalTrack>>());

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>());

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.FilterExistingFiles(It.IsAny<List<FileInfoBase>>(), It.IsAny<Artist>()))
                .Returns((List<FileInfoBase> files, Artist artist) => files);
        }

        private void GivenRootFolder(params string[] subfolders)
        {
            FileSystem.AddDirectory(_rootFolder);

            foreach (var folder in subfolders)
            {
                FileSystem.AddDirectory(folder);
            }
        }

        private void GivenArtistFolder()
        {
            GivenRootFolder(_artist.Path);
        }

        private void GivenFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                FileSystem.AddFile(file, null);
            }
        }

        [Test]
        public void should_not_scan_if_root_folder_does_not_exist()
        {
            Subject.Scan(_artist);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_artist.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Artist>(), It.IsAny<List<string>>()), Times.Never());
        }

        [Test]
        public void should_not_scan_if_artist_root_folder_is_empty()
        {
            GivenRootFolder();

            Subject.Scan(_artist);

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_artist.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Artist>(), It.IsAny<List<string>>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.IsAny<List<FileInfoBase>>(), _artist, true), Times.Never());
        }

        [Test]
        public void should_create_if_artist_folder_does_not_exist_but_create_folder_enabled()
        {
            GivenRootFolder(_otherArtistFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyArtistFolders)
                  .Returns(true);

            Subject.Scan(_artist);

            DiskProvider.FolderExists(_artist.Path).Should().BeTrue();
        }

        [Test]
        public void should_not_create_if_artist_folder_does_not_exist_and_create_folder_disabled()
        {
            GivenRootFolder(_otherArtistFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyArtistFolders)
                  .Returns(false);

            Subject.Scan(_artist);

            DiskProvider.FolderExists(_artist.Path).Should().BeFalse();
        }

        [Test]
        public void should_clean_but_not_import_if_artist_folder_does_not_exist()
        {
            GivenRootFolder(_otherArtistFolder);

            Subject.Scan(_artist);

            DiskProvider.FolderExists(_artist.Path).Should().BeFalse();

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Artist>(), It.IsAny<List<string>>()), Times.Once());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.IsAny<List<FileInfoBase>>(), _artist, true), Times.Never());
        }

        [Test]
        public void should_clean_but_not_import_if_artist_folder_does_not_exist_and_create_folder_enabled()
        {
            GivenRootFolder(_otherArtistFolder);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.CreateEmptyArtistFolders)
                  .Returns(true);

            Subject.Scan(_artist);

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<Artist>(), It.IsAny<List<string>>()), Times.Once());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.IsAny<List<FileInfoBase>>(), _artist, true), Times.Never());
        }

        [Test]
        public void should_find_files_at_root_of_artist_folder()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "file1.flac"),
                           Path.Combine(_artist.Path, "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 2), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_extras_subfolder()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "EXTRAS", "file1.flac"),
                           Path.Combine(_artist.Path, "Extras", "file2.flac"),
                           Path.Combine(_artist.Path, "EXTRAs", "file3.flac"),
                           Path.Combine(_artist.Path, "ExTrAs", "file4.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_AppleDouble_subfolder()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, ".AppleDouble", "file1.flac"),
                           Path.Combine(_artist.Path, ".appledouble", "file2.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_scan_extras_artist_and_subfolders()
        {
            _artist.Path = @"C:\Test\Music\Extras".AsOsAgnostic();

            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "Extras", "file1.flac"),
                           Path.Combine(_artist.Path, ".AppleDouble", "file2.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e02.flac"),
                           Path.Combine(_artist.Path, "Season 2", "s02e01.flac"),
                           Path.Combine(_artist.Path, "Season 2", "s02e02.flac"),
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 4), _artist, true), Times.Once());
        }

        [Test]
        public void should_scan_files_that_start_with_period()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "Album 1", ".t01.mp3")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolders_that_start_with_period()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, ".@__thumb", "file1.flac"),
                           Path.Combine(_artist.Path, ".@__THUMB", "file2.flac"),
                           Path.Combine(_artist.Path, ".hidden", "file2.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolder_of_season_folder_that_starts_with_a_period()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "Season 1", ".@__thumb", "file1.flac"),
                           Path.Combine(_artist.Path, "Season 1", ".@__THUMB", "file2.flac"),
                           Path.Combine(_artist.Path, "Season 1", ".hidden", "file2.flac"),
                           Path.Combine(_artist.Path, "Season 1", ".AppleDouble", "s01e01.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_Synology_eaDir()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "@eaDir", "file1.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_not_scan_thumb_folder()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, ".@__thumb", "file1.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }

        [Test]
        public void should_scan_dotHack_folder()
        {
            _artist.Path = @"C:\Test\Music\.hack".AsOsAgnostic();

            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, "Season 1", "file1.flac"),
                           Path.Combine(_artist.Path, "Season 1", "s01e01.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 2), _artist, true), Times.Once());
        }

        [Test]
        public void should_exclude_osx_metadata_files()
        {
            GivenArtistFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_artist.Path, ".DS_STORE"),
                           Path.Combine(_artist.Path, "._24 The Status Quo Combustion.flac"),
                           Path.Combine(_artist.Path, "24 The Status Quo Combustion.flac")
                       });

            Subject.Scan(_artist);

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(v => v.GetImportDecisions(It.Is<List<FileInfoBase>>(l => l.Count == 1), _artist, true), Times.Once());
        }
    }
}
