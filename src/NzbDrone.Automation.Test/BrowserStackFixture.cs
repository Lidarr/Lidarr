using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    [TestFixture("","Windows","10","Chrome", "63")]
    [TestFixture("", "Windows","10", "Firefox", "67")]
    [TestFixture("", "Windows","10", "Edge", "18")]
    [TestFixture("iPhone X", "", "11", "iPhone", "")]
    [TestFixture("Samsung Galaxy S9 Plus", "", "9.0", "android", "")]
    public class BSMainPagesTest : BrowserStackAutomationTest
    {
        public BSMainPagesTest(string device, string os, string osVersion, string browser, string browserVersion) : 
            base(device, os, osVersion, browser, browserVersion) { }

    }
}