using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Lidarr.Http;
using Lidarr.Http.Extensions;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;

namespace Lidarr.Api.V1.Artist
{
    [V1ApiController]
    public class ArtistController : RestControllerWithSignalR<ArtistResource, NzbDrone.Core.Music.Artist>,
                                IHandle<AlbumImportedEvent>,
                                IHandle<AlbumEditedEvent>,
                                IHandle<AlbumDeletedEvent>,
                                IHandle<TrackFileDeletedEvent>,
                                IHandle<ArtistUpdatedEvent>,
                                IHandle<ArtistEditedEvent>,
                                IHandle<ArtistsDeletedEvent>,
                                IHandle<ArtistRenamedEvent>,
                                IHandle<MediaCoversUpdatedEvent>
    {
        private readonly IArtistService _artistService;
        private readonly IAlbumService _albumService;
        private readonly IAddArtistService _addArtistService;
        private readonly IArtistStatisticsService _artistStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;

        public ArtistController(IBroadcastSignalRMessage signalRBroadcaster,
                            IArtistService artistService,
                            IAlbumService albumService,
                            IAddArtistService addArtistService,
                            IArtistStatisticsService artistStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IManageCommandQueue commandQueueManager,
                            IRootFolderService rootFolderService,
                            RootFolderValidator rootFolderValidator,
                            MappedNetworkDriveValidator mappedNetworkDriveValidator,
                            ArtistPathValidator artistPathValidator,
                            ArtistExistsValidator artistExistsValidator,
                            ArtistAncestorValidator artistAncestorValidator,
                            SystemFolderValidator systemFolderValidator,
                            QualityProfileExistsValidator qualityProfileExistsValidator,
                            MetadataProfileExistsValidator metadataProfileExistsValidator)
            : base(signalRBroadcaster)
        {
            _artistService = artistService;
            _albumService = albumService;
            _addArtistService = addArtistService;
            _artistStatisticsService = artistStatisticsService;

            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.MetadataProfileId));

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(artistPathValidator)
                           .SetValidator(artistAncestorValidator)
                           .SetValidator(systemFolderValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            SharedValidator.RuleFor(s => s.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            SharedValidator.RuleFor(s => s.MetadataProfileId).SetValidator(metadataProfileExistsValidator);

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath).IsValidPath().When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.ArtistName).NotEmpty();
            PostValidator.RuleFor(s => s.ForeignArtistId).NotEmpty().SetValidator(artistExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();
        }

        public override ArtistResource GetResourceById(int id)
        {
            var artist = _artistService.GetArtist(id);
            return GetArtistResource(artist);
        }

        private ArtistResource GetArtistResource(NzbDrone.Core.Music.Artist artist)
        {
            if (artist == null)
            {
                return null;
            }

            var resource = artist.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkArtistStatistics(resource);
            LinkNextPreviousAlbums(resource);

            //PopulateAlternateTitles(resource);
            LinkRootFolderPath(resource);

            return resource;
        }

        [HttpGet]
        public List<ArtistResource> AllArtists(Guid? mbId)
        {
            var artistStats = _artistStatisticsService.ArtistStatistics();
            var artistsResources = new List<ArtistResource>();

            if (mbId.HasValue)
            {
                artistsResources.AddIfNotNull(_artistService.FindById(mbId.Value.ToString()).ToResource());
            }
            else
            {
                artistsResources.AddRange(_artistService.GetAllArtists().ToResource());
            }

            MapCoversToLocal(artistsResources.ToArray());
            LinkNextPreviousAlbums(artistsResources.ToArray());
            LinkArtistStatistics(artistsResources, artistStats);
            artistsResources.ForEach(LinkRootFolderPath);

            //PopulateAlternateTitles(seriesResources);
            return artistsResources;
        }

        [RestPostById]
        public ActionResult<ArtistResource> AddArtist(ArtistResource artistResource)
        {
            var artist = _addArtistService.AddArtist(artistResource.ToModel());

            return Created(artist.Id);
        }

        [RestPutById]
        public ActionResult<ArtistResource> UpdateArtist(ArtistResource artistResource)
        {
            var moveFiles = Request.GetBooleanQueryParameter("moveFiles");
            var artist = _artistService.GetArtist(artistResource.Id);

            var sourcePath = artist.Path;
            var destinationPath = artistResource.Path;

            _commandQueueManager.Push(new MoveArtistCommand
            {
                ArtistId = artist.Id,
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                MoveFiles = moveFiles,
                Trigger = CommandTrigger.Manual
            });

            var model = artistResource.ToModel(artist);

            _artistService.UpdateArtist(model);

            BroadcastResourceChange(ModelAction.Updated, artistResource);

            return Accepted(artistResource.Id);
        }

        [RestDeleteById]
        public void DeleteArtist(int id)
        {
            var deleteFiles = Request.GetBooleanQueryParameter("deleteFiles");
            var addImportListExclusion = Request.GetBooleanQueryParameter("addImportListExclusion");

            _artistService.DeleteArtist(id, deleteFiles, addImportListExclusion);
        }

        private void MapCoversToLocal(params ArtistResource[] artists)
        {
            foreach (var artistResource in artists)
            {
                _coverMapper.ConvertToLocalUrls(artistResource.Id, MediaCoverEntity.Artist, artistResource.Images);
            }
        }

        private void LinkNextPreviousAlbums(params ArtistResource[] artists)
        {
            var nextAlbums = _albumService.GetNextAlbumsByArtistMetadataId(artists.Select(x => x.ArtistMetadataId));
            var lastAlbums = _albumService.GetLastAlbumsByArtistMetadataId(artists.Select(x => x.ArtistMetadataId));

            foreach (var artistResource in artists)
            {
                artistResource.NextAlbum = nextAlbums.FirstOrDefault(x => x.ArtistMetadataId == artistResource.ArtistMetadataId);
                artistResource.LastAlbum = lastAlbums.FirstOrDefault(x => x.ArtistMetadataId == artistResource.ArtistMetadataId);
            }
        }

        private void FetchAndLinkArtistStatistics(ArtistResource resource)
        {
            LinkArtistStatistics(resource, _artistStatisticsService.ArtistStatistics(resource.Id));
        }

        private void LinkArtistStatistics(List<ArtistResource> resources, List<ArtistStatistics> artistStatistics)
        {
            foreach (var artist in resources)
            {
                var stats = artistStatistics.SingleOrDefault(ss => ss.ArtistId == artist.Id);
                if (stats == null)
                {
                    continue;
                }

                LinkArtistStatistics(artist, stats);
            }
        }

        private void LinkArtistStatistics(ArtistResource resource, ArtistStatistics artistStatistics)
        {
            resource.Statistics = artistStatistics.ToResource();
        }

        //private void PopulateAlternateTitles(List<ArtistResource> resources)
        //{
        //    foreach (var resource in resources)
        //    {
        //        PopulateAlternateTitles(resource);
        //    }
        //}

        //private void PopulateAlternateTitles(ArtistResource resource)
        //{
        //    var mappings = _sceneMappingService.FindByTvdbId(resource.TvdbId);

        //    if (mappings == null) return;

        //    resource.AlternateTitles = mappings.Select(v => new AlternateTitleResource { Title = v.Title, SeasonNumber = v.SeasonNumber, SceneSeasonNumber = v.SceneSeasonNumber }).ToList();
        //}
        private void LinkRootFolderPath(ArtistResource resource)
        {
            resource.RootFolderPath = _rootFolderService.GetBestRootFolderPath(resource.Path);
        }

        [NonAction]
        public void Handle(AlbumImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Artist));
        }

        [NonAction]
        public void Handle(AlbumEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Album.Artist.Value));
        }

        [NonAction]
        public void Handle(AlbumDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Album.Artist.Value));
        }

        [NonAction]
        public void Handle(TrackFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.TrackFile.Artist.Value));
        }

        [NonAction]
        public void Handle(ArtistUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Artist));
        }

        [NonAction]
        public void Handle(ArtistEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Artist));
        }

        [NonAction]
        public void Handle(ArtistsDeletedEvent message)
        {
            foreach (var artist in message.Artists)
            {
                BroadcastResourceChange(ModelAction.Deleted, artist.ToResource());
            }
        }

        [NonAction]
        public void Handle(ArtistRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Artist.Id);
        }

        [NonAction]
        public void Handle(MediaCoversUpdatedEvent message)
        {
            if (message.Updated)
            {
                BroadcastResourceChange(ModelAction.Updated, GetArtistResource(message.Artist));
            }
        }
    }
}
