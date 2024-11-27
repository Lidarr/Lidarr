using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderAddedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderDeletedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderUpdatedEvent<IIndexer>))]
    public class RedactedGazelleCheck : HealthCheckBase
    {
        private readonly IIndexerFactory _indexerFactory;

        public RedactedGazelleCheck(IIndexerFactory indexerFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            _indexerFactory = indexerFactory;
        }

        public override HealthCheck Check()
        {
            var indexers = _indexerFactory.GetAvailableProviders();

            foreach (var indexer in indexers)
            {
                var definition = (IndexerDefinition)indexer.Definition;

                if (definition.Settings is GazelleSettings { BaseUrl: "https://redacted.sh" } || definition.Settings is GazelleSettings { BaseUrl: "https://redacted.ch" })
                {
                    return new HealthCheck(GetType(), HealthCheckResult.Warning, "You have set up Redacted as a Gazelle indexer, please reconfigure using the Redacted indexer setting");
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
