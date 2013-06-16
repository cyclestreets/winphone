using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

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

		
	}
}