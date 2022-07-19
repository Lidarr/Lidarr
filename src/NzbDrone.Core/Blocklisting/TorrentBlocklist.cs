using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blocklisting
{
    public class TorrentBlocklist : IBlocklistForProtocol
    {
        private readonly IBlocklistRepository _blocklistRepository;

        public TorrentBlocklist(IBlocklistRepository blocklistRepository)
        {
            _blocklistRepository = blocklistRepository;
        }

        public string Protocol => nameof(TorrentDownloadProtocol);

        public bool IsBlocklisted(int artistId, ReleaseInfo release)
        {
            var blocklistedByTitle = _blocklistRepository.BlocklistedByTitle(artistId, release.Title)
                .Where(b => b.Protocol == Protocol);

            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return false;
            }

            if (torrentInfo.InfoHash.IsNullOrWhiteSpace())
            {
                return blocklistedByTitle.Where(b => b.Protocol == nameof(TorrentDownloadProtocol))
                    .Any(b => SameTorrent(b, torrentInfo));
            }

            var blocklistedByTorrentInfohash = _blocklistRepository.BlocklistedByTorrentInfoHash(artistId, torrentInfo.InfoHash);
            return blocklistedByTorrentInfohash.Any(b => SameTorrent(b, torrentInfo));
        }

        public Blocklist GetBlocklist(DownloadFailedEvent message)
        {
            return new Blocklist
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

        private bool SameTorrent(Blocklist item, TorrentInfo release)
        {
            if (release.InfoHash.IsNotNullOrWhiteSpace())
            {
                return release.InfoHash.Equals(item.TorrentInfoHash);
            }

            return item.Indexer.Equals(release.Indexer, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
