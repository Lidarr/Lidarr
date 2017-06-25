﻿using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.MediaFiles.TrackImport
{
    public interface IImportApprovedTracks
    {
        List<ImportResult> Import(List<ImportDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto);
    }

    public class ImportApprovedTracks : IImportApprovedTracks
    {
        private readonly IUpgradeMediaFiles _trackFileUpgrader;
        private readonly IMediaFileService _mediaFileService;
        //private readonly IExtraService _extraService;
        private readonly IDiskProvider _diskProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ImportApprovedTracks(IUpgradeMediaFiles episodeFileUpgrader,
                                      IMediaFileService mediaFileService,
                                      //IExtraService extraService,
                                      IDiskProvider diskProvider,
                                      IEventAggregator eventAggregator,
                                      Logger logger)
        {
            _trackFileUpgrader = episodeFileUpgrader;
            _mediaFileService = mediaFileService;
           // _extraService = extraService;
            _diskProvider = diskProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public List<ImportResult> Import(List<ImportDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto)
        {
            var qualifiedImports = decisions.Where(c => c.Approved)
               .GroupBy(c => c.LocalTrack.Artist.Id, (i, s) => s
                   .OrderByDescending(c => c.LocalTrack.Quality, new QualityModelComparer(s.First().LocalTrack.Artist.Profile))
                   .ThenByDescending(c => c.LocalTrack.Size))
               .SelectMany(c => c)
               .ToList();

            var importResults = new List<ImportResult>();

            foreach (var importDecision in qualifiedImports.OrderBy(e => e.LocalTrack.Tracks.Select(track => track.TrackNumber).MinOrDefault())
                                                           .ThenByDescending(e => e.LocalTrack.Size))
            {
                var localTrack = importDecision.LocalTrack;
                var oldFiles = new List<TrackFile>();

                try
                {
                    //check if already imported
                    if (importResults.SelectMany(r => r.ImportDecision.LocalTrack.Tracks)
                                         .Select(e => e.Id)
                                         .Intersect(localTrack.Tracks.Select(e => e.Id))
                                         .Any())
                    {
                        importResults.Add(new ImportResult(importDecision, "Track has already been imported"));
                        continue;
                    }

                    var trackFile = new TrackFile();
                    trackFile.DateAdded = DateTime.UtcNow;
                    trackFile.ArtistId = localTrack.Artist.Id;
                    trackFile.Path = localTrack.Path.CleanFilePath();
                    trackFile.Size = _diskProvider.GetFileSize(localTrack.Path);
                    trackFile.Quality = localTrack.Quality;
                    trackFile.MediaInfo = localTrack.MediaInfo;
                    trackFile.AlbumId = localTrack.Album.Id;
                    trackFile.Tracks = localTrack.Tracks;
                    trackFile.ReleaseGroup = localTrack.ParsedTrackInfo.ReleaseGroup;

                    bool copyOnly;
                    switch (importMode)
                    {
                        default:
                        case ImportMode.Auto:
                            copyOnly = downloadClientItem != null && downloadClientItem.IsReadOnly;
                            break;
                        case ImportMode.Move:
                            copyOnly = false;
                            break;
                        case ImportMode.Copy:
                            copyOnly = true;
                            break;
                    }

                    if (newDownload)
                    {
                        //trackFile.SceneName = GetSceneName(downloadClientItem, localTrack);

                        var moveResult = _trackFileUpgrader.UpgradeTrackFile(trackFile, localTrack, copyOnly);
                        oldFiles = moveResult.OldFiles;
                    }
                    else
                    {
                        trackFile.RelativePath = localTrack.Artist.Path.GetRelativePath(trackFile.Path);
                    }

                    _mediaFileService.Add(trackFile);
                    importResults.Add(new ImportResult(importDecision));

                    //if (newDownload)
                    //{
                    //    _extraService.ImportExtraFiles(localTrack, trackFile, copyOnly); // TODO: Import Music Extras
                    //}

                    if (downloadClientItem != null)
                    {
                        _eventAggregator.PublishEvent(new TrackImportedEvent(localTrack, trackFile, newDownload, downloadClientItem.DownloadClient, downloadClientItem.DownloadId, downloadClientItem.IsReadOnly));
                    }
                    else
                    {
                        _eventAggregator.PublishEvent(new TrackImportedEvent(localTrack, trackFile, newDownload));
                    }

                    if (newDownload)
                    {
                        _eventAggregator.PublishEvent(new TrackDownloadedEvent(localTrack, trackFile, oldFiles));
                    }
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Couldn't import track " + localTrack);
                    importResults.Add(new ImportResult(importDecision, "Failed to import episode"));
                }
            }

            //Adding all the rejected decisions
            importResults.AddRange(decisions.Where(c => !c.Approved)
                                            .Select(d => new ImportResult(d, d.Rejections.Select(r => r.Reason).ToArray())));

            return importResults;
        }

        //private string GetSceneName(DownloadClientItem downloadClientItem, LocalEpisode localEpisode)
        //{
        //    if (downloadClientItem != null)
        //    {
        //        var title = Parser.Parser.RemoveFileExtension(downloadClientItem.Title);

        //        var parsedTitle = Parser.Parser.ParseTitle(title);

        //        if (parsedTitle != null && !parsedTitle.FullSeason)
        //        {
        //            return title;
        //        }
        //    }

        //    var fileName = Path.GetFileNameWithoutExtension(localEpisode.Path.CleanFilePath());

        //    if (SceneChecker.IsSceneTitle(fileName))
        //    {
        //        return fileName;
        //    }

        //    return null;
        //}
    }
}
