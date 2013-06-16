using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets
{


	public partial class DirectionsResults : PhoneApplicationPage
	{

		public DirectionsResults()
		{
			InitializeComponent();

			directionList.ItemsSource = Directions.route.segments;
			factsList.ItemsSource = Directions.facts;
		}

		private void ApplicationBarIconButton_Click( object sender, EventArgs e )
		{
			//PhoneApplicationService.Current.State[ "route" ] = currentRouteData;
			NavigationService.Navigate( new Uri( "/Pages/SaveRoute.xaml", UriKind.Relative ) );
		}

		private void share_Click( object sender, EventArgs e )
		{
			ShareLinkTask shareLinkTask = new ShareLinkTask();

			shareLinkTask.Title = "Cycle Route";
			shareLinkTask.LinkUri = new Uri( "http://www.cyclestreets.net/journey/" + (int)PhoneApplicationService.Current.State[ "routeIndex" ] + "/", UriKind.Absolute );
			shareLinkTask.Message = "CycleStreets route sharing test.";

			shareLinkTask.Show();
		}


	}
}