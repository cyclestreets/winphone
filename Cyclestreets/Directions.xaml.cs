using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Maps.Services;
using System.Device.Location;

namespace Cyclestreets
{
	public partial class Directions : PhoneApplicationPage
	{
		ReverseGeocodeQuery geoQ = null;
		List<GeoCoordinate> waypoints = new List<GeoCoordinate>();

		public Directions()
		{
			InitializeComponent();

			geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;
		}

		private void geoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
		{
			MapLocation loc = e.Result[0];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.City + ", " + loc.Information.Address.PostalCode;
		}

		private void myPosition_Click(object sender, EventArgs e)
		{
			geoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate(MainPage.MyGeoPosition.Coordinate);
			geoQ.QueryAsync();

			SmartDispatcher.BeginInvoke(() =>
			{
				MyMap.SetView(geoQ.GeoCoordinate, 16);
				//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
			});
		}
	}
}