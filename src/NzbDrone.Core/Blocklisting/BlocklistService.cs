using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistService
    {
        bool Blocklisted(int artistId, ReleaseInfo release);
        PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec);
        void Block(RemoteAlbum remoteAlbum, string message);
        void Delete(int id);
        void Delete(List<int> ids);
    }

    public class BlocklistService : IBlocklistService,
                                    IExecute<ClearBlocklistCommand>,
                                    IHandle<DownloadFailedEvent>,
                                    IHandleAsync<ArtistsDeletedEvent>
    {
        private readonly IBlocklistRepository _blocklistRepository;
        private readonly List<IBlocklistForProtocol> _protocolBlocklists;

        public BlocklistService(IBlocklistRepository blocklistRepository,
                                IEnumerable<IBlocklistForProtocol> protocolBlocklists)
        {
            _blocklistRepository = blocklistRepository;
            _protocolBlocklists = protocolBlocklists.ToList();
        }

        public bool Blocklisted(int artistId, ReleaseInfo release)
        {
            var protocolBlocklist = _protocolBlocklists.FirstOrDefault(x => x.Protocol == release.DownloadProtocol);

            if (protocolBlocklist != null)
            {
                return protocolBlocklist.IsBlocklisted(artistId, release);
            }

            return false;
        }

        public PagingSpec<Blocklist> Paged(PagingSpec<Blocklist> pagingSpec)
        {
            return _blocklistRepository.GetPaged(pagingSpec);
        }

        public void Block(RemoteAlbum remoteAlbum, string message)
        {
            var blocklist = new Blocklist
            {
                ArtistId = remoteAlbum.Artist.Id,
                AlbumIds = remoteAlbum.Albums.Select(e => e.Id).ToList(),
                SourceTitle = remoteAlbum.Release.Title,
                Quality = remoteAlbum.ParsedAlbumInfo.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = remoteAlbum.Release.PublishDate,
                Size = remoteAlbum.Release.Size,
                Indexer = remoteAlbum.Release.Indexer,
                Protocol = remoteAlbum.Release.DownloadProtocol,
                Message = message,
            };

            if (remoteAlbum.Release is TorrentInfo torrentRelease)
            {
                blocklist.TorrentInfoHash = torrentRelease.InfoHash;
            }

            _blocklistRepository.Insert(blocklist);
        }

        public void Delete(int id)
        {
            _blocklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blocklistRepository.DeleteMany(ids);
        }

        private bool SameNzb(Blocklist item, ReleaseInfo release)
        {
            if (item.PublishedDate == release.PublishDate)
            {
                return true;
            }

            if (!HasSameIndexer(item, release.Indexer) &&
                HasSamePublishedDate(item, release.PublishDate) &&
                HasSameSize(item, release.Size))
            {
                return true;
            }

            return false;
        }

        private bool SameTorrent(Blocklist item, TorrentInfo release)
        {
            if (release.InfoHash.IsNotNullOrWhiteSpace())
            {
                return release.InfoHash.Equals(item.TorrentInfoHash, StringComparison.InvariantCultureIgnoreCase);
            }

            return HasSameIndexer(item, release.Indexer);
        }

        private bool HasSameIndexer(Blocklist item, string indexer)
        {
            if (item.Indexer.IsNullOrWhiteSpace())
            {
                return true;
            }

            return item.Indexer.Equals(indexer, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool HasSamePublishedDate(Blocklist item, DateTime publishedDate)
        {
            if (!item.PublishedDate.HasValue)
            {
                return true;
            }

            return item.PublishedDate.Value.AddMinutes(-2) <= publishedDate &&
                   item.PublishedDate.Value.AddMinutes(2) >= publishedDate;
        }

        private bool HasSameSize(Blocklist item, long size)
        {
            if (!item.Size.HasValue)
            {
                return true;
            }

            var difference = Math.Abs(item.Size.Value - size);

            return difference <= 2.Megabytes();
        }

        public void Execute(ClearBlocklistCommand message)
        {
            _blocklistRepository.Purge();
        }

        public void Handle(DownloadFailedEvent message)
        {
            var protocolBlocklist = _protocolBlocklists.FirstOrDefault(x => x.Protocol == message.Data.GetValueOrDefault("protocol"));

            if (protocolBlocklist != null)
            {
                var blocklist = protocolBlocklist.GetBlocklist(message);

                _blocklistRepository.Insert(blocklist);
            }
        }

        public void HandleAsync(ArtistsDeletedEvent message)
        {
            _blocklistRepository.DeleteForArtists(message.Artists.Select(x => x.Id).ToList());
        }
    }
}
