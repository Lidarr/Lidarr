using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NzbDrone.Automation.Test.PageModel;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Test.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    [AutomationTest]
    public abstract class AutomationTest
    {
        protected NzbDroneRunner _runner;
        protected RemoteWebDriver driver;

        public AutomationTest()
        {
            string username = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
            string accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
            string testType = this.GetType().Name;

            if (username.IsNotNullOrWhiteSpace() && accessKey.IsNotNullOrWhiteSpace() && !testType.Contains("BrowserStack"))
            {
                Assert.Ignore("BrowserStack Tests Enabled, Don't Run Normal Automation Tests");
            }

            new StartupContext();

            LogManager.Configuration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget { Layout = "${level}: ${message} ${exception}" };
            LogManager.Configuration.AddTarget(consoleTarget.GetType().Name, consoleTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, consoleTarget));
        }

        [OneTimeSetUp]
        public virtual void SmokeTestSetup()
        {
            var options = new FirefoxOptions();
            options.AddArguments("--headless");
            driver = new FirefoxDriver(options);

            _runner = new NzbDroneRunner(LogManager.GetCurrentClassLogger());
            _runner.KillAll();
            _runner.Start();

            driver.Url = "http://localhost:8686";

            var page = new PageBase(driver);
            page.WaitForNoSpinner();

            driver.ExecuteScript("window.Lidarr.NameViews = true;");

            GetPageErrors().Should().BeEmpty();
        }

        protected IEnumerable<string> GetPageErrors()
        {
            return driver?.FindElements(By.CssSelector("#errors div"))
                .Select(e => e.Text);
        }

        [OneTimeTearDown]
        public virtual void SmokeTestTearDown()
        {
            _runner?.KillAll();
            driver?.Quit();
        }

        [TearDown]
        public void AutomationTearDown()
        {
            GetPageErrors().Should().BeNullOrEmpty();
        }
    }
}
