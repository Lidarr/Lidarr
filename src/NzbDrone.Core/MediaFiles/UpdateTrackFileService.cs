using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpdateTrackFileService
    {
        void ChangeFileDateForFile(TrackFile trackFile, Artist artist, List<Track> tracks);
    }

    public class UpdateTrackFileService : IUpdateTrackFileService,
                                            IHandle<ArtistScannedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IAlbumService _albumService;
        private readonly IConfigService _configService;
        private readonly ITrackService _trackService;
        private readonly Logger _logger;

        public UpdateTrackFileService(IDiskProvider diskProvider,
                                        IConfigService configService,
                                        ITrackService trackService,
                                        IAlbumService albumService,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _trackService = trackService;
            _albumService = albumService;
            _logger = logger;
        }

        public void ChangeFileDateForFile(TrackFile trackFile, Artist artist, List<Track> tracks)
        {
            ChangeFileDate(trackFile, artist, tracks);
        }

        private bool ChangeFileDate(TrackFile trackFile, Artist artist, List<Track> tracks)
        {
            var trackFilePath = Path.Combine(artist.Path, trackFile.RelativePath);

            switch (_configService.FileDate)
            {
                case FileDateType.AlbumReleaseDate:
                    {
                        var relDate = _albumService.GetAlbum(trackFile.AlbumId).ReleaseDate.ToString();

                        if (relDate.IsNullOrWhiteSpace())
                        {
                            return false;
                        }

                        return ChangeFileDateToLocalAirDate(trackFilePath, relDate);
                    }
            }

            return false;
        }

        public void Handle(ArtistScannedEvent message)
        {
            if (_configService.FileDate == FileDateType.None)
            {
                return;
            }

            var episodes = _trackService.TracksWithFiles(message.Artist.Id);

            var trackFiles = new List<TrackFile>();
            var updated = new List<TrackFile>();

            foreach (var group in episodes.GroupBy(e => e.TrackFileId))
            {
                var tracksInFile = group.Select(e => e).ToList();
                var trackFile = tracksInFile.First().TrackFile;

                trackFiles.Add(trackFile);

                if (ChangeFileDate(trackFile, message.Artist, tracksInFile))
                {
                    updated.Add(trackFile);
                }
            }

            if (updated.Any())
            {
                _logger.ProgressDebug("Changed file date for {0} files of {1} in {2}", updated.Count, trackFiles.Count, message.Artist.Name);
            }

            else
            {
                _logger.ProgressDebug("No file dates changed for {0}", message.Artist.Name);
            }
        }

        private bool ChangeFileDateToLocalAirDate(string filePath, string fileDate)
        {
            DateTime airDate;

            if (DateTime.TryParse(fileDate, out airDate))
            {
                // avoiding false +ve checks and set date skewing by not using UTC (Windows)
                DateTime oldDateTime = _diskProvider.FileGetLastWrite(filePath);

                if (!DateTime.Equals(airDate, oldDateTime))
                {
                    try
                    {
                        _diskProvider.FileSetLastWriteTime(filePath, airDate);
                        _logger.Debug("Date of file [{0}] changed from '{1}' to '{2}'", filePath, oldDateTime, airDate);

                        return true;
                    }

                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Unable to set date of file [" + filePath + "]");
                    }
                }
            }

            else
            {
                _logger.Debug("Could not create valid date to change file [{0}]", filePath);
            }

            return false;
        }
    }
}
