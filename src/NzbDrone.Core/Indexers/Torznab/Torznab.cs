using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class Torznab : HttpIndexerBase<TorznabSettings>
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public override string Name => "Torznab";

        public override string Protocol => nameof(TorrentDownloadProtocol);
        public override int PageSize => GetProviderPageSize();

        public Torznab(INewznabCapabilitiesProvider capabilitiesProvider, IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _capabilitiesProvider = capabilitiesProvider;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NewznabRequestGenerator(_capabilitiesProvider)
            {
                PageSize = PageSize,
                Settings = Settings
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorznabRssParser();
        }

        private IndexerDefinition GetDefinition(string name, TorznabSettings settings)
        {
            return new IndexerDefinition
            {
                EnableRss = false,
                EnableAutomaticSearch = false,
                EnableInteractiveSearch = false,
                Name = name,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = nameof(UsenetDownloadProtocol),
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch
            };
        }

        private TorznabSettings GetSettings(string url, params int[] categories)
        {
            var settings = new TorznabSettings { BaseUrl = url };

            if (categories.Any())
            {
                settings.Categories = categories;
            }

            return settings;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await base.Test(failures);

            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(JackettAll());
            failures.AddIfNotNull(TestCapabilities());
        }

        protected virtual ValidationFailure TestCapabilities()
        {
            try
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                if (capabilities.SupportedSearchParameters != null && capabilities.SupportedSearchParameters.Contains("q"))
                {
                    return null;
                }

                if (capabilities.SupportedAudioSearchParameters != null &&
                    new[] { "artist", "album" }.All(v => capabilities.SupportedAudioSearchParameters.Contains(v)))
                {
                    return null;
                }

                return new ValidationFailure(string.Empty, "Indexer does not support required search parameters");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer: " + ex.Message);

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }
        }

        protected virtual ValidationFailure JackettAll()
        {
            if (Settings.ApiPath.Contains("/torznab/all") ||
                Settings.ApiPath.Contains("/api/v2.0/indexers/all/results/torznab") ||
                Settings.BaseUrl.Contains("/torznab/all") ||
                Settings.BaseUrl.Contains("/api/v2.0/indexers/all/results/torznab"))
            {
                return new NzbDroneValidationFailure("ApiPath", "Jackett's all endpoint is not supported, please add indexers individually")
                {
                    IsWarning = true,
                    DetailedDescription = "Jackett's all endpoint is not supported, please add indexers individually"
                };
            }

            return null;
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "newznabCategories")
            {
                List<NewznabCategory> categories = null;
                try
                {
                    categories = _capabilitiesProvider.GetCapabilities(Settings).Categories;
                }
                catch
                {
                    // Use default categories
                }

                return new
                {
                    options = NewznabCategoryFieldOptionsConverter.GetFieldSelectOptions(categories)
                };
            }

            return base.RequestAction(action, query);
        }

        private int GetProviderPageSize()
        {
            try
            {
                return Math.Min(100, Math.Max(_capabilitiesProvider.GetCapabilities(Settings).DefaultPageSize, _capabilitiesProvider.GetCapabilities(Settings).MaxPageSize));
            }
            catch
            {
                return 100;
            }
        }
    }
}
