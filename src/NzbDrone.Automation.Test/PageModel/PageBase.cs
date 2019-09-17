using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace NzbDrone.Automation.Test.PageModel
{
    public class PageBase
    {
        protected readonly RemoteWebDriver _driver;

        public PageBase(RemoteWebDriver driver)
        {
            _driver = driver;
            MaximizeWindow();
        }

        public virtual void MaximizeWindow()
        {
            _driver.Manage().Window.Maximize();
        }

        public virtual void OpenSidebar()
        {
        }

        public virtual void CloseSidebar()
        {
        }

        public IWebElement FindByClass(string className, int timeout = 5)
        {
            return Find(By.ClassName(className), timeout);
        }

        public IWebElement Find(By by, int timeout = 5)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeout));
            return wait.Until(d => d.FindElement(by));
        }

        public void WaitForNoSpinner(int timeout = 30)
        {
            //give the spinner some time to show up.
            Thread.Sleep(200);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeout));
            wait.Until(d =>
            {
                try
                {
                    IWebElement element = d.FindElement(By.Id("followingBalls"));
                    return !element.Displayed;
                }
                catch (NoSuchElementException)
                {
                    return true;
                }
            });
        }

        public virtual IWebElement LibraryNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.LinkText("Library"));
            }
        }

        public virtual IWebElement CalendarNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.LinkText("Calendar"));
            }
        }

        public virtual IWebElement ActivityNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.LinkText("Activity"));
            }
        }

        public virtual IWebElement WantedNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.LinkText("Wanted"));
            }
        }

        public virtual IWebElement SettingNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.LinkText("Setting"));
            }
        }

        public virtual IWebElement SystemNavIcon
        {
            get
            {
                OpenSidebar();
                return Find(By.PartialLinkText("System"));
            }
        }
    }
}
