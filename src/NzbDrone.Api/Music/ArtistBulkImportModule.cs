using System.Collections;
using System.Collections.Generic;
using Nancy;
using NzbDrone.Api.Extensions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using System.Linq;
using System;
using Marr.Data;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.RootFolders;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Music;

namespace NzbDrone.Api.Music
{

    public class UnmappedComparer : IComparer<UnmappedFolder>
    {
        public int Compare(UnmappedFolder a, UnmappedFolder b)
        {
            return a.Name.CompareTo(b.Name);
        }
    }

    public class MovieBulkImportModule : NzbDroneRestModule<ArtistResource>
    {
        private readonly ISearchForNewSeries _searchProxy;
        private readonly IRootFolderService _rootFolderService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IDiskScanService _diskScanService;
		private readonly ICached<Core.Music.Artist> _mappedArtists;
        private readonly IArtistService _artistService;

        public MovieBulkImportModule(ISearchForNewSeries searchProxy, IRootFolderService rootFolderService, IMakeImportDecision importDecisionMaker,
		                             IDiskScanService diskScanService, ICacheManager cacheManager, IArtistService artistService)
            : base("/artist/bulkimport")
        {
            _searchProxy = searchProxy;
            _rootFolderService = rootFolderService;
            _importDecisionMaker = importDecisionMaker;
            _diskScanService = diskScanService;
			_mappedArtists = cacheManager.GetCache<Artist>(GetType(), "mappedArtistsCache");
            _artistService = artistService;
            Get["/"] = x => Search();
        }


        private Response Search()
        {
            if (Request.Query.Id == 0)
            {
                //Todo error handling
            }

            RootFolder rootFolder = _rootFolderService.Get(Request.Query.Id);

            var unmapped = rootFolder.UnmappedFolders.OrderBy(f => f.Name).ToList();

            var paged = unmapped;

            var mapped = paged.Select(f =>
			{
				Artist m = null;

				var mappedArtist = _mappedArtists.Find(f.Name);

				if (mappedArtist != null)
				{
					return mappedArtist;
				}

                var files = _diskScanService.GetMusicFiles(f.Path);

                // Check for music files in directory
                if (files.Count() == 0)
                {
                    return null;
                }

                var parsedTitle = Parser.ParseMusicPath(files.FirstOrDefault());
				if (parsedTitle == null)
				{
					m = new Artist
					{
						Name = f.Name.Replace(".", " ").Replace("-", " "),
						Path = f.Path,
					};
				}
				else
				{
					m = new Artist
                    {
						Name = parsedTitle.ArtistTitle,
						Path = f.Path
					};
				}

				var files1 = _diskScanService.GetMusicFiles(f.Path);

				var decisions = _importDecisionMaker.GetImportDecisions(files1.ToList(), m);

				var decision = decisions.Where(d => d.Approved && !d.Rejections.Any()).FirstOrDefault();

				mappedArtist = _searchProxy.SearchForNewArtist(parsedTitle.ArtistTitle.ToString())[0];

				if (mappedArtist != null)
				{
					mappedArtist.Monitored = true;
                    mappedArtist.Path = f.Path;

                    _mappedArtists.Set(f.Name, mappedArtist, TimeSpan.FromDays(2));

					return mappedArtist;
				}

				return null;
            });
            
            var mapping = MapToResource(mapped.Where(m => m != null)).ToList().AsResponse();

            return mapping;
        }


        private static IEnumerable<ArtistResource> MapToResource(IEnumerable<Artist> artists)
        {
            foreach (var currentArtist in artists)
            {
                var resource = currentArtist.ToResource();
                var poster = currentArtist.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.Url;
                }

                yield return resource;
            }
        }
    }
}
