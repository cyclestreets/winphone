using System;
using Microsoft.Phone.Controls;

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

		
	}
}