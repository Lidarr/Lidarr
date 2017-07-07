﻿using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Api.REST;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.SignalR;
using System;

namespace NzbDrone.Api.EpisodeFiles
{
    public class EpisodeFileModule : NzbDroneRestModuleWithSignalR<EpisodeFileResource, EpisodeFile>,
                                 IHandle<EpisodeFileAddedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly ISeriesService _seriesService;
        private readonly IQualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly Logger _logger;

        public EpisodeFileModule(IBroadcastSignalRMessage signalRBroadcaster,
                             IMediaFileService mediaFileService,
                             IDiskProvider diskProvider,
                             IRecycleBinProvider recycleBinProvider,
                             ISeriesService seriesService,
                             IQualityUpgradableSpecification qualityUpgradableSpecification,
                             Logger logger)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _seriesService = seriesService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _logger = logger;
            GetResourceById = GetEpisodeFile;
            GetResourceAll = GetEpisodeFiles;
            UpdateResource = SetQuality;
            DeleteResource = DeleteEpisodeFile;
        }

        private EpisodeFileResource GetEpisodeFile(int id)
        {
            throw new NotImplementedException();
            //var episodeFile = _mediaFileService.Get(id);
            //var series = _seriesService.GetSeries(episodeFile.SeriesId);

            //return episodeFile.ToResource(series, _qualityUpgradableSpecification);
        }

        private List<EpisodeFileResource> GetEpisodeFiles()
        {
            throw new NotImplementedException();
            //if (!Request.Query.SeriesId.HasValue)
            //{
            //    throw new BadRequestException("seriesId is missing");
            //}

            //var seriesId = (int)Request.Query.SeriesId;

            //var series = _seriesService.GetSeries(seriesId);

            //return _mediaFileService.GetFilesBySeries(seriesId).ConvertAll(f => f.ToResource(series, _qualityUpgradableSpecification));
        }

        private void SetQuality(EpisodeFileResource episodeFileResource)
        {
            var episodeFile = _mediaFileService.Get(episodeFileResource.Id);
            episodeFile.Quality = episodeFileResource.Quality;
            _mediaFileService.Update(episodeFile);
        }

        private void DeleteEpisodeFile(int id)
        {
            throw new NotImplementedException();
            //var episodeFile = _mediaFileService.Get(id);
            //var series = _seriesService.GetSeries(episodeFile.SeriesId);
            //var fullPath = Path.Combine(series.Path, episodeFile.RelativePath);
            //var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));

            //_logger.Info("Deleting episode file: {0}", fullPath);
            //_recycleBinProvider.DeleteFile(fullPath, subfolder);
            //_mediaFileService.Delete(episodeFile, DeleteMediaFileReason.Manual);
        }

        public void Handle(EpisodeFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.EpisodeFile.Id);
        }
    }
}