using System.Collections.Generic;
using Lidarr.Http.REST;
using NzbDrone.Core.Statistics;

namespace Lidarr.Api.V1.Statistics
{
    public class StatisticsResource : RestResource
    {
        public int ArtistCount { get; set; }
        public int MonitoredArtistCount { get; set; }
        public int CompletedArtistCount { get; set; }
        public int ContinuingArtistCount { get; set; }
        public int EndedArtistCount { get; set; }
        public int DeletedArtistCount { get; set; }
        public int AlbumCount { get; set; }
        public int MonitoredAlbumCount { get; set; }
        public int CompletedAlbumCount { get; set; }
        public int MissingAlbumCount { get; set; }
        public int TotalTrackCount { get; set; }
        public int MonitoredTrackCount { get; set; }
        public int DownloadedTrackCount { get; set; }
        public int MissingTrackCount { get; set; }
        public int UnreleasedTrackCount { get; set; }
        public int TrackFileCount { get; set; }
        public long SizeOnDisk { get; set; }
        public List<QualityProfileStatisticsResource> QualityProfileStatistics { get; set; }
        public List<MetadataProfileStatisticsResource> MetadataProfileStatistics { get; set; }
        public List<QualityStatisticsResource> QualityStatistics { get; set; }
        public List<TagStatisticsResource> TagStatistics { get; set; }
        public List<AlbumTypeStatisticsResource> AlbumTypeStatistics { get; set; }
        public List<SecondaryTypeStatisticsResource> SecondaryTypeStatistics { get; set; }
        public List<DecadeStatisticsResource> DecadeStatistics { get; set; }
        public List<GenreStatisticsResource> GenreStatistics { get; set; }
        public List<LargestArtistStatisticsResource> LargestArtistStatistics { get; set; }
        public List<MediaFormatStatisticsResource> MediaFormatStatistics { get; set; }
    }

    public static class StatisticsResourceMapper
    {
        public static StatisticsResource MapToResource(this LibraryStatistics model)
        {
            return new StatisticsResource
            {
                ArtistCount = model.ArtistCount,
                MonitoredArtistCount = model.MonitoredArtistCount,
                CompletedArtistCount = model.CompletedArtistCount,
                ContinuingArtistCount = model.ContinuingArtistCount,
                EndedArtistCount = model.EndedArtistCount,
                DeletedArtistCount = model.DeletedArtistCount,
                AlbumCount = model.AlbumCount,
                MonitoredAlbumCount = model.MonitoredAlbumCount,
                CompletedAlbumCount = model.CompletedAlbumCount,
                MissingAlbumCount = model.MissingAlbumCount,
                TotalTrackCount = model.TotalTrackCount,
                MonitoredTrackCount = model.MonitoredTrackCount,
                DownloadedTrackCount = model.DownloadedTrackCount,
                MissingTrackCount = model.MissingTrackCount,
                UnreleasedTrackCount = model.UnreleasedTrackCount,
                TrackFileCount = model.TrackFileCount,
                SizeOnDisk = model.SizeOnDisk,
                QualityProfileStatistics = model.QualityProfileStatistics.ConvertAll(QualityProfileStatisticsResourceMapper.MapToResource),
                MetadataProfileStatistics = model.MetadataProfileStatistics.ConvertAll(MetadataProfileStatisticsResourceMapper.MapToResource),
                QualityStatistics = model.QualityStatistics.ConvertAll(QualityStatisticsResourceMapper.MapToResource),
                TagStatistics = model.TagStatistics.ConvertAll(TagStatisticsResourceMapper.MapToResource),
                AlbumTypeStatistics = model.AlbumTypeStatistics.ConvertAll(AlbumTypeStatisticsResourceMapper.MapToResource),
                SecondaryTypeStatistics = model.SecondaryTypeStatistics.ConvertAll(SecondaryTypeStatisticsResourceMapper.MapToResource),
                DecadeStatistics = model.DecadeStatistics.ConvertAll(DecadeStatisticsResourceMapper.MapToResource),
                GenreStatistics = model.GenreStatistics.ConvertAll(GenreStatisticsResourceMapper.MapToResource),
                LargestArtistStatistics = model.LargestArtistStatistics.ConvertAll(LargestArtistStatisticsResourceMapper.MapToResource),
                MediaFormatStatistics = model.MediaFormatStatistics.ConvertAll(MediaFormatStatisticsResourceMapper.MapToResource)
            };
        }
    }
}
