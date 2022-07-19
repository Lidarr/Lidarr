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

        public void Delete(int id)
        {
            _blocklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blocklistRepository.DeleteMany(ids);
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
