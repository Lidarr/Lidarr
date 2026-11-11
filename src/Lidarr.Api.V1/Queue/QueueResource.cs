using System;
using System.Collections.Generic;
using System.Linq;
using Lidarr.Api.V1.Albums;
using Lidarr.Api.V1.Artist;
using Lidarr.Api.V1.CustomFormats;
using Lidarr.Http.REST;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace Lidarr.Api.V1.Queue
{
    public class QueueResource : RestResource
    {
        public int? ArtistId { get; set; }
        public int? AlbumId { get; set; }
        public ArtistResource Artist { get; set; }
        public AlbumResource Album { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public decimal Size { get; set; }
        public string Title { get; set; }
        public decimal Sizeleft { get; set; }
        public TimeSpan? Timeleft { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public DateTime? Added { get; set; }
        public string Status { get; set; }
        public TrackedDownloadStatus? TrackedDownloadStatus { get; set; }
        public TrackedDownloadState? TrackedDownloadState { get; set; }
        public List<TrackedDownloadStatusMessage> StatusMessages { get; set; }
        public string ErrorMessage { get; set; }
        public string DownloadId { get; set; }
        public string Protocol { get; set; }
        public string DownloadClient { get; set; }
        public bool DownloadClientHasPostImportCategory { get; set; }
        public string Indexer { get; set; }
        public string OutputPath { get; set; }
        public int TrackFileCount { get; set; }
        public int TrackHasFileCount { get; set; }
        public bool DownloadForced { get; set; }
        public List<QueueTargetTrackResource> TargetTracks { get; set; }
    }

    public class QueueTargetTrackResource
    {
        public string ForeignRecordingId { get; set; }
        public string Title { get; set; }
    }

    public static class QueueResourceMapper
    {
        public static QueueResource ToResource(this NzbDrone.Core.Queue.Queue model, bool includeArtist, bool includeAlbum)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = model.RemoteAlbum?.CustomFormats;
            var customFormatScore = model.Artist?.QualityProfile?.Value?.CalculateCustomFormatScore(customFormats) ?? 0;

            var albumRelease = model.Album?.AlbumReleases?.Value?.SingleOrDefault(x => x.Monitored);

            return new QueueResource
            {
                Id = model.Id,
                ArtistId = model.Artist?.Id,
                AlbumId = model.Album?.Id,
                Artist = includeArtist && model.Artist != null ? model.Artist.ToResource() : null,
                Album = includeAlbum && model.Album != null ? model.Album.ToResource() : null,
                Quality = model.Quality,
                CustomFormats = customFormats?.ToResource(false),
                CustomFormatScore = customFormatScore,
                Size = model.Size,
                Title = model.Title,
                Sizeleft = model.Sizeleft,
                Timeleft = model.Timeleft,
                EstimatedCompletionTime = model.EstimatedCompletionTime,
                Added = model.Added,
                Status = model.Status.FirstCharToLower(),
                TrackedDownloadStatus = model.TrackedDownloadStatus,
                TrackedDownloadState = model.TrackedDownloadState,
                StatusMessages = model.StatusMessages,
                ErrorMessage = model.ErrorMessage,
                DownloadId = model.DownloadId,
                Protocol = model.Protocol,
                DownloadClient = model.DownloadClient,
                DownloadClientHasPostImportCategory = model.DownloadClientHasPostImportCategory,
                Indexer = model.Indexer,
                OutputPath = model.OutputPath,
                TrackFileCount = albumRelease?.Tracks?.Value?.Count ?? 0,
                TrackHasFileCount = albumRelease?.Tracks?.Value?.Count(x => x.HasFile) ?? 0,
                DownloadForced = model.DownloadForced,
                TargetTracks = GetTargetTracks(model)
            };
        }

        private static List<QueueTargetTrackResource> GetTargetTracks(NzbDrone.Core.Queue.Queue model)
        {
            // Use the targets saved with the grab, not today's monitoring selections.
            var recordingIds = model.RemoteAlbum?.TargetRecordingIds?.Where(id => id.IsNotNullOrWhiteSpace()).Distinct().ToList() ?? new List<string>();
            if (recordingIds.Count == 0)
            {
                return new List<QueueTargetTrackResource>();
            }

            var titles = new Dictionary<string, string>();
            var albums = new[] { model.Album }
                .Concat(model.RemoteAlbum?.Albums ?? new List<Album>())
                .Where(album => album != null)
                .Distinct();

            foreach (var release in albums.SelectMany(album => album.AlbumReleases?.Value ?? new List<AlbumRelease>())
                         .OrderByDescending(release => release.Monitored))
            {
                foreach (var track in release.Tracks?.Value ?? new List<Track>())
                {
                    if (recordingIds.Contains(track.ForeignRecordingId) && track.Title.IsNotNullOrWhiteSpace())
                    {
                        titles.TryAdd(track.ForeignRecordingId, track.Title);
                    }
                }

                if (titles.Count == recordingIds.Count)
                {
                    break;
                }
            }

            // Retain unresolved targets so the UI never silently hides wanted songs.
            return recordingIds.Select(id => new QueueTargetTrackResource
            {
                ForeignRecordingId = id,
                Title = titles.GetValueOrDefault(id)
            }).ToList();
        }

        public static List<QueueResource> ToResource(this IEnumerable<NzbDrone.Core.Queue.Queue> models, bool includeArtist, bool includeAlbum)
        {
            return models.Select((m) => ToResource(m, includeArtist, includeAlbum)).ToList();
        }
    }
}
