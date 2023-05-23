using System.Collections.Specialized;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IImportScript
    {
        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalTrack localTrack, TrackFile trackFile, TransferMode mode);
    }

    public class ImportScriptService : IImportScript
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IProcessProvider _processProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ImportScriptService(IProcessProvider processProvider,
                                   IConfigService configService,
                                   IConfigFileProvider configFileProvider,
                                   Logger logger)
        {
            _processProvider = processProvider;
            _configService = configService;
            _configFileProvider = configFileProvider;
            _logger = logger;
        }

        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalTrack localTrack, TrackFile trackFile, TransferMode mode)
        {
            var artist = localTrack.Artist;
            var oldFiles = localTrack.OldFiles;
            var downloadClientInfo = localTrack.DownloadItem?.DownloadClientInfo;
            var downloadId = localTrack.DownloadItem?.DownloadId;

            if (!_configService.UseScriptImport)
            {
                return ScriptImportDecision.DeferMove;
            }

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Lidarr_SourcePath", sourcePath);
            environmentVariables.Add("Lidarr_DestinationPath", destinationFilePath);

            environmentVariables.Add("Lidarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Lidarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Lidarr_TransferMode", mode.ToString());

            environmentVariables.Add("Lidarr_Artist_Id", artist.Id.ToString());
            environmentVariables.Add("Lidarr_Artist_Name", artist.Metadata.Value.Name);
            environmentVariables.Add("Lidarr_Artist_Path", artist.Path);
            environmentVariables.Add("Lidarr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
            environmentVariables.Add("Lidarr_Artist_Type", artist.Metadata.Value.Type);

            environmentVariables.Add("Lidarr_Album_Id", localTrack.Album.Id.ToString());
            environmentVariables.Add("Lidarr_Album_Title", localTrack.Album.Title);
            environmentVariables.Add("Lidarr_Album_Overview", localTrack.Album.Overview);
            environmentVariables.Add("Lidarr_Album_MBId", localTrack.Album.ForeignAlbumId);
            environmentVariables.Add("Lidarr_AlbumRelease_MBId", localTrack.Release.ForeignReleaseId);
            environmentVariables.Add("Lidarr_Album_ReleaseDate", localTrack.Release.ReleaseDate.ToString());

            environmentVariables.Add("Lidarr_Download_Client", downloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Lidarr_Download_Client_Type", downloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Lidarr_Download_Id", downloadId ?? string.Empty);

            if (oldFiles.Any())
            {
                environmentVariables.Add("Lidarr_DeletedPaths", string.Join("|", oldFiles.Select(e => e.Path)));
                environmentVariables.Add("Lidarr_DeletedDateAdded", string.Join("|", oldFiles.Select(e => e.DateAdded)));
            }

            _logger.Debug("Executing external script: {0}", _configService.ScriptImportPath);

            var processOutput = _processProvider.StartAndCapture(_configService.ScriptImportPath, $"\"{sourcePath}\" \"{destinationFilePath}\"", environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", _configService.ScriptImportPath, processOutput.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            switch (processOutput.ExitCode)
            {
                case 0: // Copy complete
                    return ScriptImportDecision.MoveComplete;
                case 2: // Copy complete, file potentially changed, should try renaming again
                    // trackFile.MediaInfo = _videoFileInfoReader.GetMediaInfo(destinationFilePath);
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
