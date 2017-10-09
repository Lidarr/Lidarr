using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileRepository<TExtraFile> : IBasicRepository<TExtraFile> where TExtraFile : ExtraFile, new()
    {
        void DeleteForSeries(int artistId);
        void DeleteForSeason(int seriesId, int seasonNumber);
        void DeleteForEpisodeFile(int episodeFileId);
        List<TExtraFile> GetFilesBySeries(int seriesId);
        List<TExtraFile> GetFilesBySeason(int seriesId, int seasonNumber);
        List<TExtraFile> GetFilesByEpisodeFile(int episodeFileId);
        TExtraFile FindByPath(string path);
    }

    public class ExtraFileRepository<TExtraFile> : BasicRepository<TExtraFile>, IExtraFileRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        public ExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteForSeries(int artistId)
        {
            Delete(c => c.ArtistId == artistId);
        }

        public void DeleteForSeason(int seriesId, int seasonNumber)
        {
            Delete(c => c.ArtistId == seriesId && c.AlbumId == seasonNumber);
        }

        public void DeleteForEpisodeFile(int episodeFileId)
        {
            Delete(c => c.TrackFileId == episodeFileId);
        }

        public List<TExtraFile> GetFilesBySeries(int seriesId)
        {
            return Query.Where(c => c.ArtistId == seriesId);
        }

        public List<TExtraFile> GetFilesBySeason(int seriesId, int seasonNumber)
        {
            return Query.Where(c => c.ArtistId == seriesId && c.AlbumId == seasonNumber);
        }

        public List<TExtraFile> GetFilesByEpisodeFile(int episodeFileId)
        {
            return Query.Where(c => c.TrackFileId == episodeFileId);
        }

        public TExtraFile FindByPath(string path)
        {
            return Query.Where(c => c.RelativePath == path).SingleOrDefault();
        }
    }
}
