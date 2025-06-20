using System;
using System.Collections.Generic;
using System.Linq;
using DryIoc.ImTools;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Youtube
{
    public class YoutubePlaylist : YoutubeImportListBase<YoutubePlaylistSettings>
    {
        public YoutubePlaylist(IImportListStatusService importListStatusService,
            IImportListRepository importListRepository,
            IConfigService configService,
            IParsingService parsingService,
            IHttpClient httpClient,
            Logger logger)
            : base(importListStatusService, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Youtube Playlists";

        public override IList<YoutubeImportListItemInfo> Fetch(YouTubeService service)
        {
            return Settings.PlaylistIds.SelectMany(x => Fetch(service, x)).ToList();
        }

        public IList<YoutubeImportListItemInfo> Fetch(YouTubeService service, string playlistId)
        {
            var results = new List<YoutubeImportListItemInfo>();
            var req = service.PlaylistItems.List("contentDetails,snippet");
            req.PlaylistId = playlistId;
            req.MaxResults = 50;
            var page = 0;
            var playlist = req.Execute();
            do
            {
                page++;
                req.PageToken = playlist.NextPageToken;

                foreach (var song in playlist.Items)
                {
                    var listItem = new YoutubeImportListItemInfo();
                    var topicChannel = song.Snippet.VideoOwnerChannelTitle.EndsWith("- Topic");
                    if (topicChannel)
                    {
                        ParseTopicChannel(song, ref listItem);
                    }
                    else
                    {
                        // No album name just video
                        listItem.ReleaseDate = ParseDateTimeOffset(song);
                        listItem.Artist = song.Snippet.VideoOwnerChannelTitle;
                    }

                    results.Add(listItem);
                }

                playlist = req.Execute();
            }
            while (playlist.NextPageToken != null && page < 10);
            return results;
        }

        public void ParseTopicChannel(PlaylistItem playlistItem, ref YoutubeImportListItemInfo listItem)
        {
            var description = playlistItem.Snippet.Description;
            var descArgs = description.Split("\n\n");

            listItem.Artist = playlistItem.Snippet.VideoOwnerChannelTitle.Contains("- Topic") ?
                playlistItem.Snippet.VideoOwnerChannelTitle[.. (playlistItem.Snippet.VideoOwnerChannelTitle.LastIndexOf('-') - 1)] :
                playlistItem.Snippet.VideoOwnerChannelTitle;
            listItem.Album = descArgs[2];

            if (descArgs.Any(s => s.StartsWith("Released on:")))
            {
                // Custom release date
                var release = descArgs.FindFirst(s => s.StartsWith("Released on:"));
                var date = release.Substring(release.IndexOf(':') + 1);
                listItem.ReleaseDate = DateTime.Parse(date);
            }
            else
            {
                listItem.ReleaseDate = ParseDateTimeOffset(playlistItem);
            }
        }

        private DateTime ParseDateTimeOffset(PlaylistItem playlistItem)
        {
            return (playlistItem.ContentDetails.VideoPublishedAtDateTimeOffset ?? DateTimeOffset.UnixEpoch).DateTime;
        }
    }
}
