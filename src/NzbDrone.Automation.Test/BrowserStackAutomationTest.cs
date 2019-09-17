using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using Mono.Unix.Native;
using NLog;
using NUnit.Framework;
using NzbDrone.Automation.Test.PageModel;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Test.Common;
using OpenQA.Selenium.Remote;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    [AutomationTest]
    [Parallelizable(ParallelScope.Fixtures)]
    public abstract class BrowserStackAutomationTest : MainPagesTest
    {
        protected string browser;
        protected string browserVersion;
        protected string os;
        protected string osVersion;
        protected string device;

        private readonly Logger _logger;

        private ProcessProvider _processProvider;
        private Process _browserStackLocalProcess;

        public BrowserStackAutomationTest(string device, string os, string osVersion, string browser, string browserVersion, int port)
        {
            this.device = device;
            this.browser = browser;
            this.browserVersion = browserVersion;
            this.os = os;
            this.osVersion = osVersion;

            _logger = LogManager.GetCurrentClassLogger();
            _processProvider = new ProcessProvider(_logger);
            _runner = new NzbDroneRunner(_logger, port);
        }

        [OneTimeSetUp]
        public override void SmokeTestSetup()
        {
            string username = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
            string accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");

            if (username.IsNullOrWhiteSpace() || accessKey.IsNullOrWhiteSpace())
            {
                Assert.Ignore("BrowserStack Tests Disabled, No Credentials");
            }

            _runner.Start();

            string browserstackLocal = "true";
            string browserstackLocalIdentifier = string.Format("Lidarr_{0}_{1}", DateTime.UtcNow.Ticks, new Random().Next());
            string buildName = BuildInfo.Version.ToString();
            string serverOs = OsInfo.Os.ToString();

            DesiredCapabilities capabilities = new DesiredCapabilities();

            capabilities.SetCapability("device", device);
            capabilities.SetCapability("os", os);
            capabilities.SetCapability("os_version", osVersion);
            capabilities.SetCapability("browser", browser);
            capabilities.SetCapability("browser_version", browserVersion);
            capabilities.SetCapability("browserstack.local", browserstackLocal);
            capabilities.SetCapability("browserstack.localIdentifier", browserstackLocalIdentifier);
            capabilities.SetCapability("browserstack.debug", "true");
            capabilities.SetCapability("browserstack.console", "verbose");
            capabilities.SetCapability("name", "Functional Tests: " + serverOs + " - " +  browser);
            capabilities.SetCapability("project", "Lidarr");
            capabilities.SetCapability("build", buildName);

            var bsLocalArgs = $"--key {accessKey} --local-identifier {browserstackLocalIdentifier} --verbose";
            _browserStackLocalProcess = StartBrowserStackLocal(_runner.AppData, bsLocalArgs);

            driver = new RemoteWebDriver(new Uri("https://" + username + ":" + accessKey + "@hub.browserstack.com/wd/hub"), capabilities);

            driver.Url = $"http://{LocalIPAddress()}:{_runner.Port}";

            var page = GetPageBase(driver, device);
            page.WaitForNoSpinner();

            driver.ExecuteScript("window.Lidarr.NameViews = true;");

            GetPageErrors().Should().BeEmpty();
        }

        private IPAddress LocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private PageBase GetPageBase(RemoteWebDriver driver, string device)
        {
            if (device.IsNullOrWhiteSpace())
            {
                return new PageBase(driver);
            }
            else
            {
                return new PageBaseMobile(driver);
            }
        }

        [SetUp]
        public override void Setup()
        {
            page = GetPageBase(driver, device);
        }

        [OneTimeTearDown]
        public override void SmokeTestTearDown()
        {
            driver?.Quit();
            _browserStackLocalProcess?.Kill();
            _runner?.Kill();
        }

        private Process StartBrowserStackLocal(string tempDir, string args = null)
        {
            string url;
            string name;

            if (OsInfo.IsWindows)
            {
                url = "https://www.browserstack.com/browserstack-local/BrowserStackLocal-win32.zip";
                name = "BrowserStackLocal.exe";
            }
            else if (OsInfo.IsOsx)
            {
                url = "https://www.browserstack.com/browserstack-local/BrowserStackLocal-darwin-x64.zip";
                name = "BrowserStackLocal";
            }
            else
            {
                url = "https://www.browserstack.com/browserstack-local/BrowserStackLocal-linux-x64.zip";
                name = "BrowserStackLocal";
            }

            var dest = Path.Combine(tempDir, "browserstack.zip");
            TestContext.Progress.WriteLine("Fetching browserstack local");

            using (var client = new WebClient())
            {
                client.DownloadFile(url, dest);
            }
            ZipFile.ExtractToDirectory(dest, tempDir);

            var browserStack = Path.Combine(tempDir, name);

            if (OsInfo.IsNotWindows)
            {
                Syscall.chmod(browserStack, FilePermissions.DEFFILEMODE | FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH);
            }

            TestContext.Progress.WriteLine("Starting browserstack local");

            var processStarted = new ManualResetEventSlim();

            var process = _processProvider.Start(browserStack, args, onOutputDataReceived: (string data) => {
                    TestContext.Progress.WriteLine(data);
                    if (data.Contains("You can now access your local server"))
                    {
                        processStarted.Set();
                    }
                });

            if (!processStarted.Wait(5000))
            {
                Assert.Fail("Failed to start browserstack within 5 sec");
            }

            TestContext.Progress.WriteLine($"Successfully started browserstacklocal pid {process.Id}");

            return process;
        }
    }
}
