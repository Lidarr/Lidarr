using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blacklisting
{
    public class TorrentBlacklist : IBlacklistForProtocol
    {
        private readonly IBlacklistRepository _blacklistRepository;

        public TorrentBlacklist(IBlacklistRepository blacklistRepository)
        {
            _blacklistRepository = blacklistRepository;
        }

        public string Protocol => nameof(TorrentDownloadProtocol);

        public bool IsBlacklisted(int artistId, ReleaseInfo release)
        {
            var blacklistedByTitle = _blacklistRepository.BlacklistedByTitle(artistId, release.Title)
                .Where(b => b.Protocol == Protocol);

            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return false;
            }

            if (torrentInfo.InfoHash.IsNullOrWhiteSpace())
            {
                return blacklistedByTitle.Where(b => b.Protocol == nameof(TorrentDownloadProtocol))
                    .Any(b => SameTorrent(b, torrentInfo));
            }

            var blacklistedByTorrentInfohash = _blacklistRepository.BlacklistedByTorrentInfoHash(artistId, torrentInfo.InfoHash);
            return blacklistedByTorrentInfohash.Any(b => SameTorrent(b, torrentInfo));
        }

        public Blacklist GetBlacklist(DownloadFailedEvent message)
        {
            return new Blacklist
            {
                ArtistId = message.ArtistId,
                AlbumIds = message.AlbumIds,
                SourceTitle = message.SourceTitle,
                Quality = message.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = DateTime.Parse(message.Data.GetValueOrDefault("publishedDate")),
                Size = long.Parse(message.Data.GetValueOrDefault("size", "0")),
                Indexer = message.Data.GetValueOrDefault("indexer"),
                Protocol = message.Data.GetValueOrDefault("protocol"),
                Message = message.Message,
                TorrentInfoHash = message.Data.GetValueOrDefault("torrentInfoHash")
            };
        }

        private bool SameTorrent(Blacklist item, TorrentInfo release)
        {
            if (release.InfoHash.IsNotNullOrWhiteSpace())
            {
                return release.InfoHash.Equals(item.TorrentInfoHash);
            }

            return item.Indexer.Equals(release.Indexer, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
