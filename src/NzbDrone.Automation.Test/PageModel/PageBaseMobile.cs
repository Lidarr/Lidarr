using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace NzbDrone.Automation.Test.PageModel
{
    public class PageBaseMobile : PageBase
    {
        public PageBaseMobile(RemoteWebDriver driver)
        : base(driver)
        {
        }

        public override void MaximizeWindow()
        {
        }

        public override void OpenSidebar()
        {
            // if (!SidebarIsOpen())
            // {
                ToggleSidebar();
            // }
        }

        public override void CloseSidebar()
        {
            // if (SidebarIsOpen())
            // {
                ToggleSidebar();
            // }
        }

        private void ToggleSidebar()
        {
            Find(By.Id("sidebar-toggle-button")).Click();
        }

        private bool SidebarIsOpen()
        {
            var sidebar = _driver.FindElement(By.CssSelector("div[class*='PageSidebar-sidebar']"));
            return sidebar != null;
        }
    }
}
