using System.Collections.Specialized;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.MediaFiles
{
    public interface IImportScript
    {
        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalTrack localTrack, TrackFile trackFile, TransferMode mode, DownloadClientItem downloadClientItem = null);
    }

    public class ImportScriptService : IImportScript
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAudioTagService _audioTagService;
        private readonly IProcessProvider _processProvider;
        private readonly IConfigService _configService;
        private readonly ITagRepository _tagRepository;
        private readonly ICustomFormatCalculationService _customFormatCalculationService;
        private readonly Logger _logger;

        public ImportScriptService(IProcessProvider processProvider,
                                   IAudioTagService audioTagService,
                                   IConfigService configService,
                                   IConfigFileProvider configFileProvider,
                                   ITagRepository tagRepository,
                                   ICustomFormatCalculationService customFormatCalculationService,
                                   Logger logger)
        {
            _processProvider = processProvider;
            _audioTagService = audioTagService;
            _configService = configService;
            _configFileProvider = configFileProvider;
            _tagRepository = tagRepository;
            _customFormatCalculationService = customFormatCalculationService;
            _logger = logger;
        }

        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalTrack localTrack, TrackFile trackFile, TransferMode mode, DownloadClientItem downloadClientItem = null)
        {
            var artist = localTrack.Artist;
            var album = localTrack.Album;
            var downloadClientInfo = downloadClientItem?.DownloadClientInfo;
            var downloadId = downloadClientItem?.DownloadId;

            if (!_configService.UseScriptImport)
            {
                return ScriptImportDecision.DeferMove;
            }

            var environmentVariables = new StringDictionary
            {
                { "Lidarr_SourcePath", sourcePath },
                { "Lidarr_DestinationPath", destinationFilePath },
                { "Lidarr_InstanceName", _configFileProvider.InstanceName },
                { "Lidarr_ApplicationUrl", _configService.ApplicationUrl },
                { "Lidarr_TransferMode", mode.ToString() },
                { "Lidarr_Artist_Id", artist.Id.ToString() },
                { "Lidarr_Artist_Name", artist.Name },
                { "Lidarr_Artist_Path", artist.Path },
                { "Lidarr_Artist_MBId", artist.ForeignArtistId },
                { "Lidarr_Artist_Tags", string.Join("|", artist.Tags.Select(t => _tagRepository.Get(t).Label)) },
                { "Lidarr_Album_Id", album.Id.ToString() },
                { "Lidarr_Album_Title", album.Title },
                { "Lidarr_Album_MBId", album.ForeignAlbumId },
                { "Lidarr_Album_ReleaseDate", album.ReleaseDate?.ToString("yyyy-MM-dd") ?? string.Empty },
                { "Lidarr_Album_Genres", string.Join("|", album.Genres) },
                { "Lidarr_TrackFile_TrackCount", localTrack.Tracks.Count.ToString() },
                { "Lidarr_TrackFile_TrackIds", string.Join(",", localTrack.Tracks.Select(t => t.Id)) },
                { "Lidarr_TrackFile_TrackNumbers", string.Join(",", localTrack.Tracks.Select(t => t.TrackNumber)) },
                { "Lidarr_TrackFile_TrackTitles", string.Join("|", localTrack.Tracks.Select(t => t.Title)) },
                { "Lidarr_TrackFile_Quality", localTrack.Quality.Quality.Name },
                { "Lidarr_TrackFile_QualityVersion", localTrack.Quality.Revision.Version.ToString() },
                { "Lidarr_TrackFile_ReleaseGroup", localTrack.ReleaseGroup ?? string.Empty },
                { "Lidarr_TrackFile_SceneName", localTrack.SceneName ?? string.Empty },
                { "Lidarr_Download_Client", downloadClientInfo?.Name ?? string.Empty },
                { "Lidarr_Download_Client_Type", downloadClientInfo?.Type ?? string.Empty },
                { "Lidarr_Download_Id", downloadId ?? string.Empty }
            };

            // Audio-specific MediaInfo (no video properties for music files)
            if (localTrack.FileTrackInfo?.MediaInfo != null)
            {
                var mediaInfo = localTrack.FileTrackInfo.MediaInfo;
                environmentVariables.Add("Lidarr_TrackFile_MediaInfo_AudioChannels", mediaInfo.AudioChannels.ToString());
                environmentVariables.Add("Lidarr_TrackFile_MediaInfo_AudioCodec", mediaInfo.AudioFormat ?? string.Empty);
                environmentVariables.Add("Lidarr_TrackFile_MediaInfo_AudioBitRate", mediaInfo.AudioBitrate.ToString());
                environmentVariables.Add("Lidarr_TrackFile_MediaInfo_AudioSampleRate", mediaInfo.AudioSampleRate.ToString());
                environmentVariables.Add("Lidarr_TrackFile_MediaInfo_BitsPerSample", mediaInfo.AudioBits.ToString());
            }

            // CustomFormats for music files
            var customFormats = _customFormatCalculationService.ParseCustomFormat(localTrack);
            environmentVariables.Add("Lidarr_TrackFile_CustomFormat", string.Join("|", customFormats.Select(x => x.Name)));

            _logger.Debug("Executing external script: {0}", _configService.ScriptImportPath);

            var processOutput = _processProvider.StartAndCapture(_configService.ScriptImportPath, $"\"{sourcePath}\" \"{destinationFilePath}\"", environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", _configService.ScriptImportPath, processOutput.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            switch (processOutput.ExitCode)
            {
                case 0: // Copy complete
                    return ScriptImportDecision.MoveComplete;
                case 2: // Copy complete, file potentially changed, should try renaming again
                    trackFile.MediaInfo = _audioTagService.ReadTags(destinationFilePath).MediaInfo;
                    trackFile.Path = null;
                    return ScriptImportDecision.RenameRequested;
                case 3: // Let Lidarr handle it
                    return ScriptImportDecision.DeferMove;
                default: // Error, fail to import
                    throw new ScriptImportException("Moving with script failed! Exit code {0}", processOutput.ExitCode);
            }
        }
    }
}
