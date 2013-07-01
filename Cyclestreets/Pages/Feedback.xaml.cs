using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Cyclestreets.Pages
{
	class feedbackType
	{
		public string DisplayName { get; set; }
		public string InternalCode { get; set; }
		public feedbackType(string d, string i)
		{
			DisplayName = d;
			InternalCode = i;
		}
	}
	public partial class Feedback : PhoneApplicationPage
	{
		public Feedback()
		{
			InitializeComponent();

			feedbackTypeDropdown.ItemsSource = feedbackTypes;
			feedbackTypeDropdown.DisplayMemberPath = "DisplayName";
			feedbackTypeDropdown.SelectedIndex = 0;
		}

		private feedbackType[] feedbackTypes =
		{
			new feedbackType("Route Feedback", "route"),
			new feedbackType("App Feedback", "mobile"),
			new feedbackType("Bug Report", "bug"),
			new feedbackType("Other", "other"),
		};

		private void submitButton_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{

		}

		private void feedbackType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				feedbackType ftype = e.AddedItems[0] as feedbackType;
				if( ftype.InternalCode.Equals( "route" ) )
				{
					itineraryHead.Visibility = Visibility.Visible;
					itinerary.Visibility = Visibility.Visible;
				}
				else
				{
					itineraryHead.Visibility = Visibility.Collapsed;
					itinerary.Visibility = Visibility.Collapsed;
				}
			}
		}
	}
}