using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blacklisting
{
    public interface IBlacklistService
    {
        bool Blacklisted(int artistId, ReleaseInfo release);
        PagingSpec<Blacklist> Paged(PagingSpec<Blacklist> pagingSpec);
        void Delete(int id);
        void Delete(List<int> ids);
    }

    public class BlacklistService : IBlacklistService,
                                    IExecute<ClearBlacklistCommand>,
                                    IHandle<DownloadFailedEvent>,
                                    IHandleAsync<ArtistsDeletedEvent>
    {
        private readonly IBlacklistRepository _blacklistRepository;
        private readonly List<IBlacklistForProtocol> _protocolBlacklists;

        public BlacklistService(IBlacklistRepository blacklistRepository,
                                IEnumerable<IBlacklistForProtocol> protocolBlacklists)
        {
            _blacklistRepository = blacklistRepository;
            _protocolBlacklists = protocolBlacklists.ToList();
        }

        public bool Blacklisted(int artistId, ReleaseInfo release)
        {
            var protocolBlacklist = _protocolBlacklists.FirstOrDefault(x => x.Protocol == release.DownloadProtocol);

            if (protocolBlacklist != null)
            {
                return protocolBlacklist.IsBlacklisted(artistId, release);
            }

            return false;
        }

        public PagingSpec<Blacklist> Paged(PagingSpec<Blacklist> pagingSpec)
        {
            return _blacklistRepository.GetPaged(pagingSpec);
        }

        public void Delete(int id)
        {
            _blacklistRepository.Delete(id);
        }

        public void Delete(List<int> ids)
        {
            _blacklistRepository.DeleteMany(ids);
        }

        public void Execute(ClearBlacklistCommand message)
        {
            _blacklistRepository.Purge();
        }

        public void Handle(DownloadFailedEvent message)
        {
            var protocolBlacklist = _protocolBlacklists.FirstOrDefault(x => x.Protocol == message.Data.GetValueOrDefault("protocol"));

            if (protocolBlacklist != null)
            {
                var blacklist = protocolBlacklist.GetBlacklist(message);

                _blacklistRepository.Insert(blacklist);
            }
        }

        public void HandleAsync(ArtistsDeletedEvent message)
        {
            _blacklistRepository.DeleteForArtists(message.Artists.Select(x => x.Id).ToList());
        }
    }
}
