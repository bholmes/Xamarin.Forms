using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Interactions;
using Xamarin.UITest.Queries;
using Xamarin.Forms.Core.UITests.Appium;

namespace Xamarin.Forms.Core.UITests
{
	internal class IOSDriverApp : AppiumDriverApp<IOSDriver<IOSElement>, IOSElement>
	{
		public const string AppName = Xamarin.Forms.Controls.AppPaths.BundleId;
		protected int _scrollBarOffset = 5;

		IOSElement _viewPort;
		IOSElement _window;

		public IOSDriverApp(IOSDriver<IOSElement> session) :
			base (
				controlNameToTag: new Dictionary<string, string>
				{
					{ "button", "XCUIElementTypeButton" }
				},
				translatePropertyAccessor: new Dictionary<string, string>
				{
					{ "getAlpha", "Opacity" },
					{ "isEnabled", "IsEnabled" }
				})
		{
			Init(session);
		}

		protected void Init(IOSDriver<IOSElement> session)
		{
			Session = session;
			TestServer = new IOSTestServer(Session, this);
		}

		protected override void ClickOrTapElement(IOSElement element)
		{
			element?.Click();
		}

		public override object Invoke(string methodName, object[] arguments)
		{
			if (methodName == "iOSVersion")
			{
				var fullVersion = Session.Capabilities.GetCapability("platformVersion").ToString();
				var versionPart = fullVersion.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				return int.Parse(versionPart[0]);
			}

			return base.Invoke(methodName, arguments);
		}

		protected override void DoubleTap(AppiumQuery query)
		{
			throw new NotImplementedException();
		}

		protected override void Scroll(AppiumQuery query, bool down)
		{
			throw new NotImplementedException();
		}

		protected override void ScrollTo(AppiumQuery toQuery, AppiumQuery withinQuery, TimeSpan? timeout = null, bool down = true)
		{
			throw new NotImplementedException();
		}


	}
}
