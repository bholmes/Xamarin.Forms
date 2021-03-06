﻿using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;
using System.Linq;

#if UITEST
using Xamarin.Forms.Core.UITests;
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls.Issues
{
#if UITEST
	[NUnit.Framework.Category(UITestCategories.CarouselView)]
#endif
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 12574, "CarouselView Loop=True default freezes iOS app", PlatformAffected.Default)]
	public class Issue12574 : TestContentPage
	{
		ViewModelIssue12574 viewModel;
		CarouselView _carouselView;
		Button _btn;
		string carouselAutomationId = "carouselView";
		string btnRemoveAutomationId = "btnRemove";

		protected override void Init()
		{
			_btn = new Button
			{
				Text = "Remove Last",
				AutomationId = btnRemoveAutomationId
			};
			_btn.SetBinding(Button.CommandProperty, "RemoveItemsCommand");
			// Initialize ui here instead of ctor
			_carouselView = new CarouselView
			{
				AutomationId = carouselAutomationId,
				Margin = new Thickness(30),
				BackgroundColor = Color.Yellow,
				ItemTemplate = new DataTemplate(() =>
				{

					var stacklayout = new StackLayout();
					var labelId = new Label();
					var labelText = new Label();
					var labelDescription = new Label();
					labelId.SetBinding(Label.TextProperty, "Id");
					labelText.SetBinding(Label.TextProperty, "Text");
					labelDescription.SetBinding(Label.TextProperty, "Description");

					stacklayout.Children.Add(labelId);
					stacklayout.Children.Add(labelText);
					stacklayout.Children.Add(labelDescription);
					return stacklayout;
				})
			};
			_carouselView.SetBinding(CarouselView.ItemsSourceProperty, "Items");
			this.SetBinding(Page.TitleProperty, "Title");

			var layout = new Grid();
			layout.RowDefinitions.Add(new RowDefinition { Height = 100 });
			layout.RowDefinitions.Add(new RowDefinition());
			Grid.SetRow(_carouselView, 1);
			layout.Children.Add(_btn);
			layout.Children.Add(_carouselView);

			BindingContext = viewModel = new ViewModelIssue12574();
			Content = layout;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			viewModel.OnAppearing();
		}

#if UITEST
		[Test]
		public void Issue12574Test()
		{
			RunningApp.WaitForElement("0 item");

			var rect = RunningApp.Query(c => c.Marked(carouselAutomationId)).First().Rect;
			var centerX = rect.CenterX;
			var rightX = rect.X - 5;
			RunningApp.DragCoordinates(centerX + 40, rect.CenterY, rightX, rect.CenterY);

			RunningApp.WaitForElement("1 item");

			RunningApp.DragCoordinates(centerX + 40, rect.CenterY, rightX, rect.CenterY);

			RunningApp.WaitForElement("2 item");

			RunningApp.Tap(btnRemoveAutomationId);
			
			RunningApp.WaitForElement("1 item");

			rightX = rect.X + rect.Width - 1;
			RunningApp.DragCoordinates(centerX, rect.CenterY, rightX, rect.CenterY);

			RunningApp.WaitForElement("0 item");
		}
#endif
	}

	[Preserve(AllMembers = true)]
	class ViewModelIssue12574 : BaseViewModel1
	{
		public ObservableCollection<ModelIssue12574> Items { get; set; }
		public Command LoadItemsCommand { get; set; }
		public Command RemoveItemsCommand { get; set; }

		public ViewModelIssue12574()
		{
			Title = "CarouselView Looping";
			Items = new ObservableCollection<ModelIssue12574>();
			LoadItemsCommand = new Command(() => ExecuteLoadItemsCommand());
			RemoveItemsCommand = new Command(() => ExecuteRemoveItemsCommand());
		}
		void ExecuteRemoveItemsCommand()
		{
			Items.Remove(Items.Last());
		}
		void ExecuteLoadItemsCommand()
		{
			IsBusy = true;

			try
			{
				Items.Clear();
				for (int i = 0; i < 3; i++)
				{
					Items.Add(new ModelIssue12574 { Id = Guid.NewGuid().ToString(), Text = $"{i} item", Description = "This is an item description." });
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			finally
			{
				IsBusy = false;
			}
		}

		public void OnAppearing()
		{
			IsBusy = true;
			LoadItemsCommand.Execute(null);
		}
	}

	[Preserve(AllMembers = true)]
	class ModelIssue12574
	{
		public string Id { get; set; }
		public string Text { get; set; }
		public string Description { get; set; }
	}

	class BaseViewModel1 : INotifyPropertyChanged
	{
		public string Title { get; set; }
		public bool IsInitialized { get; set; }

		bool _isBusy;

		/// <summary>
		/// Gets or sets if VM is busy working
		/// </summary>
		public bool IsBusy
		{
			get { return _isBusy; }
			set { _isBusy = value; OnPropertyChanged("IsBusy"); }
		}

		//INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged == null)
				return;

			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

}