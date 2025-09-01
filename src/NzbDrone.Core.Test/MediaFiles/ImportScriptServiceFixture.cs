using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ImportScriptServiceFixture : CoreTest<ImportScriptService>
    {
        private LocalTrack _localTrack;
        private TrackFile _trackFile;
        private Artist _artist;
        private Album _album;
        private List<Track> _tracks;
        private Tag _tag;

        [SetUp]
        public void Setup()
        {
            _tag = Builder<Tag>.CreateNew()
                .With(t => t.Id = 1)
                .With(t => t.Label = "TestTag")
                .Build();

            _artist = Builder<Artist>.CreateNew()
                .With(a => a.Id = 1)
                .With(a => a.Name = "Test Artist")
                .With(a => a.Path = "/music/Test Artist")
                .With(a => a.ForeignArtistId = "test-artist-mbid")
                .With(a => a.Tags = new HashSet<int> { 1 })
                .Build();

            _album = Builder<Album>.CreateNew()
                .With(a => a.Id = 1)
                .With(a => a.Title = "Test Album")
                .With(a => a.ForeignAlbumId = "test-album-mbid")
                .With(a => a.ReleaseDate = new System.DateTime(2023, 1, 1))
                .With(a => a.Genres = new List<string> { "Rock", "Alternative" })
                .Build();

            _tracks = new List<Track>
            {
                Builder<Track>.CreateNew()
                    .With(t => t.Id = 1)
                    .With(t => t.TrackNumber = "1")
                    .With(t => t.Title = "Test Track 1")
                    .Build(),
                Builder<Track>.CreateNew()
                    .With(t => t.Id = 2)
                    .With(t => t.TrackNumber = "2")
                    .With(t => t.Title = "Test Track 2")
                    .Build()
            };

            var mediaInfo = Builder<MediaInfoModel>.CreateNew()
                .With(m => m.AudioChannels = 2)
                .With(m => m.AudioFormat = "FLAC")
                .With(m => m.AudioBitrate = 1000)
                .With(m => m.AudioSampleRate = 44100)
                .With(m => m.AudioBits = 16)
                .Build();

            var fileTrackInfo = Builder<ParsedTrackInfo>.CreateNew()
                .With(p => p.MediaInfo = mediaInfo)
                .Build();

            _localTrack = Builder<LocalTrack>.CreateNew()
                .With(l => l.Artist = _artist)
                .With(l => l.Album = _album)
                .With(l => l.Tracks = _tracks)
                .With(l => l.Quality = new QualityModel(Quality.FLAC))
                .With(l => l.ReleaseGroup = "TestGroup")
                .With(l => l.SceneName = "Test.Scene.Name")
                .With(l => l.FileTrackInfo = fileTrackInfo)
                .Build();

            _trackFile = Builder<TrackFile>.CreateNew()
                .With(t => t.Path = "/destination/path/track.flac")
                .Build();

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.UseScriptImport)
                .Returns(true);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ScriptImportPath)
                .Returns("/usr/local/bin/import_script.sh");

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ApplicationUrl)
                .Returns("http://localhost:8686");

            Mocker.GetMock<IConfigFileProvider>()
                .Setup(s => s.InstanceName)
                .Returns("Lidarr");

            Mocker.GetMock<ITagRepository>()
                .Setup(s => s.Get(1))
                .Returns(_tag);

            var customFormats = Builder<CustomFormat>.CreateListOfSize(2)
                .TheFirst(1)
                .With(f => f.Name = "Lossless")
                .TheNext(1)
                .With(f => f.Name = "Scene")
                .Build().ToList();

            Mocker.GetMock<ICustomFormatCalculationService>()
                .Setup(s => s.ParseCustomFormat(_localTrack))
                .Returns(customFormats);
        }

        [Test]
        public void should_return_defer_when_script_import_disabled()
        {
            // Given
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.UseScriptImport)
                .Returns(false);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            result.Should().Be(ScriptImportDecision.DeferMove);
            Mocker.GetMock<IProcessProvider>()
                .Verify(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()), Times.Never);
        }

        [Test]
        public void should_call_script_with_correct_arguments()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 0,
                Lines = new List<ProcessOutputLine> { new ProcessOutputLine(ProcessOutputLevel.Standard, "Script executed successfully") }
            };

            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Returns(processOutput);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            Mocker.GetMock<IProcessProvider>()
                .Verify(p => p.StartAndCapture(
                    "/usr/local/bin/import_script.sh",
                    "\"/source/path\" \"/dest/path\"",
                    It.IsAny<StringDictionary>()),
                Times.Once);

            result.Should().Be(ScriptImportDecision.MoveComplete);
        }

        [Test]
        public void should_pass_correct_environment_variables()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 3,
                Lines = new List<ProcessOutputLine>()
            };

            StringDictionary capturedEnv = null;
            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Callback<string, string, StringDictionary>((script, args, env) => capturedEnv = env)
                .Returns(processOutput);

            // When
            Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Copy);

            // Then
            capturedEnv.Should().NotBeNull();

            // Basic paths and instance info
            capturedEnv["Lidarr_SourcePath"].Should().Be("/source/path");
            capturedEnv["Lidarr_DestinationPath"].Should().Be("/dest/path");
            capturedEnv["Lidarr_InstanceName"].Should().Be("Lidarr");
            capturedEnv["Lidarr_ApplicationUrl"].Should().Be("http://localhost:8686");
            capturedEnv["Lidarr_TransferMode"].Should().Be("Copy");

            // Artist info
            capturedEnv["Lidarr_Artist_Id"].Should().Be("1");
            capturedEnv["Lidarr_Artist_Name"].Should().Be("Test Artist");
            capturedEnv["Lidarr_Artist_Path"].Should().Be("/music/Test Artist");
            capturedEnv["Lidarr_Artist_MBId"].Should().Be("test-artist-mbid");
            capturedEnv["Lidarr_Artist_Tags"].Should().Be("TestTag");

            // Album info
            capturedEnv["Lidarr_Album_Id"].Should().Be("1");
            capturedEnv["Lidarr_Album_Title"].Should().Be("Test Album");
            capturedEnv["Lidarr_Album_MBId"].Should().Be("test-album-mbid");
            capturedEnv["Lidarr_Album_ReleaseDate"].Should().Be("2023-01-01");
            capturedEnv["Lidarr_Album_Genres"].Should().Be("Rock|Alternative");

            // Track info
            capturedEnv["Lidarr_TrackFile_TrackCount"].Should().Be("2");
            capturedEnv["Lidarr_TrackFile_TrackIds"].Should().Be("1,2");
            capturedEnv["Lidarr_TrackFile_TrackNumbers"].Should().Be("1,2");
            capturedEnv["Lidarr_TrackFile_TrackTitles"].Should().Be("Test Track 1|Test Track 2");
            capturedEnv["Lidarr_TrackFile_Quality"].Should().Be("FLAC");
            capturedEnv["Lidarr_TrackFile_ReleaseGroup"].Should().Be("TestGroup");
            capturedEnv["Lidarr_TrackFile_SceneName"].Should().Be("Test.Scene.Name");

            // Media info
            capturedEnv["Lidarr_TrackFile_MediaInfo_AudioChannels"].Should().Be("2");
            capturedEnv["Lidarr_TrackFile_MediaInfo_AudioCodec"].Should().Be("FLAC");
            capturedEnv["Lidarr_TrackFile_MediaInfo_AudioBitRate"].Should().Be("1000");
            capturedEnv["Lidarr_TrackFile_MediaInfo_AudioSampleRate"].Should().Be("44100");
            capturedEnv["Lidarr_TrackFile_MediaInfo_BitsPerSample"].Should().Be("16");

            // Custom formats
            capturedEnv["Lidarr_TrackFile_CustomFormat"].Should().Be("Lossless|Scene");

            // Download client info (should be empty when not provided)
            capturedEnv["Lidarr_Download_Client"].Should().Be("");
            capturedEnv["Lidarr_Download_Client_Type"].Should().Be("");
            capturedEnv["Lidarr_Download_Id"].Should().Be("");
        }

        [Test]
        public void should_include_download_client_info_when_provided()
        {
            // Given
            var downloadClientInfo = Builder<DownloadClientItemClientInfo>.CreateNew()
                .With(d => d.Name = "qBittorrent")
                .With(d => d.Type = "Torrent")
                .Build();

            var downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                .With(d => d.DownloadClientInfo = downloadClientInfo)
                .With(d => d.DownloadId = "test-download-id")
                .Build();

            var processOutput = new ProcessOutput
            {
                ExitCode = 3,
                Lines = new List<ProcessOutputLine>()
            };

            StringDictionary capturedEnv = null;
            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Callback<string, string, StringDictionary>((script, args, env) => capturedEnv = env)
                .Returns(processOutput);

            // When
            Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move, downloadClientItem);

            // Then
            capturedEnv["Lidarr_Download_Client"].Should().Be("qBittorrent");
            capturedEnv["Lidarr_Download_Client_Type"].Should().Be("Torrent");
            capturedEnv["Lidarr_Download_Id"].Should().Be("test-download-id");
        }

        [Test]
        public void should_return_move_complete_when_script_returns_0()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 0,
                Lines = new List<ProcessOutputLine>()
            };

            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Returns(processOutput);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            result.Should().Be(ScriptImportDecision.MoveComplete);
        }

        [Test]
        public void should_return_rename_requested_when_script_returns_2()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 2,
                Lines = new List<ProcessOutputLine>()
            };

            var audioTag = Builder<AudioTag>.CreateNew()
                .With(a => a.MediaInfo = new MediaInfoModel())
                .Build();

            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Returns(processOutput);

            Mocker.GetMock<IAudioTagService>()
                .Setup(s => s.ReadTags("/dest/path"))
                .Returns(audioTag);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            result.Should().Be(ScriptImportDecision.RenameRequested);
            _trackFile.MediaInfo.Should().Be(audioTag.MediaInfo);
            _trackFile.Path.Should().BeNull();

            Mocker.GetMock<IAudioTagService>()
                .Verify(s => s.ReadTags("/dest/path"), Times.Once);
        }

        [Test]
        public void should_return_defer_move_when_script_returns_3()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 3,
                Lines = new List<ProcessOutputLine>()
            };

            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Returns(processOutput);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            result.Should().Be(ScriptImportDecision.DeferMove);
        }

        [Test]
        public void should_throw_exception_when_script_returns_error_code()
        {
            // Given
            var processOutput = new ProcessOutput
            {
                ExitCode = 1,
                Lines = new List<ProcessOutputLine> { new ProcessOutputLine(ProcessOutputLevel.Error, "Error message from script") }
            };

            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Returns(processOutput);

            // When & Then
            Assert.Throws<ScriptImportException>(() =>
                Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move));
        }

        [Test]
        public void should_handle_missing_media_info_gracefully()
        {
            // Given
            _localTrack.FileTrackInfo.MediaInfo = null;

            var processOutput = new ProcessOutput
            {
                ExitCode = 3,
                Lines = new List<ProcessOutputLine>()
            };

            StringDictionary capturedEnv = null;
            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Callback<string, string, StringDictionary>((script, args, env) => capturedEnv = env)
                .Returns(processOutput);

            // When
            Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            capturedEnv.Should().NotBeNull();
            capturedEnv.ContainsKey("Lidarr_TrackFile_MediaInfo_AudioChannels").Should().BeFalse();
            capturedEnv.ContainsKey("Lidarr_TrackFile_MediaInfo_AudioCodec").Should().BeFalse();
        }

        [Test]
        public void should_handle_missing_file_track_info_gracefully()
        {
            // Given
            _localTrack.FileTrackInfo = null;

            var processOutput = new ProcessOutput
            {
                ExitCode = 3,
                Lines = new List<ProcessOutputLine>()
            };

            StringDictionary capturedEnv = null;
            Mocker.GetMock<IProcessProvider>()
                .Setup(p => p.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StringDictionary>()))
                .Callback<string, string, StringDictionary>((script, args, env) => capturedEnv = env)
                .Returns(processOutput);

            // When
            var result = Subject.TryImport("/source/path", "/dest/path", _localTrack, _trackFile, TransferMode.Move);

            // Then
            result.Should().Be(ScriptImportDecision.DeferMove);
            capturedEnv.ContainsKey("Lidarr_TrackFile_MediaInfo_AudioChannels").Should().BeFalse();
        }
    }
}
