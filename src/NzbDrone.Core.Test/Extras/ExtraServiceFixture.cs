using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Extras;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras
{
    public class ExtraServiceFixture : CoreTest<ExtraService>
    {
        private string _albumDir;
        private Artist _artist;
        private Album _album;

        [SetUp]
        public void CommonSetup()
        {
            var artistDir = @"C:\Test\Music\Foo Fooers".AsOsAgnostic();
            _artist = new Artist()
            {
                QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() },
                Path = artistDir,
            };
            _album = new Album()
            {
                Id = 15,
                Artist = _artist,
                Title = "Twenty Thirties",
            };
            var release = new AlbumRelease()
            {
                AlbumId = _album.Id,
                Monitored = true,
            };
            _album.AlbumReleases = new List<AlbumRelease> { release };
            _albumDir = Path.Combine(_artist.Path, $"{_album.Title} (1995) [FLAC]");

            Mocker.GetMock<IDiskProvider>()
                .Setup(x => x.GetParentFolder(It.IsAny<string>()))
                .Returns<string>(arg => Path.GetDirectoryName(arg.AsOsAgnostic()));

            Mocker.GetMock<IConfigService>()
                .Setup(x => x.ImportExtraFiles).Returns(true);
            Mocker.GetMock<IConfigService>()
                .Setup(x => x.ExtraFileExtensions).Returns(".cue,.nfo,.log,.jpg");

            // Rename on by default
            var cfg = NamingConfig.Default;
            cfg.RenameTracks = true;
            Mocker.GetMock<INamingConfigService>().Setup(x => x.GetConfig()).Returns(cfg);
        }

        public class AlbumImportTests : ExtraServiceFixture
        {
            private List<ImportDecision<LocalTrack>> _importDecisions;
            private List<string> _importDirExtraFiles;

            [SetUp]
            public void Setup()
            {
                var track = NewTrack(_album, _albumDir, "01 - hello world.flac");
                _importDecisions = new ()
                {
                    new ImportDecision<LocalTrack>(track)
                };
                _importDirExtraFiles = new List<string>
                {
                    Path.Combine(_albumDir, "album.cue"),
                    Path.Combine(_albumDir, "albumfoo_barz.jpg"),
                    Path.Combine(_albumDir, "release.nfo"),
                    Path.Combine(_albumDir, "eac.log"),
                };

                Mocker.GetMock<IMediaFileService>().Setup(x => x.GetFilesByArtist(_album.ArtistId))
                    .Returns(track.Tracks.Select(t => t.TrackFile.Value).ToList());
                Mocker.GetMock<ITrackService>().Setup(x => x.GetTracksByArtist(_album.ArtistId))
                    .Returns(new List<Track> { track.Tracks.Single() });
            }

            [Test]
            public void should_import_extras_during_manual_import_with_naming_config_having_rename_on()
            {
                SetupFilesUnderCommonDir(_albumDir, _importDecisions.Select(d => d.Item.Path).Concat(_importDirExtraFiles));

                // act
                Subject.ImportAlbumExtras(_importDecisions);

                // assert
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == _importDirExtraFiles.Count)));
            }

            [TestCase(false)]
            [TestCase(true)]
            public void should_not_import_extras_when_no_separate_album_dir_set(bool testStandardTrackFormat)
            {
                SetupFilesUnderCommonDir(_albumDir, _importDecisions.Select(d => d.Item.Path).Concat(_importDirExtraFiles));

                var cfg = NamingConfig.Default;
                cfg.RenameTracks = true;

                // modify either standard or multidisc format to test both branches:
                if (testStandardTrackFormat)
                {
                    cfg.StandardTrackFormat = "{Artist Name} - {Album Title} - {track:00} - {Track Title}";
                }
                else
                {
                    cfg.MultiDiscTrackFormat = "{Medium Format} {medium:00}/{Artist Name} - {Album Title} - {track:00} - {Track Title}";
                }

                SetupNamingConfig(cfg);

                Subject.ImportAlbumExtras(_importDecisions);

                Mocker.GetMock<IOtherExtraFileService>().VerifyNoOtherCalls();
            }

            [Test]
            public void should_import_extra_from_multi_cd_root_dir()
            {
                var cd1Subdir = Path.Combine(_albumDir, "CD1");
                var cd2Subdir = Path.Combine(_albumDir, "CD2");

                var cd1Track = NewTrack(_album, cd1Subdir, "101 - Foo Track.flac");
                var cd2Track = NewTrack(_album, cd2Subdir, "201 - bonustrackbar.flac");

                var extraFileInAlbumRoot = Path.Combine(_albumDir, "album.cue");

                SetupFilesUnderCommonDir(_albumDir, cd1Track.Path, cd2Track.Path, extraFileInAlbumRoot);

                // act
                var decisions = new List<ImportDecision<LocalTrack>>
                {
                    new ImportDecision<LocalTrack>(cd1Track),
                    new ImportDecision<LocalTrack>(cd2Track),
                };

                Subject.ImportAlbumExtras(decisions);

                // assert
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == 1)));
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(
                        It.Is<List<OtherExtraFile>>(
                            arg => arg.Single().Extension == ".cue"
                                && arg.Single().RelativePath.AsOsAgnostic() == _artist.Path.GetRelativePath(extraFileInAlbumRoot).AsOsAgnostic())));
            }

            [TestCase("")]
            [TestCase("extras_subdir")]
            public void should_move_album_extra_to_correct_subdir_on_artist_renamed_event(string extraFilesDir)
            {
                var newDir = $"{_albumDir} [Release FOO]".AsOsAgnostic();
                var renamed = new List<RenamedTrackFile>();
                foreach (var import in _importDecisions)
                {
                    renamed.Add(new RenamedTrackFile()
                    {
                        PreviousPath = import.Item.Path,
                        TrackFile = new TrackFile()
                        {
                            Id = 11,
                            Album = _album,
                            AlbumId = _album.Id,
                            Path = import.Item.Path.Replace(_albumDir, newDir),
                            Tracks = new List<Track>()
                            {
                                new Track() { Album = _album, Artist = _artist, TrackFileId = 11 },
                            }
                        },
                    });
                }

                var relativePathBeforeMove = Path.Combine(new DirectoryInfo(_albumDir).Name, extraFilesDir, "album.cue");
                var albumExtra = new OtherExtraFile
                {
                    Id = 251,
                    AlbumId = _album.Id,
                    ArtistId = _album.ArtistId,
                    RelativePath = relativePathBeforeMove,
                    Extension = ".cue",
                    Added = DateTime.UtcNow,
                    TrackFileId = null,
                };

                Mocker.GetMock<IOtherExtraFileService>().Setup(x => x.GetFilesByArtist(_album.ArtistId))
                    .Returns(new List<OtherExtraFile>() { albumExtra });

                // act
                Subject.Handle(new ArtistRenamedEvent(_artist, renamed));

                var expectedExtraDir = Path.Combine(newDir, extraFilesDir);

                // assert
                Mocker.GetMock<IDiskProvider>()
                    .Verify(x => x.MoveFile(
                        It.Is<string>(arg => arg.Contains(relativePathBeforeMove)),
                        It.Is<string>(arg => arg.Contains(expectedExtraDir)),
                        It.IsAny<bool>()), Times.Once);
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == 1)));
            }

            [Test]
            public void should_move_album_extras_for_multicd_release_on_artist_renamed_event()
            {
                var newAlbumDir = $"{_albumDir} 2CDs".AsOsAgnostic();

                var oldCd1Subdir = Path.Combine(_albumDir, "Disk 1");
                var oldCd2Subdir = Path.Combine(_albumDir, "Disk 2");
                var cd1Subdir = Path.Combine(newAlbumDir, "CD1");
                var cd2Subdir = Path.Combine(newAlbumDir, "CD2");
                var cd1Track = NewTrack(_album, cd1Subdir, "101 - Foo Track.flac");
                var cd2Track = NewTrack(_album, cd2Subdir, "201 - bonustrackbar.flac");

                var renamed = new List<RenamedTrackFile>()
                {
                    new RenamedTrackFile
                    {
                        PreviousPath = Path.Combine(oldCd1Subdir, "101 - Foo Track.flac"),
                        TrackFile = cd1Track.Tracks.Single().TrackFile.Value,
                    },
                    new RenamedTrackFile
                    {
                        PreviousPath = Path.Combine(oldCd2Subdir, "201 - bonustrackbar.flac"),
                        TrackFile = cd2Track.Tracks.Single().TrackFile.Value,
                    },
                };

                var albumDirExtraOldRelativePath = Path.Combine(new DirectoryInfo(_albumDir).Name, "album.cue");
                var albumExtraAtRoot = new OtherExtraFile
                {
                    Id = 251,
                    AlbumId = _album.Id,
                    ArtistId = _album.ArtistId,
                    RelativePath = albumDirExtraOldRelativePath,
                    Extension = ".cue",
                    Added = DateTime.UtcNow,
                    TrackFileId = null,
                };

                var cd1ExtraOldRelativePath = Path.Combine(_artist.Path.GetRelativePath(oldCd1Subdir), "cd1.log");
                var cd1ExtraFile = new OtherExtraFile()
                {
                    Id = 252,
                    AlbumId = _album.Id,
                    ArtistId = _album.ArtistId,
                    RelativePath = cd1ExtraOldRelativePath,
                    Extension = ".log",
                    Added = DateTime.UtcNow,
                    TrackFileId = null,
                };

                Mocker.GetMock<IOtherExtraFileService>().Setup(x => x.GetFilesByArtist(_album.ArtistId))
                    .Returns(new List<OtherExtraFile>() { albumExtraAtRoot, cd1ExtraFile });

                // act
                Subject.Handle(new ArtistRenamedEvent(_artist, renamed));

                // verify
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == 2)));

                // assert
                Mocker.GetMock<IDiskProvider>()
                    .Verify(x => x.MoveFile(
                        It.Is<string>(arg => arg.EndsWithIgnoreCase(albumDirExtraOldRelativePath)),
                        It.Is<string>(arg => arg.StartsWith(newAlbumDir)),
                        It.IsAny<bool>()), Times.Once);

                Mocker.GetMock<IDiskProvider>()
                    .Verify(x => x.MoveFile(
                        It.Is<string>(arg => arg.EndsWithIgnoreCase(cd1ExtraOldRelativePath)),
                        It.Is<string>(arg => arg.StartsWith(cd1Subdir)),
                        It.IsAny<bool>()), Times.Once);
            }
        }

        public class AlbumDownloadTests : ExtraServiceFixture
        {
            private string _downloadDir;
            private List<ImportDecision<LocalTrack>> _approvedDownloadDecisions;
            private List<string> _downloadDirExtraFiles;

            [SetUp]
            public void Setup()
            {
                _downloadDir = @"C:\temp\downloads\Artist - TT (1995) FLAC".AsOsAgnostic();
                var downloadedTrack = NewTrack(_album, _albumDir, "01 - First seconds.flac", _downloadDir);
                _approvedDownloadDecisions = new List<ImportDecision<LocalTrack>>()
                {
                    new ImportDecision<LocalTrack>(downloadedTrack),
                };
                _downloadDirExtraFiles = new List<string>
                {
                    Path.Combine(_downloadDir, "album.cue"),
                    Path.Combine(_downloadDir, "cover.nfo"),
                    Path.Combine(_downloadDir, "eac.log"),
                };
            }

            [Test]
            public void should_import_extras_from_download_location()
            {
                SetupFilesUnderCommonDir(_downloadDir, _approvedDownloadDecisions.Select(d => d.Item.Path).Concat(_downloadDirExtraFiles));

                Subject.ImportAlbumExtras(_approvedDownloadDecisions);

                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == _downloadDirExtraFiles.Count)));
                foreach (var sourcePath in _downloadDirExtraFiles)
                {
                    Mocker.GetMock<IDiskTransferService>()
                        .Verify(x => x.TransferFile(
                            It.Is<string>(arg => arg.AsOsAgnostic() == sourcePath.AsOsAgnostic()),
                            It.Is<string>(arg => arg.AsOsAgnostic().StartsWith(_albumDir.AsOsAgnostic())),
                            It.IsAny<TransferMode>(),
                            It.IsAny<bool>()));
                }
            }

            [Test]
            public void should_not_import_track_specific_extras()
            {
                var trackName = Path.GetFileNameWithoutExtension(_approvedDownloadDecisions.First().Item.Path);
                var trackExtra = Path.Combine(_downloadDir, $"{trackName}.cue");

                SetupFilesUnderCommonDir(_downloadDir,
                    _approvedDownloadDecisions.Select(d => d.Item.Path).Concat(_downloadDirExtraFiles)
                        .Append(trackExtra));

                Subject.ImportAlbumExtras(_approvedDownloadDecisions);

                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == _downloadDirExtraFiles.Count)));

                Mocker.GetMock<IDiskTransferService>()
                    .Verify(x => x.TransferFile(
                        It.Is<string>(arg => arg.AsOsAgnostic() == trackExtra.AsOsAgnostic()),
                        It.IsAny<string>(),
                        It.IsAny<TransferMode>(),
                        It.IsAny<bool>()),
                    Times.Never);
            }

            [Test]
            public void should_import_with_extensions_from_settings()
            {
                SetupFilesUnderCommonDir(_downloadDir, _downloadDirExtraFiles);

                Mocker.GetMock<IConfigService>()
                    .Setup(x => x.ExtraFileExtensions)
                    .Returns(".cue, .txt");

                Subject.ImportAlbumExtras(_approvedDownloadDecisions);

                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(
                        arg => arg.Count == 1
                        && arg.Single().Extension == ".cue")));
            }

            [Test]
            public void should_not_import_extras_with_naming_cfg_having_rename_off()
            {
                SetupFilesUnderCommonDir(_downloadDir,
                    _approvedDownloadDecisions.Select(d => d.Item.Path)
                        .Concat(_downloadDirExtraFiles));

                var cfg = NamingConfig.Default;
                cfg.RenameTracks = false; // explicitly set for readability
                SetupNamingConfig(cfg);

                Subject.ImportAlbumExtras(_approvedDownloadDecisions);

                Mocker.GetMock<IOtherExtraFileService>().VerifyNoOtherCalls();
            }

            [TestCase("{Album Title} ({Release Year})")]
            [TestCase("{ALBUM TITLE} ({Release Year})")]
            [TestCase("{Album Title}")]
            [TestCase("{Album.Title}")]
            [TestCase("{Album_Title}")]
            public void should_import_extras_rename_pattern_contains_album_title(string albumDirPattern)
            {
                SetupFilesUnderCommonDir(_downloadDir,
                    _approvedDownloadDecisions.Select(d => d.Item.Path)
                        .Concat(_downloadDirExtraFiles));

                var cfg = NamingConfig.Default;
                cfg.RenameTracks = true;

                cfg.StandardTrackFormat = cfg.StandardTrackFormat
                    .Replace("{Album Title} ({Release Year})", albumDirPattern);
                cfg.MultiDiscTrackFormat = cfg.MultiDiscTrackFormat
                    .Replace("{Album Title} ({Release Year})", albumDirPattern);

                SetupNamingConfig(cfg);

                // act
                Subject.ImportAlbumExtras(_approvedDownloadDecisions);

                // assert
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == _downloadDirExtraFiles.Count)));
            }

            [Test]
            public void should_import_extra_from_multi_cd_subdirs()
            {
                var cd1Source = Path.Combine(_downloadDir, "CD1");
                var cd2Source = Path.Combine(_downloadDir, "CD2");
                var cd1Destination = Path.Combine(_albumDir, "Disk 1");
                var cd2Destination = Path.Combine(_albumDir, "Disk 2");

                var cd1Track = NewTrack(_album, cd1Destination, "101 - Foo Track.flac", cd1Source);
                var cd2Track = NewTrack(_album, cd2Destination, "201 - bonustrackbar.flac", cd2Source);
                var decisions = new List<ImportDecision<LocalTrack>>
                {
                    new ImportDecision<LocalTrack>(cd1Track),
                    new ImportDecision<LocalTrack>(cd2Track),
                };
                var cd1Extra = Path.Combine(cd1Source, "cd1_foo.cue");
                var cd2Extra = Path.Combine(cd2Source, "cd2_bar.cue");

                SetupFilesUnderCommonDir(_downloadDir, cd1Track.Path, cd1Extra, cd2Track.Path, cd2Extra);

                Subject.ImportAlbumExtras(decisions);

                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == 2)));
            }

            [Test]
            public void should_import_from_separate_extras_dir_having_no_tracks()
            {
                var cd1Track = NewTrack(_album, _albumDir, "101 - Foo Track.flac", _downloadDir);
                var cd2Track = NewTrack(_album, _albumDir, "201 - Bonustrackbar.flac", _downloadDir);
                var extraFileInRoot = Path.Combine(_downloadDir, "cuesheet.cue");
                var extraFileInSubdir = Path.Combine(_downloadDir, "artwork", "cover.jpg");

                SetupFilesUnderCommonDir(_downloadDir, cd1Track.Path, cd2Track.Path, extraFileInRoot, extraFileInSubdir);
                var decisions = new List<ImportDecision<LocalTrack>>
                {
                    new ImportDecision<LocalTrack>(cd1Track),
                    new ImportDecision<LocalTrack>(cd2Track),
                };
                Subject.ImportAlbumExtras(decisions);

                // assert
                Mocker.GetMock<IOtherExtraFileService>()
                    .Verify(x => x.Upsert(It.Is<List<OtherExtraFile>>(arg => arg.Count == 2)));
            }

            [TestCase(new string[] { "" }, null)]
            [TestCase(new string[] { "files" }, null)]
            [TestCase(new string[] { "first", "second_dir" }, null)]
            [TestCase(new string[] { "Disk 1" }, new string[] { "CD1" })]
            [TestCase(new string[] { "Disk 2", "cd2_extras" }, new string[] { "CD2", "cd2_extras" })]
            public void should_copy_multicd_extra_file_to_correct_subdirectory(string[] sourcePathDirs, string[] destinationPathDirs = null)
            {
                var relativeSourcePath = Path.Combine(sourcePathDirs);
                var relativeDestinationPath = destinationPathDirs != null ? Path.Combine(destinationPathDirs) : relativeSourcePath;

                var cd1Source = Path.Combine(_downloadDir, "Disk 1");
                var cd2Source = Path.Combine(_downloadDir, "Disk 2");
                var cd1Destination = Path.Combine(_albumDir, "CD1");
                var cd2Destination = Path.Combine(_albumDir, "CD2");

                var cd1Track = NewTrack(_album, cd1Destination, "101 - Foo Track.flac", cd1Source);
                var cd2Track = NewTrack(_album, cd2Destination, "201 - bonustrackbar.flac", cd2Source);
                var extraFileName = "foobarextra.nfo";
                var extraFilePath = Path.Combine(_downloadDir, relativeSourcePath, extraFileName);

                SetupFilesUnderCommonDir(_downloadDir, cd1Track.Path, cd2Track.Path, extraFilePath);

                var decisions = new List<ImportDecision<LocalTrack>>
                {
                    new ImportDecision<LocalTrack>(cd1Track),
                    new ImportDecision<LocalTrack>(cd2Track),
                };

                Subject.ImportAlbumExtras(decisions);

                var expectedExtraPath = Path.Combine(_albumDir, relativeDestinationPath, extraFileName);

                Mocker.GetMock<IDiskTransferService>()
                  .Verify(x => x.TransferFile(
                      It.Is<string>(arg => arg.AsOsAgnostic() == extraFilePath.AsOsAgnostic()),
                      It.Is<string>(arg => arg.AsOsAgnostic() == expectedExtraPath.AsOsAgnostic()),
                      It.IsAny<TransferMode>(),
                      It.IsAny<bool>()),
                    Times.Once);
            }

            [Test]
            public void should_copy_multicd_nosubdir_extras_at_destination_root()
            {
                var cd1Destination = Path.Combine(_albumDir, "CD1");
                var cd2Destination = Path.Combine(_albumDir, "CD2");
                var cd1Track = NewTrack(_album, cd1Destination, "101 - Foo Track.flac", _downloadDir);
                var cd2Track = NewTrack(_album, cd2Destination, "201 - bonustrackbar.flac", _downloadDir);
                var extraFile = Path.Combine(_downloadDir, "album.jpg");

                SetupFilesUnderCommonDir(_downloadDir, cd1Track.Path, cd2Track.Path, extraFile);

                var decisions = new List<ImportDecision<LocalTrack>>
                {
                    new ImportDecision<LocalTrack>(cd1Track),
                    new ImportDecision<LocalTrack>(cd2Track),
                };
                Subject.ImportAlbumExtras(decisions);

                // assert
                var expectedExtraDestination = Path.Combine(_albumDir, "album.jpg");
                Mocker.GetMock<IDiskTransferService>()
                    .Verify(x => x.TransferFile(
                        It.Is<string>(arg => arg == extraFile),
                        It.Is<string>(arg => arg == expectedExtraDestination),
                        It.IsAny<TransferMode>(),
                        It.IsAny<bool>()));
            }
        }

        /// <summary>
        /// Set <paramref name="cfg"/> as the current naming configuration for the current test.
        /// </summary>
        /// <param name="cfg">The naming config to return from <see cref="INamingConfigService"/>.</param>
        private void SetupNamingConfig(NamingConfig cfg)
        {
            Mocker.GetMock<INamingConfigService>().Setup(x => x.GetConfig()).Returns(cfg);
        }

        /// <summary>
        /// Create a new track record with a given path and optional source dir for the download.
        /// </summary>
        /// <param name="album">Track album</param>
        /// <param name="trackDir">The directory of the track file in the Lidarr library dir.</param>
        /// <param name="trackFileName">File name.</param>
        /// <param name="downloadSourceDir">The source dir when the import is from a download. Pass null for track import.</param>
        private LocalTrack NewTrack(Album album, string trackDir, string trackFileName, string downloadSourceDir = null)
        {
            var sourcePath = Path.Combine(downloadSourceDir ?? trackDir, trackFileName);
            var destinationPath = Path.Combine(trackDir, trackFileName);
            return new LocalTrack
            {
                Artist = album.Artist,
                Album = album,
                Release = album.AlbumReleases.Value.First(),
                Tracks = new List<Track>
                {
                    new Track()
                    {
                        Album = album,
                        TrackFile = new LazyLoaded<TrackFile>(
                            new TrackFile { Album = _album, AlbumId = _album.Id, Path = destinationPath })
                    },
                },
                Path = sourcePath,
            };
        }

        private void SetupFilesUnderCommonDir(string rootDir, IEnumerable<string> filePath)
        {
            SetupFilesUnderCommonDir(rootDir, filePath.ToArray());
        }

        private void SetupFilesUnderCommonDir(string rootDir, params string[] filePaths)
        {
            Mocker.GetMock<IDiskProvider>()
                 .Setup(x => x.GetFiles(It.Is<string>(arg => arg.AsOsAgnostic() == rootDir.AsOsAgnostic()), true))
                 .Returns(filePaths);

            var fileGroups = filePaths.GroupBy(x => Path.GetDirectoryName(x))
                .OrderBy(p => p.Key.Length).ToArray();

            for (var i = 0; i < fileGroups.Length; i++)
            {
                var currentDir = fileGroups[i].Key;

                // current dir
                Mocker.GetMock<IDiskProvider>()
                    .Setup(x => x.GetFiles(It.Is<string>(arg => arg.AsOsAgnostic() == currentDir.AsOsAgnostic()), false))
                    .Returns(fileGroups[i]);

                // recursive search
                var subdirs = fileGroups[i..fileGroups.Length]
                    .Where(grp => grp.Key.StartsWith(currentDir));

                Mocker.GetMock<IDiskProvider>()
                    .Setup(x => x.GetFiles(It.Is<string>(arg => arg.AsOsAgnostic() == currentDir.AsOsAgnostic()), true))
                    .Returns(subdirs.SelectMany(f => f));
            }
        }
    }
}
