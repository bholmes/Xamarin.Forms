using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xamarin.UITest.Queries;

namespace Xamarin.Forms.Core.UITests.Appium
{
	internal class AppiumQuery
	{
		public static AppiumQuery FromQuery(Func<AppQuery, AppQuery> query)
		{
			var raw = GetRawQuery(query);
			return FromRaw(raw);
		}

		public static AppiumQuery FromMarked(string marked)
		{
			return new AppiumQuery("*", "marked", marked, $"* marked:'{marked}'");
		}

		public static AppiumQuery FromRaw(string raw)
		{
			Debug.WriteLine($">>>>> Converting raw query '{raw}' to {nameof(AppiumQuery)}");

			var match = Regex.Match(raw, @"(.*)\s(marked|text|index):(?:')?(([^']*|\n)*)(?:')?");

			var controlType = match.Groups[1].Captures[0].Value;
			var filterType = match.Groups[2].Captures[0].Value;
			var filterValue = match.Groups[3].Captures[0].Value;

			// Just ignoring everything else for now (parent, index statements, etc)
			var result = new AppiumQuery(controlType, filterType, filterValue, raw);

			Debug.WriteLine($">>>>> WinQuery is: {result}");

			return result;
		}

		static string GetRawQuery(Func<AppQuery, AppQuery> query = null)
		{
			if (query == null)
			{
				return string.Empty;
			}

			// When we pull out the iOS query it's got any instances of "'" escaped with "\", need to fix that up
			return query(new AppQuery(QueryPlatform.iOS)).ToString().Replace("\\'", "'");
		}

		static FilterTypes ConvertToFilterType(string filterType)
		{
			switch (filterType)
			{
				case "index":
					return FilterTypes.Index;
				case "marked":
					return FilterTypes.Marked;
				case "text":
					return FilterTypes.Text;
				default:
					throw new NotImplementedException();
			}
		}

		AppiumQuery(string controlType, string filterType, string filterValue, string raw)
		{
			ControlType = controlType;
			FilterType = ConvertToFilterType(filterType);
			FilterValue = filterValue;
			Raw = raw;
		}

		public string ControlType { get; }

		public FilterTypes FilterType { get; }
		public string FilterValue { get; }

		public string Raw { get; }

		public override string ToString()
		{
			return $"{nameof(ControlType)}: {ControlType}, {FilterType}: {FilterValue}";
		}

		public enum FilterTypes
		{
			Marked,
			Text,
			Index
		}
	}
}