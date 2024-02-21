using System.Collections.Generic;
using System.Linq;
using System.Net;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Redacted
{
    public class RedactedParser : IParseIndexerResponse
    {
        private readonly RedactedSettings _settings;

        public RedactedParser(RedactedSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<GazelleResponse>(indexerResponse.HttpResponse);
            if (jsonResponse.Resource.Status != "success" ||
                jsonResponse.Resource.Status.IsNullOrWhiteSpace() ||
                jsonResponse.Resource.Response == null)
            {
                return torrentInfos;
            }

            foreach (var result in jsonResponse.Resource.Response.Results)
            {
                if (result.Torrents != null)
                {
                    foreach (var torrent in result.Torrents)
                    {
                        var id = torrent.TorrentId;
                        var title = WebUtility.HtmlDecode(GetTitle(result, torrent));
                        var artist = WebUtility.HtmlDecode(result.Artist);
                        var album = WebUtility.HtmlDecode(result.GroupName);

                        torrentInfos.Add(new GazelleInfo
                        {
                            Guid = $"Redacted-{id}",
                            InfoUrl = GetInfoUrl(result.GroupId, id),
                            DownloadUrl = GetDownloadUrl(id, torrent.CanUseToken),
                            Title = title,
                            Artist = artist,
                            Album = album,
                            Container = torrent.Encoding,
                            Codec = torrent.Format,
                            Size = long.Parse(torrent.Size),
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = torrent.Time.ToUniversalTime(),
                            IndexerFlags = GetIndexerFlags(torrent)
                        });
                    }
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        private string GetTitle(GazelleRelease result, GazelleTorrent torrent)
        {
            var title = $"{result.Artist} - {result.GroupName} ({result.GroupYear})";

            if (result.ReleaseType.IsNotNullOrWhiteSpace() && result.ReleaseType != "Unknown")
            {
                title += " [" + result.ReleaseType + "]";
            }

            if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
            {
                title += $" [{$"{torrent.RemasterTitle} {torrent.RemasterYear}".Trim()}]";
            }

            var flags = new List<string>
            {
                $"{torrent.Format} {torrent.Encoding}",
                $"{torrent.Media}"
            };

            if (torrent.HasLog)
            {
                flags.Add("Log (" + torrent.LogScore + "%)");
            }

            if (torrent.HasCue)
            {
                flags.Add("Cue");
            }

            return $"{title} [{string.Join(" / ", flags)}]";
        }

        private static IndexerFlags GetIndexerFlags(GazelleTorrent torrent)
        {
            IndexerFlags flags = 0;

            if (torrent.IsFreeLeech || torrent.IsNeutralLeech || torrent.IsFreeload || torrent.IsPersonalFreeLeech)
            {
                flags |= IndexerFlags.Freeleech;
            }

            if (torrent.Scene)
            {
                flags |= IndexerFlags.Scene;
            }

            return flags;
        }

        private string GetDownloadUrl(int torrentId, bool canUseToken)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/ajax.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId);

            if (_settings.UseFreeleechToken && canUseToken)
            {
                url = url.AddQueryParam("usetoken", "1");
            }

            return url.FullUri;
        }

        private string GetInfoUrl(string groupId, int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("id", groupId)
                .AddQueryParam("torrentid", torrentId);

            return url.FullUri;
        }
    }
}
