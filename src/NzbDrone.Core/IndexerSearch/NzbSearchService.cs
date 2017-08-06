﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;
using System.Linq;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForNzb
    {
        List<DownloadDecision> EpisodeSearch(int episodeId, bool userInvokedSearch);
        List<DownloadDecision> EpisodeSearch(Episode episode, bool userInvokedSearch);
        List<DownloadDecision> SeasonSearch(int seriesId, int seasonNumber, bool missingOnly, bool userInvokedSearch);
        List<DownloadDecision> AlbumSearch(int albumId, bool missingOnly, bool userInvokedSearch);
        List<DownloadDecision> ArtistSearch(int artistId, bool missingOnly, bool userInvokedSearch);
    }

    public class NzbSearchService : ISearchForNzb
    {
        private readonly IIndexerFactory _indexerFactory;
        [System.Obsolete("Used for sonarr, not lidarr")]
        private readonly ISeriesService _seriesService;
        [System.Obsolete("Used for sonarr, not lidarr")]
        private readonly IEpisodeService _episodeService;

        private readonly IAlbumService _albumService;
        private readonly IArtistService _artistService;
        private readonly IMakeDownloadDecision _makeDownloadDecision;
        private readonly Logger _logger;

        public NzbSearchService(IIndexerFactory indexerFactory,
                                ISeriesService seriesService,
                                IEpisodeService episodeService,
                                IAlbumService albumService,
                                IArtistService artistService,
                                IMakeDownloadDecision makeDownloadDecision,
                                Logger logger)
        {
            _indexerFactory = indexerFactory;
            //_sceneMapping = sceneMapping;
            _seriesService = seriesService;
            _episodeService = episodeService;
            _albumService = albumService;
            _artistService = artistService;
            _makeDownloadDecision = makeDownloadDecision;
            _logger = logger;
        }

        public List<DownloadDecision> EpisodeSearch(int episodeId, bool userInvokedSearch)
        {
            var episode = _episodeService.GetEpisode(episodeId);

            return EpisodeSearch(episode, userInvokedSearch);
        }

        public List<DownloadDecision> EpisodeSearch(Episode episode, bool userInvokedSearch)
        {
            var series = _seriesService.GetSeries(episode.SeriesId);

            if (series.SeriesType == SeriesTypes.Daily)
            {
                if (string.IsNullOrWhiteSpace(episode.AirDate))
                {
                    throw new InvalidOperationException("Daily episode is missing AirDate. Try to refresh series info.");
                }

                return SearchDaily(series, episode, userInvokedSearch);
            }
            if (series.SeriesType == SeriesTypes.Anime)
            {
                return SearchAnime(series, episode, userInvokedSearch);
            }

            if (episode.SeasonNumber == 0)
            {
                // search for special episodes in season 0 
                return SearchSpecial(series, new List<Episode> { episode }, userInvokedSearch);
            }

            return SearchSingle(series, episode, userInvokedSearch);
        }

        public List<DownloadDecision> SeasonSearch(int seriesId, int seasonNumber, bool missingOnly, bool userInvokedSearch)
        {
            var series = _seriesService.GetSeries(seriesId);
            var episodes = _episodeService.GetEpisodesBySeason(seriesId, seasonNumber);

            if (missingOnly)
            {
                episodes = episodes.Where(e => e.Monitored && !e.HasFile).ToList();
            }

            if (series.SeriesType == SeriesTypes.Anime)
            {
                return SearchAnimeSeason(series, episodes, userInvokedSearch);
            }

            if (seasonNumber == 0)
            {
                // search for special episodes in season 0 
                return SearchSpecial(series, episodes, userInvokedSearch);
            }

            var downloadDecisions = new List<DownloadDecision>();

            if (series.UseSceneNumbering)
            {
                var sceneSeasonGroups = episodes.GroupBy(v =>
                {
                    if (v.SceneSeasonNumber.HasValue && v.SceneEpisodeNumber.HasValue)
                    {
                        return v.SceneSeasonNumber.Value;
                    }
                    return v.SeasonNumber;
                }).Distinct();

                foreach (var sceneSeasonEpisodes in sceneSeasonGroups)
                {
                    if (sceneSeasonEpisodes.Count() == 1)
                    {
                        var episode = sceneSeasonEpisodes.First();
                        var searchSpec = Get<SingleEpisodeSearchCriteria>(series, sceneSeasonEpisodes.ToList(), userInvokedSearch);

                        searchSpec.SeasonNumber = sceneSeasonEpisodes.Key;
                        searchSpec.MonitoredEpisodesOnly = true;

                        if (episode.SceneSeasonNumber.HasValue && episode.SceneEpisodeNumber.HasValue)
                        {
                            searchSpec.EpisodeNumber = episode.SceneEpisodeNumber.Value;
                        }
                        else
                        {
                            searchSpec.EpisodeNumber = episode.EpisodeNumber;
                        }

                        var decisions = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
                        downloadDecisions.AddRange(decisions);
                    }
                    else
                    {
                        var searchSpec = Get<SeasonSearchCriteria>(series, sceneSeasonEpisodes.ToList(), userInvokedSearch);
                        searchSpec.SeasonNumber = sceneSeasonEpisodes.Key;

                        var decisions = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
                        downloadDecisions.AddRange(decisions);
                    }
                }
            }
            else
            {
                var searchSpec = Get<SeasonSearchCriteria>(series, episodes, userInvokedSearch);
                searchSpec.SeasonNumber = seasonNumber;

                var decisions = Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
                downloadDecisions.AddRange(decisions);
            }

            return downloadDecisions;
        }

        public List<DownloadDecision> AlbumSearch(int albumId, bool missingOnly, bool userInvokedSearch)
        {
            var album = _albumService.GetAlbum(albumId);
            return AlbumSearch(album, missingOnly, userInvokedSearch);
        }

        public List<DownloadDecision> ArtistSearch(int artistId, bool missingOnly, bool userInvokedSearch)
        {
            var artist = _artistService.GetArtist(artistId);
            return ArtistSearch(artist, missingOnly, userInvokedSearch);
        }

        public List<DownloadDecision> ArtistSearch(Artist artist, bool missingOnly, bool userInvokedSearch)
        {
            var searchSpec = Get<ArtistSearchCriteria>(artist, userInvokedSearch);
            var albums = _albumService.GetAlbumsByArtist(artist.Id);

            searchSpec.Albums = albums;
            
            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        public List<DownloadDecision> AlbumSearch(Album album, bool missingOnly, bool userInvokedSearch)
        {
            var artist = _artistService.GetArtist(album.ArtistId);

            var searchSpec = Get<AlbumSearchCriteria>(artist, new List<Album> { album }, userInvokedSearch);

            searchSpec.AlbumTitle = album.Title;
            if (album.ReleaseDate.HasValue)
            {
                searchSpec.AlbumYear = album.ReleaseDate.Value.Year;
            }
            
            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private List<DownloadDecision> SearchSingle(Series series, Episode episode, bool userInvokedSearch)
        {
            var searchSpec = Get<SingleEpisodeSearchCriteria>(series, new List<Episode> { episode }, userInvokedSearch);

            if (series.UseSceneNumbering && episode.SceneSeasonNumber.HasValue && episode.SceneEpisodeNumber.HasValue)
            {
                searchSpec.EpisodeNumber = episode.SceneEpisodeNumber.Value;
                searchSpec.SeasonNumber = episode.SceneSeasonNumber.Value;
            }
            else
            {
                searchSpec.EpisodeNumber = episode.EpisodeNumber;
                searchSpec.SeasonNumber = episode.SeasonNumber;
            }

            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private List<DownloadDecision> SearchDaily(Series series, Episode episode, bool userInvokedSearch)
        {
            var airDate = DateTime.ParseExact(episode.AirDate, Episode.AIR_DATE_FORMAT, CultureInfo.InvariantCulture);
            var searchSpec = Get<DailyEpisodeSearchCriteria>(series, new List<Episode> { episode }, userInvokedSearch);
            searchSpec.AirDate = airDate;

            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private List<DownloadDecision> SearchAnime(Series series, Episode episode, bool userInvokedSearch)
        {
            var searchSpec = Get<AnimeEpisodeSearchCriteria>(series, new List<Episode> { episode }, userInvokedSearch);

            if (episode.SceneAbsoluteEpisodeNumber.HasValue)
            {
                searchSpec.AbsoluteEpisodeNumber = episode.SceneAbsoluteEpisodeNumber.Value;
            }
            else if (episode.AbsoluteEpisodeNumber.HasValue)
            {
                searchSpec.AbsoluteEpisodeNumber = episode.AbsoluteEpisodeNumber.Value;
            }
            else
            {
                throw new ArgumentOutOfRangeException("AbsoluteEpisodeNumber", "Can not search for an episode without an absolute episode number");
            }

            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private List<DownloadDecision> SearchSpecial(Series series, List<Episode> episodes, bool userInvokedSearch)
        {
            var searchSpec = Get<SpecialEpisodeSearchCriteria>(series, episodes, userInvokedSearch);
            // build list of queries for each episode in the form: "<series> <episode-title>"
            searchSpec.EpisodeQueryTitles = episodes.Where(e => !string.IsNullOrWhiteSpace(e.Title))
                                                    .SelectMany(e => searchSpec.QueryTitles.Select(title => title + " " + SearchCriteriaBase.GetQueryTitle(e.Title)))
                                                    .ToArray();

            return Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private List<DownloadDecision> SearchAnimeSeason(Series series, List<Episode> episodes, bool userInvokedSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            foreach (var episode in episodes.Where(e => e.Monitored))
            {
                downloadDecisions.AddRange(SearchAnime(series, episode, userInvokedSearch));
            }

            return downloadDecisions;
        }

        private TSpec Get<TSpec>(Series series, List<Episode> episodes, bool userInvokedSearch) where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();

            spec.Series = series;
            // spec.SceneTitles = _sceneMapping.GetSceneNames(series.TvdbId,
            //                                               episodes.Select(e => e.SeasonNumber).Distinct().ToList(),
            //                                               episodes.Select(e => e.SceneSeasonNumber ?? e.SeasonNumber).Distinct().ToList());

            spec.Episodes = episodes;

            spec.SceneTitles.Add(series.Title);
            spec.UserInvokedSearch = userInvokedSearch;

            return spec;
        }

        private TSpec Get<TSpec>(Artist artist, List<Album> albums, bool userInvokedSearch) where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();

            spec.Albums = albums;
            spec.Artist = artist;
            spec.UserInvokedSearch = userInvokedSearch;

            return spec;
        }

        private static TSpec Get<TSpec>(Artist artist, bool userInvokedSearch) where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();
            spec.Artist = artist;
            spec.UserInvokedSearch = userInvokedSearch;

            return spec;
        }

        private List<DownloadDecision> Dispatch(Func<IIndexer, IEnumerable<ReleaseInfo>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = _indexerFactory.SearchEnabled();
            var reports = new List<ReleaseInfo>();

            _logger.ProgressInfo("Searching {0} indexers for {1}", indexers.Count, criteriaBase);

            var taskList = new List<Task>();
            var taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            foreach (var indexer in indexers)
            {
                var indexerLocal = indexer;

                taskList.Add(taskFactory.StartNew(() =>
                {
                    try
                    {
                        var indexerReports = searchAction(indexerLocal);

                        lock (reports)
                        {
                            reports.AddRange(indexerReports);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error while searching for {0}", criteriaBase);
                    }
                }).LogExceptions());
            }

            Task.WaitAll(taskList.ToArray());

            _logger.Debug("Total of {0} reports were found for {1} from {2} indexers", reports.Count, criteriaBase, indexers.Count);

            return _makeDownloadDecision.GetSearchDecision(reports, criteriaBase).ToList();
        }
    }
}
