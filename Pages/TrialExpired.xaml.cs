using System.Windows;
using Microsoft.Phone.Controls;
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

			FlurryWP8SDK.Api.LogEvent( "Trial Expired Shown" );

			// For UI consistency, clear the entire page stack
			while( App.RootFrame.RemoveBackEntry() != null )
			{
				; // do nothing
			}
		}

		private void feedback_Click( object sender, RoutedEventArgs e )
		{
			FlurryWP8SDK.Api.LogEvent( "Send feedback tapped" );

			EmailComposeTask task = new EmailComposeTask();
			task.To = "info@cyclestreets.net";
			task.Subject = "Windows Phone Trial Feedback. Trial ID:" + ( (App)App.Current ).TrialID;
			task.Show();


		}

		private void buy_Click( object sender, RoutedEventArgs e )
		{
			FlurryWP8SDK.Api.LogEvent( "Buy tapped" );

			MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
			_marketPlaceDetailTask.Show();
		}
	}
}