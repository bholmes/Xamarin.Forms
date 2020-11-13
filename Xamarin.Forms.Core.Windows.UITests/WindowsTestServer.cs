using System;
using OpenQA.Selenium.Appium.Windows;
using Xamarin.Forms.Controls.Issues;
using Xamarin.UITest;
using Xamarin.Forms.Core.UITests.Appium;

namespace Xamarin.Forms.Core.UITests
{
	internal class WindowsTestServer : AppiumTestServer
	{
		readonly WindowsDriver<WindowsElement> _session;
		readonly WinDriverApp _winDriverApp;

		public WindowsTestServer(WindowsDriver<WindowsElement> session, WinDriverApp winDriverApp)
		{
			_session = session;
			_winDriverApp = winDriverApp;
		}

		public override string Get(string endpoint)
		{
			if (endpoint == "version")
			{
				try
				{
					return _session.CurrentWindowHandle;
				}
				catch (OpenQA.Selenium.WebDriverException we)
				when (we.IsWindowClosedException())
				{
					_winDriverApp.RestartFromCrash();
				}
				catch (Exception exception)
				{
					WindowsTestBase.HandleAppClosed(exception);
					throw;
				}
			}

			return endpoint;
		}
	}
}