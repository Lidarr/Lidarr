using NUnit.Framework;

namespace NzbDrone.Automation.Test
{
    [TestFixture("", "Windows", "10", "Chrome", "63", 9901)]
    [TestFixture("", "Windows", "10", "Firefox", "67", 9902)]
    [TestFixture("", "Windows", "10", "Edge", "18", 9903)]
    [TestFixture("", "OS X", "Mojave", "Safari", "12.1", 9904)]
    // [TestFixture("iPhone X", "", "11", "iPhone", "", 9905)]
    // [TestFixture("Samsung Galaxy S9 Plus", "", "9.0", "android", "", 9906)]
    public class BrowserStackFixture : BrowserStackAutomationTest
    {
        public BrowserStackFixture(string device, string os, string osVersion, string browser, string browserVersion, int port) :
        base(device, os, osVersion, browser, browserVersion, port)
        {
        }
    }
}
