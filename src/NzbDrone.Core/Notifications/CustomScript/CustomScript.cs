using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public CustomScript(IDiskProvider diskProvider, IProcessProvider processProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _logger = logger;
        }

        public override string Name => "Custom Script";

        public override string Link => "https://github.com/Lidarr/Lidarr/wiki/Custom-Post-Processing-Scripts";

        public override void OnGrab(GrabMessage message)
        {
            var series = message.Series;
            var remoteEpisode = message.Episode;
            var releaseGroup = remoteEpisode.ParsedEpisodeInfo.ReleaseGroup;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Lidarr_EventType", "Grab");
            environmentVariables.Add("Lidarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Lidarr_Series_Title", series.Title);
            environmentVariables.Add("Lidarr_Series_TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Lidarr_Series_Type", series.SeriesType.ToString());
            environmentVariables.Add("Lidarr_Release_EpisodeCount", remoteEpisode.Episodes.Count.ToString());
            environmentVariables.Add("Lidarr_Release_SeasonNumber", remoteEpisode.ParsedEpisodeInfo.SeasonNumber.ToString());
            environmentVariables.Add("Lidarr_Release_EpisodeNumbers", string.Join(",", remoteEpisode.Episodes.Select(e => e.EpisodeNumber)));
            environmentVariables.Add("Lidarr_Release_EpisodeAirDates", string.Join(",", remoteEpisode.Episodes.Select(e => e.AirDate)));
            environmentVariables.Add("Lidarr_Release_EpisodeAirDatesUtc", string.Join(",", remoteEpisode.Episodes.Select(e => e.AirDateUtc)));
            environmentVariables.Add("Lidarr_Release_EpisodeTitles", string.Join("|", remoteEpisode.Episodes.Select(e => e.Title)));
            environmentVariables.Add("Lidarr_Release_Title", remoteEpisode.Release.Title);
            environmentVariables.Add("Lidarr_Release_Indexer", remoteEpisode.Release.Indexer);
            environmentVariables.Add("Lidarr_Release_Size", remoteEpisode.Release.Size.ToString());
            environmentVariables.Add("Lidarr_Release_Quality", remoteEpisode.ParsedEpisodeInfo.Quality.Quality.Name);
            environmentVariables.Add("Lidarr_Release_QualityVersion", remoteEpisode.ParsedEpisodeInfo.Quality.Revision.Version.ToString());
            environmentVariables.Add("Lidarr_Release_ReleaseGroup", releaseGroup);

            ExecuteScript(environmentVariables);
        }

        public override void OnDownload(DownloadMessage message)
        {
            var series = message.Series;
            var episodeFile = message.EpisodeFile;
            var sourcePath = message.SourcePath;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Lidarr_EventType", "Download");
            environmentVariables.Add("LIdarr_IsUpgrade", message.OldFiles.Any().ToString());
            environmentVariables.Add("Lidarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Lidarr_Series_Title", series.Title);
            environmentVariables.Add("Lidarr_Series_Path", series.Path);
            environmentVariables.Add("Lidarr_Series_TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Lidarr_Series_Type", series.SeriesType.ToString());
            environmentVariables.Add("Lidarr_EpisodeFile_Id", episodeFile.Id.ToString());
            environmentVariables.Add("Lidarr_EpisodeFile_EpisodeCount", episodeFile.Episodes.Value.Count.ToString());
            environmentVariables.Add("Lidarr_EpisodeFile_RelativePath", episodeFile.RelativePath);
            environmentVariables.Add("Lidarr_EpisodeFile_Path", Path.Combine(series.Path, episodeFile.RelativePath));
            environmentVariables.Add("Lidarr_EpisodeFile_SeasonNumber", episodeFile.SeasonNumber.ToString());
            environmentVariables.Add("Lidarr_EpisodeFile_EpisodeNumbers", string.Join(",", episodeFile.Episodes.Value.Select(e => e.EpisodeNumber)));
            environmentVariables.Add("Lidarr_EpisodeFile_EpisodeAirDates", string.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDate)));
            environmentVariables.Add("Lidarr_EpisodeFile_EpisodeAirDatesUtc", string.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDateUtc)));
            environmentVariables.Add("Lidarr_EpisodeFile_EpisodeTitles", string.Join("|", episodeFile.Episodes.Value.Select(e => e.Title)));
            environmentVariables.Add("Lidarr_EpisodeFile_Quality", episodeFile.Quality.Quality.Name);
            environmentVariables.Add("Lidarr_EpisodeFile_QualityVersion", episodeFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Lidarr_EpisodeFile_ReleaseGroup", episodeFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Lidarr_EpisodeFile_SceneName", episodeFile.SceneName ?? string.Empty);
            environmentVariables.Add("Lidarr_EpisodeFile_SourcePath", sourcePath);
            environmentVariables.Add("Lidarr_EpisodeFile_SourceFolder", Path.GetDirectoryName(sourcePath));

            if (message.OldFiles.Any())
            {
                environmentVariables.Add("Lidarr_DeletedRelativePaths", string.Join("|", message.OldFiles.Select(e => e.RelativePath)));
                environmentVariables.Add("Lidarr_DeletedPaths", string.Join("|", message.OldFiles.Select(e => Path.Combine(series.Path, e.RelativePath))));
            }

            ExecuteScript(environmentVariables);
        }

        public override void OnRename(Series series)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Lidarr_EventType", "Rename");
            environmentVariables.Add("Lidarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Lidarr_Series_Title", series.Title);
            environmentVariables.Add("Lidarr_Series_Path", series.Path);
            environmentVariables.Add("Lidarr_Series_TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Lidarr_Series_Type", series.SeriesType.ToString());

            ExecuteScript(environmentVariables);
        }


        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            if (!_diskProvider.FileExists(Settings.Path))
            {
                failures.Add(new NzbDroneValidationFailure("Path", "File does not exist"));
            }

            return new ValidationResult(failures);
        }

        private void ExecuteScript(StringDictionary environmentVariables)
        {
            _logger.Debug("Executing external script: {0}", Settings.Path);

            var process = _processProvider.StartAndCapture(Settings.Path, Settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", Settings.Path, process.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", process.Lines));
        }
    }
}
