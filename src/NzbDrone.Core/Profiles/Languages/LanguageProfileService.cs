using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Profiles.Languages
{
    public interface ILanguageProfileService
    {
        LanguageProfile Add(LanguageProfile profile);
        void Update(LanguageProfile profile);
        void Delete(int id);
        List<LanguageProfile> All();
        LanguageProfile Get(int id);
        bool Exists(int id);
        LanguageProfile GetDefaultProfile(string name, Language cutoff = null, params Language[] allowed);
    }

    public class LanguageProfileService : ILanguageProfileService, IHandle<ApplicationStartedEvent>
    {
        private readonly ILanguageProfileRepository _profileRepository;
        private readonly IArtistService _artistService;
        private readonly IImportListFactory _importListFactory;
        private readonly Logger _logger;

        public LanguageProfileService(ILanguageProfileRepository profileRepository, IArtistService artistService, IImportListFactory importListFactory, Logger logger)
        {
            _profileRepository = profileRepository;
            _artistService = artistService;
            _importListFactory = importListFactory;
            _logger = logger;
        }

        public LanguageProfile Add(LanguageProfile profile)
        {
            return _profileRepository.Insert(profile);
        }

        public void Update(LanguageProfile profile)
        {
            _profileRepository.Update(profile);
        }

        public void Delete(int id)
        {
            if (_artistService.GetAllArtists().Any(c => c.LanguageProfileId == id) || _importListFactory.All().Any(c => c.LanguageProfileId == id))
            {
                var profile = _profileRepository.Get(id);
                throw new LanguageProfileInUseException(profile.Name);
            }

            _profileRepository.Delete(id);
        }

        public List<LanguageProfile> All()
        {
            return _profileRepository.All().ToList();
        }

        public LanguageProfile Get(int id)
        {
            return _profileRepository.Get(id);
        }

        public bool Exists(int id)
        {
            return _profileRepository.Exists(id);
        }

        public LanguageProfile GetDefaultProfile(string name, Language cutoff = null, params Language[] allowed)
        {
            var orderedLanguages = Language.All
                                           .Where(l => l != Language.Unknown)
                                           .OrderByDescending(l => l.Name)
                                           .ToList();

            orderedLanguages.Insert(0, Language.Unknown);

            var languages = orderedLanguages.Select(v => new LanguageProfileItem { Language = v, Allowed = false })
                                            .ToList();

            return new LanguageProfile
            {
                Cutoff = Language.Unknown,
                Languages = languages
            };
        }

        private LanguageProfile AddDefaultProfile(string name, Language cutoff, params Language[] allowed)
        {
            var languages = Language.All
                                    .OrderByDescending(l => l.Name)
                                    .Select(v => new LanguageProfileItem { Language = v, Allowed = allowed.Contains(v) })
                                    .ToList();

            var profile = new LanguageProfile
            {
                Name = name, 
                Cutoff = cutoff, 
                Languages = languages, 
            };

            return Add(profile);
        }

        public void Handle(ApplicationStartedEvent message)
        {
            if (All().Any()) return;

            _logger.Info("Setting up default language profiles");

            AddDefaultProfile("English", Language.English, Language.English);
        }
    }
}
