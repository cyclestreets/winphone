using System;
using Microsoft.Phone.Controls;

namespace Cyclestreets
{


	public partial class DirectionsResults : PhoneApplicationPage
	{

		public DirectionsResults()
		{
			InitializeComponent();

			directionList.ItemsSource = DirectionsPage.route.segments;
			factsList.ItemsSource = DirectionsPage.facts;
		}

		private void ApplicationBarIconButton_Click( object sender, EventArgs e )
		{
			//PhoneApplicationService.Current.State[ "route" ] = currentRouteData;
			NavigationService.Navigate( new Uri( "/Pages/SaveRoute.xaml", UriKind.Relative ) );
		}

		private void share_Click( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/ShareChoice.xaml", UriKind.Relative ) );
		}

		private void routeFeedback_Click( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/Feedback.xaml?routeID=" + DirectionsPage.route.routeIndex, UriKind.Relative ) );
		}


	}
}