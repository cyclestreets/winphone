using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;

namespace Cyclestreets
{
	public partial class Directions : PhoneApplicationPage
	{
		ReverseGeocodeQuery geoQ = null;
		List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
		MapLayer wayPointLayer = null;

		GeoCoordinate current = null;

		public Directions()
		{
			InitializeComponent();

			geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			MapLocation loc = e.Result[0];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.City + ", " + loc.Information.Address.PostalCode;
		}

		private void startPoint_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			//start = startPoint.SelectedItem as SearchResult;
		}

		private void myPosition_Click( object sender, EventArgs e )
		{
			geoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( MainPage.MyGeoPosition.Coordinate );
			geoQ.QueryAsync();

			current = geoQ.GeoCoordinate;

			SmartDispatcher.BeginInvoke( () =>
			{
				MyMap.SetView( geoQ.GeoCoordinate, 16 );
				//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
			} );
		}

		private void confirmWaypoint_Click( object sender, EventArgs e )
		{
			if( wayPointLayer == null )
			{
				wayPointLayer = new MapLayer();

				MyMap.Layers.Add( wayPointLayer );
			}

			Pushpin pp = new Pushpin();
			pp.Style = Resources["Start"] as Style;
			MapOverlay overlay = new MapOverlay();
			overlay.Content = pp;
			overlay.GeoCoordinate = current;
			overlay.PositionOrigin = new Point( 0.3, 1.0 );
			wayPointLayer.Add( overlay );
			/*BitmapImage imgSource = new BitmapImage( new Uri( "/Assets/start_wisp.png", UriKind.Relative ) );
			//BitmapImage imgSource = new BitmapImage( new Uri( "/Assets/start_wisp.png", UriKind.RelativeOrAbsolute ) );
			MapOverlay overlay = new MapOverlay();
			overlay.Content = imgSource;
			overlay.GeoCoordinate = current;
			overlay.PositionOrigin = new Point( 0.5, 1.0 );
			wayPointLayer.Add( overlay );*/

		}
	}
}