using System;
using System.Collections.Generic;
using BrowserStack;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Automation.Test.PageModel;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
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
        private Local browserStackLocal;

        public BrowserStackAutomationTest(string device, string os, string osVersion, string browser, string browserVersion)
        {
            this.device = device;
            this.browser = browser;
            this.browserVersion = browserVersion;
            this.os = os;
            this.osVersion = osVersion;
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

            string browserstackLocal = "true";
            string browserstackLocalIdentifier = string.Format("Lidarr_{0}_{1}", DateTime.UtcNow.Ticks, new Random().Next());
            string buildName = BuildInfo.Version.ToString();

            DesiredCapabilities capabilities = new DesiredCapabilities();

            capabilities.SetCapability("device", device);
            capabilities.SetCapability("os", os);
            capabilities.SetCapability("os_version", osVersion);
            capabilities.SetCapability("browser", browser);
            capabilities.SetCapability("browser_version", browserVersion);
            capabilities.SetCapability("browserstack.local", browserstackLocal);
            capabilities.SetCapability("browserstack.localIdentifier", browserstackLocalIdentifier);
            capabilities.SetCapability("browserstack.debug", "true");
            capabilities.SetCapability("name", "Function Tests: " + browser);
            capabilities.SetCapability("project", "Lidarr");
            capabilities.SetCapability("build", buildName);

            browserStackLocal = new Local();
            List<KeyValuePair<string, string>> bsLocalArgs = new List<KeyValuePair<string, string>>();
            bsLocalArgs.Add(new KeyValuePair<string, string>("key", accessKey));
            bsLocalArgs.Add(new KeyValuePair<string, string>("localIdentifier", browserstackLocalIdentifier));
            browserStackLocal.start(bsLocalArgs);

            driver = new RemoteWebDriver(new Uri("https://" + username + ":" + accessKey + "@hub.browserstack.com/wd/hub"), capabilities);

            driver.Url = "http://localhost:8686";

            var page = new PageBase(driver);
            page.WaitForNoSpinner();

            driver.ExecuteScript("window.Lidarr.NameViews = true;");

            GetPageErrors().Should().BeEmpty();
        }

        [OneTimeTearDown]
        public override void SmokeTestTearDown()
        {
            driver.Quit();
            if (browserStackLocal != null)
            {
                browserStackLocal.stop();
            }
        }
    }
}
