using System;
using OpenQA.Selenium.Appium.iOS;
using Xamarin.Forms.Core.UITests.Appium;

namespace Xamarin.Forms.Core.UITests
{
	internal class IOSTestServer : AppiumTestServer
	{
		readonly IOSDriver<IOSElement> _session;
		readonly IOSDriverApp _winDriverApp;

		public IOSTestServer(IOSDriver<IOSElement> session, IOSDriverApp winDriverApp)
		{
			_session = session;
			_winDriverApp = winDriverApp;
		}

		public override string Get(string endpoint)
		{
			if (endpoint == "version")
			{
				var ret = _session.Capabilities.GetCapability("platformVersion");
				return ret.ToString();
			}

			return base.Get(endpoint);
		}
	}
}