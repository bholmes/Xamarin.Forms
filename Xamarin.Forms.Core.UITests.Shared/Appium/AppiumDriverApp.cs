using System;
using System.IO;
using Xamarin.UITest;
using Xamarin.UITest.Queries;
using OpenQA.Selenium.Appium;
using System.Collections.Generic;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;
using System.Linq;
using OpenQA.Selenium;
using System.Reflection;
using Xamarin.UITest.Queries.Tokens;
using System.Diagnostics;
using Xamarin.Forms.Controls.Issues;
using System.Threading.Tasks;

namespace Xamarin.Forms.Core.UITests.Appium
{
	internal abstract class AppiumDriverApp<_AppiumDriver, _AppiumWebElement> : IApp
            where _AppiumDriver : AppiumDriver<_AppiumWebElement>
            where _AppiumWebElement : AppiumWebElement
    {
		public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

		readonly Dictionary<string, string> _controlNameToTag;
		readonly Dictionary<string, string> _translatePropertyAccessor;

		protected AppiumDriverApp(Dictionary<string, string> controlNameToTag, Dictionary<string, string> translatePropertyAccessor)
		{
			_controlNameToTag = controlNameToTag;
			_translatePropertyAccessor = translatePropertyAccessor;
		}

		#region Xamarin.UITest

		public virtual AppPrintHelper Print => throw new NotImplementedException();

		public virtual IDevice Device => throw new NotImplementedException();

		public virtual ITestServer TestServer { get; protected set; }

		public virtual void Back()
		{
			QueryWindows("Back", true).First().Click();
		}

		public virtual void ClearText(Func<AppQuery, AppQuery> query)
		{
			SwapInUsefulElement(QueryWindows(query, true).First()).Clear();
		}

		public virtual void ClearText(Func<AppQuery, AppWebQuery> query)
		{
			throw new NotImplementedException();
		}

		public virtual void ClearText(string marked)
		{
			SwapInUsefulElement(QueryWindows(marked, true).First()).Clear();
		}

		public virtual void ClearText()
		{
			throw new NotImplementedException();
		}

		public virtual void DismissKeyboard()
		{
			throw new NotImplementedException();
		}

		public virtual void DoubleTap(Func<AppQuery, AppQuery> query)
		{
			DoubleTap(AppiumQuery.FromQuery(query));
		}

		public virtual void DoubleTap(string marked)
		{
			DoubleTap(AppiumQuery.FromMarked(marked));
		}

		public virtual void DoubleTapCoordinates(float x, float y)
		{
			throw new NotImplementedException();
		}

		public virtual void DragAndDrop(Func<AppQuery, AppQuery> from, Func<AppQuery, AppQuery> to)
		{
			DragAndDrop(
				FindFirstElement(AppiumQuery.FromQuery(from)),
				FindFirstElement(AppiumQuery.FromQuery(to))
			);
		}

		public virtual void DragAndDrop(string from, string to)
		{
			DragAndDrop(
				FindFirstElement(AppiumQuery.FromMarked(from)),
				FindFirstElement(AppiumQuery.FromMarked(to))
			);
		}

		public virtual void DragCoordinates(float fromX, float fromY, float toX, float toY)
		{
			throw new NotImplementedException();
		}

		public virtual void EnterText(string text)
		{
			new Actions(Session)
					.SendKeys(text)
					.Perform();
		}

		public virtual void EnterText(Func<AppQuery, AppQuery> query, string text)
		{
			var result = QueryWindows(query, true).First();
			SwapInUsefulElement(result).SendKeys(text);
		}

		public virtual void EnterText(string marked, string text)
		{
			var result = QueryWindows(marked, true).First();
			SwapInUsefulElement(result).SendKeys(text);
		}

		public virtual void EnterText(Func<AppQuery, AppWebQuery> query, string text)
		{
			throw new NotImplementedException();
		}

		public virtual AppResult[] Flash(Func<AppQuery, AppQuery> query = null)
		{
			throw new NotImplementedException();
		}

		public virtual AppResult[] Flash(string marked)
		{
			throw new NotImplementedException();
		}

		public virtual object Invoke(string methodName, object argument = null)
		{
			return Invoke(methodName, new[] { argument });
		}

		public virtual object Invoke(string methodName, object[] arguments)
		{
			if (methodName == "hasInternetAccess")
			{
				try
				{
					using (var httpClient = new System.Net.Http.HttpClient())
					using (var httpResponse = httpClient.GetAsync(@"https://www.github.com"))
					{
						httpResponse.Wait();
						if (httpResponse.Result.StatusCode == System.Net.HttpStatusCode.OK)
							return true;
						else
							return false;
					}
				}
				catch
				{
					return false;
				}
			}

			return null;
		}

		public virtual void PinchToZoomIn(Func<AppQuery, AppQuery> query, TimeSpan? duration = null)
		{
			throw new NotImplementedException();
		}

		public virtual void PinchToZoomIn(string marked, TimeSpan? duration = null)
		{
			throw new NotImplementedException();
		}

		public virtual void PinchToZoomInCoordinates(float x, float y, TimeSpan? duration)
		{
			throw new NotImplementedException();
		}

		public virtual void PinchToZoomOut(Func<AppQuery, AppQuery> query, TimeSpan? duration = null)
		{
			throw new NotImplementedException();
		}

		public virtual void PinchToZoomOut(string marked, TimeSpan? duration = null)
		{
			throw new NotImplementedException();
		}

		public virtual void PinchToZoomOutCoordinates(float x, float y, TimeSpan? duration)
		{
			throw new NotImplementedException();
		}

		public virtual void PressEnter()
		{
			new Actions(Session)
					   .SendKeys(Keys.Enter)
					   .Perform();
		}

		public virtual void PressVolumeDown()
		{
			throw new NotImplementedException();
		}

		public virtual void PressVolumeUp()
		{
			throw new NotImplementedException();
		}

		public virtual AppResult[] Query(Func<AppQuery, AppQuery> query = null)
		{
			ReadOnlyCollection<_AppiumWebElement> elements = QueryWindows(AppiumQuery.FromQuery(query));
			return elements.Select(ToAppResult).ToArray();
		}

		public virtual AppResult[] Query(string marked)
		{
			ReadOnlyCollection<_AppiumWebElement> elements = QueryWindows(marked);
			return elements.Select(ToAppResult).ToArray();
		}

		public virtual AppWebResult[] Query(Func<AppQuery, AppWebQuery> query)
		{
			throw new NotImplementedException();
		}

		public virtual T[] Query<T>(Func<AppQuery, AppTypedSelector<T>> query)
		{
			AppTypedSelector<T> appTypedSelector = query(new AppQuery(QueryPlatform.iOS));

			// Swiss-Army Chainsaw time
			// We'll use reflection to dig into the query and get the element selector 
			// and the property value invocation in text form
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
			Type selectorType = appTypedSelector.GetType();
			PropertyInfo tokensProperty = selectorType.GetProperties(bindingFlags)
				.First(t => t.PropertyType == typeof(IQueryToken[]));

			var tokens = (IQueryToken[])tokensProperty.GetValue(appTypedSelector);

			string selector = tokens[0].ToQueryString(QueryPlatform.iOS);
			string invoke = tokens[1].ToCodeString();

			// Now that we have them in text form, we can reinterpret them for Windows
			AppiumQuery winQuery = AppiumQuery.FromRaw(selector);
			// TODO hartez 2017/07/19 17:08:44 Make this a bit more resilient if the translation isn't there	
			var translationKey = invoke.Substring(8).Replace("\")", "");

			if (!_translatePropertyAccessor.ContainsKey(translationKey))
				throw new Exception($"{translationKey} not found please add to _translatePropertyAccessor");

			string attribute = _translatePropertyAccessor[translationKey];

			ReadOnlyCollection<_AppiumWebElement> elements = QueryWindows(winQuery);

			foreach (_AppiumWebElement e in elements)
			{
				string x = e.GetAttribute(attribute);
				Debug.WriteLine($">>>>> WinDriverApp Query 261: {x}");
			}

			// TODO hartez 2017/07/19 17:09:14 Alas, for now this simply doesn't work. Waiting for WinAppDriver to implement it	
			return elements.Select(e => (T)Convert.ChangeType(e.GetAttribute(attribute), typeof(T))).ToArray();
		}

		public virtual string[] Query(Func<AppQuery, InvokeJSAppQuery> query)
		{
			throw new NotImplementedException();
		}

		public virtual void Repl()
		{
			throw new NotImplementedException();
		}

		public virtual FileInfo Screenshot(string title)
		{
			try
			{
				string filename = $"{title}.png";
				Screenshot screenshot = Session.GetScreenshot();
				screenshot.SaveAsFile(filename, ScreenshotImageFormat.Png);
				var file = new FileInfo(filename);
				return file;
			}
			catch (OpenQA.Selenium.WebDriverException we)
			when (we.IsWindowClosedException())
			{
				return null;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public virtual void ScrollDown(Func<AppQuery, AppQuery> withinQuery = null, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67,
			int swipeSpeed = 500, bool withInertia = true)
		{
			if (withinQuery == null)
			{
				Scroll(null, true);
				return;
			}

			AppiumQuery winQuery = AppiumQuery.FromQuery(withinQuery);
			Scroll(winQuery, true);
		}

		public virtual void ScrollDown(string withinMarked, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67,
			int swipeSpeed = 500, bool withInertia = true)
		{
			AppiumQuery winQuery = AppiumQuery.FromMarked(withinMarked);
			Scroll(winQuery, true);
		}

		public virtual void ScrollDownTo(string toMarked, string withinMarked = null, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			ScrollTo(AppiumQuery.FromMarked(toMarked), withinMarked == null ? null : AppiumQuery.FromMarked(withinMarked), timeout);
		}

		public virtual void ScrollDownTo(Func<AppQuery, AppWebQuery> toQuery, string withinMarked, ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void ScrollDownTo(Func<AppQuery, AppQuery> toQuery, Func<AppQuery, AppQuery> withinQuery = null,
			ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67,
			int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			ScrollTo(AppiumQuery.FromQuery(toQuery), withinQuery == null ? null : AppiumQuery.FromQuery(withinQuery), timeout);
		}

		public virtual void ScrollDownTo(Func<AppQuery, AppWebQuery> toQuery, Func<AppQuery, AppQuery> withinQuery = null, ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void ScrollTo(string toMarked, string withinMarked = null, ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void ScrollUp(Func<AppQuery, AppQuery> query = null, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67, int swipeSpeed = 500,
			bool withInertia = true)
		{
			if (query == null)
			{
				Scroll(null, false);
				return;
			}

			AppiumQuery winQuery = AppiumQuery.FromQuery(query);
			Scroll(winQuery, false);
		}

		public virtual void ScrollUp(string withinMarked, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67, int swipeSpeed = 500,
			bool withInertia = true)
		{
			AppiumQuery winQuery = AppiumQuery.FromMarked(withinMarked);
			Scroll(winQuery, false);
		}

		public virtual void ScrollUpTo(string toMarked, string withinMarked = null, ScrollStrategy strategy = ScrollStrategy.Auto,
			double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			ScrollTo(AppiumQuery.FromMarked(toMarked), withinMarked == null ? null : AppiumQuery.FromMarked(withinMarked), timeout,
				down: false);
		}

		public virtual void ScrollUpTo(Func<AppQuery, AppWebQuery> toQuery, string withinMarked, ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void ScrollUpTo(Func<AppQuery, AppQuery> toQuery, Func<AppQuery, AppQuery> withinQuery = null,
			ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67,
			int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			ScrollTo(AppiumQuery.FromQuery(toQuery), withinQuery == null ? null : AppiumQuery.FromQuery(withinQuery), timeout,
				down: false);
		}

		public virtual void ScrollUpTo(Func<AppQuery, AppWebQuery> toQuery, Func<AppQuery, AppQuery> withinQuery = null, ScrollStrategy strategy = ScrollStrategy.Auto, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true, TimeSpan? timeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void SetOrientationLandscape()
		{
			throw new NotImplementedException();
		}

		public virtual void SetOrientationPortrait()
		{
			throw new NotImplementedException();
		}

		public virtual void SetSliderValue(string marked, double value)
		{
			throw new NotImplementedException();
		}

		public virtual void SetSliderValue(Func<AppQuery, AppQuery> query, double value)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeLeftToRight(double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeLeftToRight(string marked, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeLeftToRight(Func<AppQuery, AppQuery> query, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeLeftToRight(Func<AppQuery, AppWebQuery> query, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeRightToLeft(double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeRightToLeft(string marked, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeRightToLeft(Func<AppQuery, AppQuery> query, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void SwipeRightToLeft(Func<AppQuery, AppWebQuery> query, double swipePercentage = 0.67, int swipeSpeed = 500, bool withInertia = true)
		{
			throw new NotImplementedException();
		}

		public virtual void Tap(Func<AppQuery, AppQuery> query)
		{
			AppiumQuery winQuery = AppiumQuery.FromQuery(query);
			Tap(winQuery);
		}

		public virtual void Tap(string marked)
		{
			AppiumQuery winQuery = AppiumQuery.FromMarked(marked);
			Tap(winQuery);
		}

		public virtual void Tap(Func<AppQuery, AppWebQuery> query)
		{
			throw new NotImplementedException();
		}

		public virtual void TapCoordinates(float x, float y)
		{
			throw new NotImplementedException();
		}

		public virtual void TouchAndHold(Func<AppQuery, AppQuery> query)
		{
			throw new NotImplementedException();
		}

		public virtual void TouchAndHold(string marked)
		{
			throw new NotImplementedException();
		}

		public virtual void TouchAndHoldCoordinates(float x, float y)
		{
			throw new NotImplementedException();
		}

		public virtual void WaitFor(Func<bool> predicate, string timeoutMessage = "Timed out waiting...", TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual AppResult[] WaitForElement(Func<AppQuery, AppQuery> query,
			string timeoutMessage = "Timed out waiting for element...",
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			Func<ReadOnlyCollection<_AppiumWebElement>> result = () => QueryWindows(query);
			return WaitForAtLeastOne(result, timeoutMessage, timeout, retryFrequency).Select(ToAppResult).ToArray();
		}

		public virtual AppResult[] WaitForElement(string marked, string timeoutMessage = "Timed out waiting for element...",
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			Func<ReadOnlyCollection<_AppiumWebElement>> result = () => QueryWindows(marked);
			return WaitForAtLeastOne(result, timeoutMessage, timeout, retryFrequency).Select(ToAppResult).ToArray();
		}

		public virtual AppWebResult[] WaitForElement(Func<AppQuery, AppWebQuery> query, string timeoutMessage = "Timed out waiting for element...", TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			throw new NotImplementedException();
		}

		public virtual void WaitForNoElement(Func<AppQuery, AppQuery> query,
			string timeoutMessage = "Timed out waiting for no element...",
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			Func<ReadOnlyCollection<_AppiumWebElement>> result = () => QueryWindows(query);
			WaitForNone(result, timeoutMessage, timeout, retryFrequency);
		}

		public virtual void WaitForNoElement(string marked, string timeoutMessage = "Timed out waiting for no element...",
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			Func<ReadOnlyCollection<_AppiumWebElement>> result = () => QueryWindows(marked);
			WaitForNone(result, timeoutMessage, timeout, retryFrequency);
		}

		public virtual void WaitForNoElement(Func<AppQuery, AppWebQuery> query, string timeoutMessage = "Timed out waiting for no element...", TimeSpan? timeout = null, TimeSpan? retryFrequency = null, TimeSpan? postTimeout = null)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected _AppiumDriver Session { get; set; }

		protected ReadOnlyCollection<_AppiumWebElement> QueryWindows(string marked, bool findFirst = false)
		{
			AppiumQuery winQuery = AppiumQuery.FromMarked(marked);
			return QueryWindows(winQuery, findFirst);
		}

		protected ReadOnlyCollection<_AppiumWebElement> QueryWindows(Func<AppQuery, AppQuery> query, bool findFirst = false)
		{
			AppiumQuery winQuery = AppiumQuery.FromQuery(query);
			return QueryWindows(winQuery, findFirst);
		}

		protected ReadOnlyCollection<_AppiumWebElement> QueryWindows(AppiumQuery query, bool findFirst = false)
		{
			var resultByAccessibilityId = Session.FindElementsByAccessibilityId(query.Marked);
			ReadOnlyCollection<_AppiumWebElement> resultByName = null;

			if (!findFirst || resultByAccessibilityId.Count == 0)
				resultByName = Session.FindElementsByName(query.Marked);

			IEnumerable<_AppiumWebElement> result = resultByAccessibilityId;

			if (resultByName != null)
				result = result.Concat(resultByName);

			// TODO hartez 2017/10/30 09:47:44 Should this be == "*" || == "TextBox"?	
			// what about other controls where we might be looking by content? TextBlock?
			if (query.ControlType == "*")
			{
				IEnumerable<_AppiumWebElement> textBoxesByContent =
					Session.FindElementsByClassName("TextBox").Where(e => e.Text == query.Marked);
				result = result.Concat(textBoxesByContent);
			}

			return FilterControlType(result, query.ControlType);
		}

		protected _AppiumWebElement FindFirstElement(AppiumQuery query)
		{
			Func<ReadOnlyCollection<_AppiumWebElement>> fquery =
				() => QueryWindows(query, true);

			string timeoutMessage = $"Timed out waiting for element: {query.Raw}";

			ReadOnlyCollection<_AppiumWebElement> results =
				WaitForAtLeastOne(fquery, timeoutMessage);

			_AppiumWebElement element = results.FirstOrDefault();

			return element;
		}

		protected static ReadOnlyCollection<_AppiumWebElement> Wait(Func<ReadOnlyCollection<_AppiumWebElement>> query,
			Func<int, bool> satisfactory,
			string timeoutMessage = null,
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null)
		{
			timeout = timeout ?? DefaultTimeout;
			retryFrequency = retryFrequency ?? TimeSpan.FromMilliseconds(500);
			timeoutMessage = timeoutMessage ?? "Timed out on query.";

			DateTime start = DateTime.Now;

			ReadOnlyCollection<_AppiumWebElement> result = query();

			while (!satisfactory(result.Count))
			{
				long elapsed = DateTime.Now.Subtract(start).Ticks;
				if (elapsed >= timeout.Value.Ticks)
				{
					Debug.WriteLine($">>>>> {elapsed} ticks elapsed, timeout value is {timeout.Value.Ticks}");

					throw new TimeoutException(timeoutMessage);
				}

				Task.Delay(retryFrequency.Value.Milliseconds).Wait();
				result = query();
			}

			return result;
		}

		protected static ReadOnlyCollection<_AppiumWebElement> WaitForAtLeastOne(Func<ReadOnlyCollection<_AppiumWebElement>> query,
			string timeoutMessage = null,
			TimeSpan? timeout = null,
			TimeSpan? retryFrequency = null)
		{
			return Wait(query, i => i > 0, timeoutMessage, timeout, retryFrequency);
		}

		protected void WaitForNone(Func<ReadOnlyCollection<_AppiumWebElement>> query,
			string timeoutMessage = null,
			TimeSpan? timeout = null, TimeSpan? retryFrequency = null)
		{
			Wait(query, i => i == 0, timeoutMessage, timeout, retryFrequency);
		}

		protected AppResult ToAppResult(_AppiumWebElement windowsElement)
		{
			return new AppResult
			{
				Rect = ToAppRect(windowsElement),
				Label = windowsElement.Id, // Not entirely sure about this one
				Description = SwapInUsefulElement(windowsElement).Text, // or this one
				Enabled = windowsElement.Enabled,
				Id = windowsElement.Id
			};
		}

		static AppRect ToAppRect(_AppiumWebElement windowsElement)
		{
			try
			{
				var result = new AppRect
				{
					X = windowsElement.Location.X,
					Y = windowsElement.Location.Y,
					Height = windowsElement.Size.Height,
					Width = windowsElement.Size.Width
				};

				result.CenterX = result.X + result.Width / 2;
				result.CenterY = result.Y + result.Height / 2;

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(
					$"Warning: error determining AppRect for {windowsElement}; "
					+ $"if this is a Label with a modified Text value, it might be confusing Windows automation. " +
					$"{ex}");
			}

			return null;
		}

		protected ReadOnlyCollection<_AppiumWebElement> FilterControlType(IEnumerable<_AppiumWebElement> elements, string controlType)
		{
			string tag = controlType;

			if (tag == "*")
			{
				return new ReadOnlyCollection<_AppiumWebElement>(elements.ToList());
			}

			if (_controlNameToTag.ContainsKey(controlType))
			{
				tag = _controlNameToTag[controlType];
			}

			return new ReadOnlyCollection<_AppiumWebElement>(elements.Where(element => element.TagName == tag).ToList());
		}

		protected virtual _AppiumWebElement SwapInUsefulElement(_AppiumWebElement element)
		{
			return element;
		}

		void Tap(AppiumQuery query)
		{
			_AppiumWebElement element = FindFirstElement(query);

			if (element == null)
			{
				return;
			}

			ClickOrTapElement(element);
		}

		protected abstract void ClickOrTapElement(_AppiumWebElement element);

		protected abstract void DoubleTap(AppiumQuery query);

		public void DragAndDrop(_AppiumWebElement fromElement, _AppiumWebElement toElement)
		{
			//Action Drag and Drop doesn't appear to work
			// https://github.com/microsoft/WinAppDriver/issues/1223

			var action = new Actions(Session);
			action.MoveToElement(fromElement).Build().Perform();
			action.ClickAndHold(fromElement).MoveByOffset(toElement.Location.X, toElement.Location.Y).Build().Perform();
			action.Release().Perform();
		}

		protected abstract void Scroll(AppiumQuery query, bool down);
		protected abstract void ScrollTo(AppiumQuery toQuery, AppiumQuery withinQuery, TimeSpan? timeout = null, bool down = true);

	}

}
