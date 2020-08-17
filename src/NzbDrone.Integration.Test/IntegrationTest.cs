using System.Threading;
using Lidarr.Http.ClientSchema;
using NLog;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Test.Common;

namespace NzbDrone.Integration.Test
{
    [Parallelizable(ParallelScope.Fixtures)]
    public abstract class IntegrationTest : IntegrationTestBase
    {
        protected static int StaticPort = 8686;

        protected NzbDroneRunner _runner;

        public override string ArtistRootFolder => GetTempDirectory("ArtistRootFolder");

        protected int Port { get; private set; }

        protected override string RootUrl => $"http://localhost:{Port}/";

        protected override string ApiKey => _runner.ApiKey;

        protected override void StartTestTarget()
        {
            Port = Interlocked.Increment(ref StaticPort);

            _runner = new NzbDroneRunner(LogManager.GetCurrentClassLogger(), Port);
            _runner.Kill();

            _runner.Start();
        }

        protected override void InitializeTestTarget()
        {
            Indexers.Post(new Lidarr.Api.V1.Indexers.IndexerResource
            {
                EnableRss = false,
                EnableInteractiveSearch = false,
                EnableAutomaticSearch = false,
                ConfigContract = nameof(NewznabSettings),
                Implementation = nameof(Newznab),
                Name = "NewznabTest",
                Protocol = Core.Indexers.DownloadProtocol.Usenet,
                Fields = SchemaBuilder.ToSchema(new NewznabSettings())
            });

            // Change Console Log Level to Debug so we get more details.
            var config = HostConfig.Get(1);
            config.ConsoleLogLevel = "Debug";
            HostConfig.Put(config);
        }

        protected override void StopTestTarget()
        {
            _runner.Kill();
        }
    }
}
