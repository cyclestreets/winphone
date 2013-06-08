using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets
{
	public partial class TrialExpired : PhoneApplicationPage
	{
		public TrialExpired()
		{
			InitializeComponent();
		}
		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			// For UI consistency, clear the entire page stack
			while( App.RootFrame.RemoveBackEntry() != null )
			{
				; // do nothing
			}
		}

		private void feedback_Click( object sender, RoutedEventArgs e )
		{
			EmailComposeTask task = new EmailComposeTask();
			task.To = "info@cyclestreets.net";
			task.Subject = "Windows Phone Trial Feedback. Trial ID:" + ( (App)App.Current ).trialID;
			task.Show();
		}

		private void buy_Click( object sender, RoutedEventArgs e )
		{
			MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
			_marketPlaceDetailTask.Show();
		}
	}
}