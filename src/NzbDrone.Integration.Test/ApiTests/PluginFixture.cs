using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Lifecycle.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Plugins.Commands;
using NzbDrone.Integration.Test.Client;
using RestSharp;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class PluginFixture : IntegrationTest
    {
        [Test]
        [Order(0)]
        public void should_install_plugin()
        {
            PostAndWaitForCompletion(new InstallPluginCommand
            {
                GithubUrl = "https://github.com/ta264/Lidarr.Plugin.Deemix"
            });

            PostAndWaitForRestart(new RestartCommand());

            WaitForRestart();

            var plugins = Plugins.All();
            plugins.Should().HaveCount(1);
            plugins[0].Name.Should().Be("Deemix");
        }

        [Test]
        [Order(1)]
        public void should_uninstall_plugin()
        {
            var plugins = Plugins.All();
            plugins.Should().HaveCount(1);
            plugins[0].Name.Should().Be("Deemix");

            PostAndWaitForCompletion(new UninstallPluginCommand
            {
                GithubUrl = "https://github.com/ta264/Lidarr.Plugin.Deemix"
            });

            PostAndWaitForRestart(new RestartCommand());

            WaitForRestart();

            plugins = Plugins.All();
            plugins.Should().BeEmpty();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            var request = new RestRequest("system/shutdown");
            request.Method = Method.POST;
            request.AddHeader("Authorization", ApiKey);
            RestClient.Execute(request);
        }

        private SimpleCommandResource PostAndWaitForCompletion<T>(T command)
            where T : Command, new()
        {
            var request = new RestRequest("command");
            request.Method = Method.POST;
            request.AddHeader("Authorization", ApiKey);
            request.AddJsonBody(command);

            var result = RestClient.Execute(request);
            var resource = Json.Deserialize<SimpleCommandResource>(result.Content);

            var id = resource.Id;

            id.Should().NotBe(0);

            for (var i = 0; i < 50; i++)
            {
                if (resource?.Status == CommandStatus.Completed)
                {
                    return resource;
                }

                var get = new RestRequest($"command/{id}");
                get.AddHeader("Authorization", ApiKey);

                result = RestClient.Execute(get);

                TestContext.Progress.WriteLine("Waiting for command to finish : {0}  [{1}] {2}\n{3}", result.ResponseStatus, result.StatusDescription, result.ErrorException?.Message, result.Content);

                resource = Json.Deserialize<SimpleCommandResource>(result.Content);
                Thread.Sleep(500);
            }

            Assert.Fail("Command failed");
            return resource;
        }

        private SimpleCommandResource PostAndWaitForRestart<T>(T command)
            where T : Command, new()
        {
            var request = new RestRequest("command");
            request.Method = Method.POST;
            request.AddHeader("Authorization", ApiKey);
            request.AddJsonBody(command);

            var result = RestClient.Execute(request);
            var resource = Json.Deserialize<SimpleCommandResource>(result.Content);

            var id = resource.Id;

            id.Should().NotBe(0);

            for (var i = 0; i < 50; i++)
            {
                if (resource?.Status == CommandStatus.Completed)
                {
                    return resource;
                }

                var get = new RestRequest($"command/{id}");
                get.AddHeader("Authorization", ApiKey);

                result = RestClient.Execute(get);

                TestContext.Progress.WriteLine("Waiting for command to finish : {0}  [{1}] {2}\n{3}", result.ResponseStatus, result.StatusDescription, result.ErrorException?.Message, result.Content);

                resource = Json.Deserialize<SimpleCommandResource>(result.Content);
                Thread.Sleep(500);
            }

            Assert.Fail("Command failed");
            return resource;
        }

        private void WaitForRestart()
        {
            for (var i = 0; i < 60; i++)
            {
                var request = new RestRequest("system/status");
                request.AddHeader("Authorization", ApiKey);
                request.AddHeader("X-Api-Key", ApiKey);

                var statusCall = RestClient.Get(request);

                if (statusCall.ResponseStatus == ResponseStatus.Completed)
                {
                    TestContext.Progress.WriteLine($"Lidarr {Port} is started. Running Tests");
                    return;
                }

                TestContext.Progress.WriteLine("Waiting for Lidarr to start. Response Status : {0}  [{1}] {2}", statusCall.ResponseStatus, statusCall.StatusDescription, statusCall.ErrorException.Message);

                Thread.Sleep(500);
            }

            Assert.Fail("Timed out waiting for restart");
        }
    }
}
