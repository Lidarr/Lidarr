using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.AutoTagging
{
    public interface IAutoTaggingService
    {
        void Update(AutoTag autoTag);
        AutoTag Insert(AutoTag autoTag);
        List<AutoTag> All();
        AutoTag GetById(int id);
        void Delete(int id);
        List<AutoTag> AllForTag(int tagId);
        AutoTaggingChanges GetTagChanges(Artist artist);
    }

    public class AutoTaggingService : IAutoTaggingService
    {
        private readonly IAutoTaggingRepository _repository;
        private readonly RootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICached<Dictionary<int, AutoTag>> _cache;

        public AutoTaggingService(IAutoTaggingRepository repository,
                                  RootFolderService rootFolderService,
                                  IEventAggregator eventAggregator,
                                  ICacheManager cacheManager)
        {
            _repository = repository;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;

            _cache = cacheManager.GetCache<Dictionary<int, AutoTag>>(typeof(AutoTag), "autoTags");
        }

        private Dictionary<int, AutoTag> AllDictionary()
        {
            return _cache.Get("all", () => _repository.All().ToDictionary(m => m.Id));
        }

        public List<AutoTag> All()
        {
            return AllDictionary().Values.ToList();
        }

        public AutoTag GetById(int id)
        {
            return AllDictionary()[id];
        }

        public void Update(AutoTag autoTag)
        {
            _repository.Update(autoTag);

            _cache.Clear();
            _eventAggregator.PublishEvent(new AutoTagsUpdatedEvent());
        }

        public AutoTag Insert(AutoTag autoTag)
        {
            var result = _repository.Insert(autoTag);

            _cache.Clear();
            _eventAggregator.PublishEvent(new AutoTagsUpdatedEvent());

            return result;
        }

        public void Delete(int id)
        {
            _repository.Delete(id);

            _cache.Clear();
            _eventAggregator.PublishEvent(new AutoTagsUpdatedEvent());
        }

        public List<AutoTag> AllForTag(int tagId)
        {
            return All().Where(p => p.Tags.Contains(tagId))
                .ToList();
        }

        public AutoTaggingChanges GetTagChanges(Artist artist)
        {
            var autoTags = All();
            var changes = new AutoTaggingChanges();

            if (autoTags.Empty())
            {
                return changes;
            }

            // Set the root folder path on the series
            artist.RootFolderPath = _rootFolderService.GetBestRootFolderPath(artist.Path);

            foreach (var autoTag in autoTags)
            {
                var specificationMatches = autoTag.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(artist))
                    })
                    .ToList();

                var allMatch = specificationMatches.All(x => x.DidMatch);
                var tags = autoTag.Tags;

                if (allMatch)
                {
                    foreach (var tag in tags)
                    {
                        if (!artist.Tags.Contains(tag))
                        {
                            changes.TagsToAdd.Add(tag);
                        }
                    }

                    continue;
                }

                if (autoTag.RemoveTagsAutomatically)
                {
                    foreach (var tag in tags)
                    {
                        changes.TagsToRemove.Add(tag);
                    }
                }
            }

            return changes;
        }
    }
}
