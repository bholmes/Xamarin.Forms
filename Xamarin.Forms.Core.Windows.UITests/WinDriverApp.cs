using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using Xamarin.UITest.Queries;
using Xamarin.Forms.Core.UITests.Appium;

namespace Xamarin.Forms.Core.UITests
{
	internal class WinDriverApp : AppiumDriverApp<WindowsDriver<WindowsElement>, WindowsElement>
	{
		public const string AppName = "Xamarin.Forms.ControlGallery.WindowsUniversal";
		protected int _scrollBarOffset = 5;

		WindowsElement _viewPort;
		WindowsElement _window;

		public WinDriverApp(WindowsDriver<WindowsElement> session) :
			base (
				controlNameToTag: new Dictionary<string, string>
				{
					{ "button", "ControlType.Button" }
				},
				translatePropertyAccessor: new Dictionary<string, string>
				{
					{ "getAlpha", "Opacity" },
					{ "isEnabled", "IsEnabled" }
				})
		{
			Init(session);
		}

		protected void Init(WindowsDriver<WindowsElement> session)
		{
			Session = session;
			TestServer = new WindowsTestServer(Session, this);
		}

		public void RestartFromCrash()
		{
			Init(WindowsTestBase.CreateWindowsDriver());
		}

		public void RestartApp()
		{
			Session.CloseApp();
			Init(WindowsTestBase.CreateWindowsDriver());
		}

		public bool RestartIfAppIsClosed()
		{
			try
			{
				var handle = Session.CurrentWindowHandle;
				return false;
			}
			catch
			{
				RestartFromCrash();
			}

			return true;
		}

		public override void DismissKeyboard()
		{
			// No-op for Desktop, which is all we're doing right now
		}

		protected override WindowsElement SwapInUsefulElement(WindowsElement element)
		{
			// AutoSuggestBox on UWP has some interaction issues with WebDriver
			// The AutomationID is set on the control group not the actual TextBox
			// This retrieves the actual TextBox which makes the behavior more consistent
			var isAutoSuggest = element?.FindElementsByXPath("//*[contains(@AutomationId,'_AutoSuggestBox')]")?.FirstOrDefault() as WindowsElement;
			return isAutoSuggest ?? element;
		}

		public override object Invoke(string methodName, object[] arguments)
		{
			if (methodName == "ContextClick")
			{
				// The IApp interface doesn't have a context click concept, and mapping TouchAndHold to 
				// context clicking would box us in if we have the option of running these tests on touch
				// devices later. So we're going to use the back door.
				ContextClick(arguments[0].ToString());
				return null;
			}

			return base.Invoke(methodName, arguments);
		}

		public override void SetOrientationLandscape()
		{
			// Deliberately leaving this as a no-op for now
			// Trying to set the orientation on the Desktop (the only version of UWP we're testing for the moment)
			// gives us a 405 Method Not Allowed, which makes sense. Haven't figured out how to determine
			// whether we're in a mode which allows orientation, but if we were, the next line is probably how to set it.
			//_session.Orientation = ScreenOrientation.Landscape;
		}

		public override void SetOrientationPortrait()
		{
			// Deliberately leaving this as a no-op for now
			// Trying to set the orientation on the Desktop (the only version of UWP we're testing for the moment)
			// gives us a 405 Method Not Allowed, which makes sense. Haven't figured out how to determine
			// whether we're in a mode which allows orientation, but if we were, the next line is probably how to set it.
			//_session.Orientation = ScreenOrientation.Portrait;
		}

		public override void TapCoordinates(float x, float y)
		{
			// Okay, this one's a bit complicated. For some reason, _session.Tap() with coordinates does not work
			// (Filed https://github.com/Microsoft/WinAppDriver/issues/229 for that)
			// But we can do the equivalent by manipulating the mouse. The mouse methods all take an ICoordinates
			// object, and you'd think that the "coordinates" part of ICoordinates would have something do with 
			// where the mouse clicks. You'd be wrong. The coordinates parts of that object are ignored and it just
			// clicks the center of whatever WindowsElement the ICoordinates refers to in 'AuxiliaryLocator'

			// If we could just use the element, we wouldn't be tapping at specific coordinates, so that's not 
			// very helpful.

			// Instead, we'll use MouseClickAt

			MouseClickAt(x, y);
		}


		public AppResult WaitForFirstElement(string marked, string timeoutMessage = "Timed out waiting for element...",
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null)
		{
			Func<ReadOnlyCollection<WindowsElement>> result = () => QueryWindows(marked, true);
			return WaitForAtLeastOne(result, timeoutMessage, timeout, retryFrequency)
				.Select(ToAppResult)
				.FirstOrDefault();
		}

		void ContextClick(string marked)
		{
			WindowsElement element = QueryWindows(marked, true).First();
			PointF point = ElementToClickablePoint(element);

			MouseClickAt(point.X, point.Y, ClickType.ContextClick);
		}

		internal void MouseClickAt(float x, float y, ClickType clickType = ClickType.SingleClick)
		{
			// Mouse clicking with ICoordinates doesn't work the way we'd like (see TapCoordinates comments),
			// so we have to do some math on our own to get the mouse in the right spot

			// So here's how we're working around it for the moment:
			// 1. Get the Window viewport (which is a known-to-exist element)
			// 2. Using the Window's ICoordinates and the MouseMove() overload with x/y offsets, move the pointer
			//		to the location we care about
			// 3. Use the (undocumented, except in https://github.com/Microsoft/WinAppDriver/issues/118#issuecomment-269404335)
			//		null parameter for Mouse.Click() to click at the current pointer location

			WindowsElement viewPort = GetViewPort();
			int xOffset = viewPort.Coordinates.LocationInViewport.X;
			int yOffset = viewPort.Coordinates.LocationInViewport.Y;

			var actions = new Actions(Session)
					   .MoveToElement(viewPort, (int)x - xOffset, (int)y - yOffset);

			switch (clickType)
			{
				case ClickType.DoubleClick:
					actions.DoubleClick();
					break;
				case ClickType.ContextClick:
					actions.ContextClick();
					break;
				case ClickType.SingleClick:
				default:
					actions.Click();
					break;
			}

			actions.Perform();
		}

		protected override void ClickOrTapElement(WindowsElement element)
		{
			try
			{
				// For most stuff, a simple click will work
				element.Click();
			}
			catch (InvalidOperationException)
			{
				ProcessException();
			}
			catch (WebDriverException)
			{
				ProcessException();
			}

			void ProcessException()
			{
				// Some elements aren't "clickable" from an automation perspective (e.g., Frame renders as a Border
				// with content in it; if the content is just a TextBlock, we'll end up here)

				// All is not lost; we can figure out the location of the element in in the application window
				// and Tap in that spot
				PointF p = ElementToClickablePoint(element);
				TapCoordinates(p.X, p.Y);
			}
		}

		void DoubleClickElement(WindowsElement element)
		{
			PointF point = ElementToClickablePoint(element);

			MouseClickAt(point.X, point.Y, clickType: ClickType.DoubleClick);
		}

		protected override void DoubleTap(AppiumQuery query)
		{
			WindowsElement element = FindFirstElement(query);

			if (element == null)
			{
				return;
			}

			DoubleClickElement(element);
		}

		PointF ElementToClickablePoint(WindowsElement element)
		{
			PointF clickablePoint = GetClickablePoint(element);

			WindowsElement window = GetWindow();
			PointF origin = GetOriginOfBoundingRectangle(window);

			// Use the coordinates in the app window's viewport relative to the window's origin
			return new PointF(clickablePoint.X - origin.X, clickablePoint.Y - origin.Y);
		}

		static PointF GetBottomRightOfBoundingRectangle(WindowsElement element)
		{
			string vpcpString = element.GetAttribute("BoundingRectangle");

			// returned string format looks like:
			// Left:-1868 Top:382 Width:1013 Height:680

			string[] vpparts = vpcpString.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			float vpx = float.Parse(vpparts[1]);
			float vpy = float.Parse(vpparts[3]);

			float vpw = float.Parse(vpparts[5]);
			float vph = float.Parse(vpparts[7]);

			return new PointF(vpx + vpw, vpy + vph);
		}

		static PointF GetClickablePoint(WindowsElement element)
		{
			string cpString = element.GetAttribute("ClickablePoint");
			string[] parts = cpString.Split(',');
			float x = float.Parse(parts[0]);
			float y = float.Parse(parts[1]);

			return new PointF(x, y);
		}

		static PointF GetOriginOfBoundingRectangle(WindowsElement element)
		{
			string vpcpString = element.GetAttribute("BoundingRectangle");

			// returned string format looks like:
			// Left:-1868 Top:382 Width:1013 Height:680

			string[] vpparts = vpcpString.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			float vpx = float.Parse(vpparts[1]);
			float vpy = float.Parse(vpparts[3]);

			return new PointF(vpx, vpy);
		}

		static PointF GetTopRightOfBoundingRectangle(WindowsElement element)
		{
			string vpcpString = element.GetAttribute("BoundingRectangle");

			// returned string format looks like:
			// Left:-1868 Top:382 Width:1013 Height:680

			string[] vpparts = vpcpString.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			float vpx = float.Parse(vpparts[1]);
			float vpy = float.Parse(vpparts[3]);

			float vpw = float.Parse(vpparts[5]);

			return new PointF(vpx + vpw, vpy);
		}

		WindowsElement GetViewPort()
		{
			if (_viewPort != null)
			{
				return _viewPort;
			}

			ReadOnlyCollection<WindowsElement> candidates = QueryWindows(AppName);

			// When you go full screen there are less candidates because certain elements go away on the window
			if (candidates.Count >= 4)
				_viewPort = candidates[3]; // We really just want the viewport; skip the full window, title bar, min/max buttons...
			else
				_viewPort = candidates.Last();

			int xOffset = _viewPort.Coordinates.LocationInViewport.X;

			if (xOffset > 1) // Everything having to do with scrolling right now is a horrid kludge
			{
				// This makes the scrolling stuff work correctly on a higher density screen (e.g. MBP running Windows) 
				_scrollBarOffset = -70;
			}

			return _viewPort;
		}

		WindowsElement GetWindow()
		{
			if (_window != null)
			{
				return _window;
			}

			_window = QueryWindows(AppName)[0];
			return _window;
		}

		void OriginMouse()
		{
			WindowsElement viewPort = GetViewPort();
			int xOffset = viewPort.Coordinates.LocationInViewport.X;
			int yOffset = viewPort.Coordinates.LocationInViewport.Y;
			new Actions(Session).MoveToElement(viewPort, xOffset, yOffset);
		}

		protected override void Scroll(AppiumQuery query, bool down)
		{
			if (query == null)
			{
				ScrollClick(GetWindow(), down);
				return;
			}

			WindowsElement element = FindFirstElement(query);

			ScrollClick(element, down);
		}

		void ScrollClick(WindowsElement element, bool down = true)
		{
			PointF point = down ? GetBottomRightOfBoundingRectangle(element) : GetTopRightOfBoundingRectangle(element);

			PointF origin = GetOriginOfBoundingRectangle(GetWindow());

			var realPoint = new PointF(point.X - origin.X, point.Y - origin.Y);

			int xOffset = _scrollBarOffset;
			if (origin.X < 0)
			{
				// The scrollbar's in a slightly different place relative to the window bounds
				// if we're running on the left monitor (which I like to do)
				xOffset = xOffset * 3;
			}

			float finalX = realPoint.X - xOffset;
			float finalY = realPoint.Y - (down ? 15 : -15);

			OriginMouse();
			MouseClickAt(finalX, finalY, ClickType.SingleClick);
		}

		protected override void ScrollTo(AppiumQuery toQuery, AppiumQuery withinQuery, TimeSpan? timeout = null, bool down = true)
		{
			timeout = timeout ?? DefaultTimeout;
			DateTime start = DateTime.Now;

			while (true)
			{
				Func<ReadOnlyCollection<WindowsElement>> result = () => QueryWindows(toQuery);
				TimeSpan iterationTimeout = TimeSpan.FromMilliseconds(0);
				TimeSpan retryFrequency = TimeSpan.FromMilliseconds(0);

				try
				{
					ReadOnlyCollection<WindowsElement> found = WaitForAtLeastOne(result, timeoutMessage: null,
						timeout: iterationTimeout, retryFrequency: retryFrequency);

					if (found.Count > 0)
					{
						// Success
						return;
					}
				}
				catch (TimeoutException ex)
				{
					// Haven't found it yet, keep scrolling
				}

				long elapsed = DateTime.Now.Subtract(start).Ticks;
				if (elapsed >= timeout.Value.Ticks)
				{
					Debug.WriteLine($">>>>> {elapsed} ticks elapsed, timeout value is {timeout.Value.Ticks}");
					throw new TimeoutException($"Timed out scrolling to {toQuery}");
				}

				Scroll(withinQuery, down);
			}
		}

		internal enum ClickType
		{
			SingleClick,
			DoubleClick,
			ContextClick
		}
	}
}
