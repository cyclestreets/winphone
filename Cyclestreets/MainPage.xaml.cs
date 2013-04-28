using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	public class POIItem
	{
		public string POILabel { get; set; }
		public bool POIEnabled { get; set; }
		public string POIName { get; set; }
	}

	/*public class SearchResult
	{
		public double longitude
		{
			get;
			set;
		}
		public double latitude
		{
			get;
			set;
		}
		public string name
		{
			get;
			set;
		}
		public string near
		{
			get;
			set;
		}

		public override string ToString()
		{
			return name + ", " + near;
		}
	}*/

	public partial class MainPage : PhoneApplicationPage
	{
		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();
		List<GeoCoordinate> waypoints = new List<GeoCoordinate>();

		private Geolocator trackingGeolocator;
		public static Geoposition MyGeoPosition = null;

		private GeoCoordinate max = new GeoCoordinate( 90, -180 );
		private GeoCoordinate min = new GeoCoordinate( -90, 180 );

		ReverseGeocodeQuery geoQ = null;

		// Constructor
		public MainPage()
		{
			InitializeComponent();
			this.StartTracking();

			geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[ 0 ] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, ( (VisualState)sg.States[ 0 ] ).Name, true );
		}

		public void StartTracking()
		{
			if( this.trackingGeolocator != null )
			{
				return;
			}

			this.trackingGeolocator = new Geolocator();
			this.trackingGeolocator.ReportInterval = (uint)TimeSpan.FromSeconds( 30 ).TotalMilliseconds;

			// this implicitly starts the tracking operation
			this.trackingGeolocator.PositionChanged += positionChangedHandler;
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			throw new NotImplementedException();
		}

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
			SmartDispatcher.BeginInvoke( () =>
			{
				MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 14 );
			} );
		}

		private LocationRectangle GetMapBounds()
		{
			GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate( new Point( 0, 0 ) );
			GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate( new Point( MyMap.Width, MyMap.Height ) );

			return LocationRectangle.CreateBoundingRectangle( new[] { topLeft, bottomRight } );
		}

		private void ApplicationBarMenuItem_ToggleAerialView( object sender, EventArgs e )
		{
			ApplicationBarMenuItem item = sender as ApplicationBarMenuItem;
			if( MyMap.CartographicMode == MapCartographicMode.Hybrid )
			{
				item.Text = "Enable aerial view";
				MyMap.CartographicMode = MapCartographicMode.Road;
			}
			else
			{
				item.Text = "Disable aerial view";
				MyMap.CartographicMode = MapCartographicMode.Hybrid;
			}
		}

		private void ApplicationBarIconButton_Directions( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Directions.xaml", UriKind.Relative ) );
		}

		private void poiList_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/POIList.xaml?longitude="+MyMap.Center.Longitude+"&latitude="+MyMap.Center.Latitude, UriKind.Relative ) );
		}
	}
}