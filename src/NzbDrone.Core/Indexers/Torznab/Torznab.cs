using System;
using System.Collections.Generic;
using System.Linq;
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

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override int PageSize => _capabilitiesProvider.GetCapabilities(Settings).DefaultPageSize;

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

        public Torznab(INewznabCapabilitiesProvider capabilitiesProvider, IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _capabilitiesProvider = capabilitiesProvider;
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
                Protocol = DownloadProtocol.Usenet,
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

        protected override void Test(List<ValidationFailure> failures)
        {
            base.Test(failures);
            if (failures.HasErrors())
            {
                return;
            }

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
    }
}
