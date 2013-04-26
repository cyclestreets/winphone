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

	public class SearchResult
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
	}

	public partial class MainPage : PhoneApplicationPage
	{
		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();
		List<GeoCoordinate> waypoints = new List<GeoCoordinate>();

		RouteQuery MyQuery = null;
		GeocodeQuery Mygeocodequery = null;
		private Geolocator trackingGeolocator;
		public static Geoposition MyGeoPosition = null;
		private bool runOnce = false;

		public static string hereAppID = "zgcciiZ696xHUiuoyJZi";
		public static string hereAppToken = "tH8mLbASkG9oz6j8DuXn7A";

		public static string apiKey = "d2ff10bbbded8e86";

		private GeoCoordinate max = new GeoCoordinate( 90, -180 );
		private GeoCoordinate min = new GeoCoordinate( -90, 180 );
		private bool locationFound = false;
		private bool trackMe = false;

		private SearchResult start;
		private SearchResult finish;

		ReverseGeocodeQuery geoQ = null;

		// Constructor
		public MainPage()
		{
			InitializeComponent();
			this.StartTracking();

			string plan = "balanced";
			string itinerarypoints = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
			int speed = 20;		//16 = 10mph 20 = 12mph 24 = 15mph
			int useDom = 0;		// 0=xml 1=gml

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.xml?key=" + apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
			_request.Start();

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

			startPoint.Populating += ( s, args ) =>
			{
				args.Cancel = true;
				WebClient wc = new WebClient();
				string prefix = HttpUtility.UrlEncode( args.Parameter );

				string myLocation = "";
				if( locationFound )
				{
					myLocation = "&w=" + MyGeoPosition.Coordinate.Longitude + "&s=" + MyGeoPosition.Coordinate.Latitude + "&e=" + MyGeoPosition.Coordinate.Longitude + "&n=" + MyGeoPosition.Coordinate.Latitude + "&zoom=16";
				}

				Uri service = new Uri( "http://cambridge.cyclestreets.net/api/geocoder.xml?key=" + apiKey + myLocation + "&street=" + prefix );
				wc.DownloadStringCompleted += DownloadStringCompleted;
				wc.DownloadStringAsync( service, s );

				/*if (geoQ.IsBusy == true)
				{
					geoQ.CancelAsync();
				}

				if (locationFound)
				{
					geoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				}
				else
				{
					GeoCoordinate setMe = new GeoCoordinate(MyMap.Center.Latitude, MyMap.Center.Longitude);
					setMe.HorizontalAccuracy = 1000000;
				}
				geoQ.SearchTerm = args.Parameter;
				geoQ.MaxResultCount = 20;

				geoQ.QueryAsync();*/
			};
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			MapLocation loc = e.Result[ 0 ];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.City + ", " + loc.Information.Address.PostalCode;
			usePos.IsEnabled = true;
			start = new SearchResult();
			start.longitude = loc.GeoCoordinate.Longitude;
			start.latitude = loc.GeoCoordinate.Latitude;
			start.name = loc.Information.Address.ToString();
			start.near = loc.Information.Address.City;


			/*//			throw new NotImplementedException();
			AutoCompleteBox acb = startPoint as AutoCompleteBox;
			List<SearchResult> suggestions = new List<SearchResult>();
			for (int i = 0; i < e.Result.Count; i++)
			{
				SearchResult result = new SearchResult();
				MapLocation loc = e.Result[i];
				result.longitude = loc.GeoCoordinate.Longitude;
				result.latitude = loc.GeoCoordinate.Latitude;
				result.name = loc.Information.Address.ToString();
				result.near = loc.Information.Address.City;
				suggestions.Add(result);

			}
			if (suggestions.Count > 0)
			{
				acb.ItemsSource = suggestions;
				acb.PopulateComplete();
			}*/
		}

		private void DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
		{
			AutoCompleteBox acb = e.UserState as AutoCompleteBox;
			if( acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty( e.Result ) )
			{
				List<SearchResult> suggestions = new List<SearchResult>();

				XDocument xml = XDocument.Parse( e.Result.Trim() );

				var results = xml.Descendants( "result" )
										.Where( ev => (string)ev.Parent.Name.LocalName == "results" );

				foreach( XElement p in results )
				{
					SearchResult result = new SearchResult();
					result.longitude = float.Parse( p.Element( "longitude" ).Value );
					result.latitude = float.Parse( p.Element( "latitude" ).Value );
					result.name = p.Element( "name" ).Value;
					result.near = p.Element( "near" ).Value;
					suggestions.Add( result );
				}

				if( suggestions.Count > 0 )
				{
					acb.ItemsSource = suggestions;
					acb.PopulateComplete();
				}
			}
		}

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
			locationFound = true;
			if( trackMe )
			{
				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), MyMap.ZoomLevel );
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				} );
			}
		}

		private void RouteFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			/*var fixtures = xml.Descendants( "waypoint" )
									.Where( e => (string)e.Parent.Name.LocalName == "markers" );

			foreach( XElement p in fixtures )
			{
				float longitude = float.Parse( p.Attribute( "longitude" ).Value );
				float latitude = float.Parse( p.Attribute( "latitude" ).Value );

				MyCoordinates.Add( new GeoCoordinate( latitude, longitude ) );
			}*/

			// 			MyQuery = new RouteQuery();
			// 			MyQuery.Waypoints = MyCoordinates;
			// 			MyQuery.TravelMode = TravelMode.Walking;
			// 			MyQuery.QueryCompleted += MyQuery_QueryCompleted;
			// 			MyQuery.QueryAsync();



			List<RouteManeuver> manouvers = new List<RouteManeuver>();
			var steps = xml.Descendants( "marker" )
									.Where( e => (string)e.Parent.Name.LocalName == "markers" );

			foreach( XElement p in steps )
			{
				string markerType = p.Attribute( "type" ).Value;
				if( markerType == "segment" )
				{
					string pointsText = p.Attribute( "points" ).Value;
					string[] points = pointsText.Split( ' ' );
					List<GeoCoordinate> coords = new List<GeoCoordinate>();
					for( int i = 0; i < points.Length; i++ )
					{
						string[] xy = points[ i ].Split( ',' );

						double longitude = double.Parse( xy[ 0 ] );
						double latitude = double.Parse( xy[ 1 ] );
						coords.Add( new GeoCoordinate( latitude, longitude ) );

						if( max.Latitude > latitude )
							max.Latitude = latitude;
						if( min.Latitude < latitude )
							min.Latitude = latitude;
						if( max.Longitude < longitude )
							max.Longitude = longitude;
						if( min.Longitude > longitude )
							min.Longitude = longitude;
					}
					geometryCoords.Add( coords );
					geometryColor.Add( ConvertHexStringToColour( p.Attribute( "color" ).Value ) );
				}
			}

			SmartDispatcher.BeginInvoke( () =>
			{
				LocationRectangle rect = new LocationRectangle( min, max );
				MyMap.SetView( rect );
				//MyMap.Center = new GeoCoordinate(min.Latitude + ((max.Latitude - min.Latitude) / 2f), min.Longitude + ((max.Longitude - min.Longitude) / 2f));
				//MyMap.ZoomLevel = 10;
				int count = geometryCoords.Count;
				for( int i = 0; i < count; i++ )
				{
					List<GeoCoordinate> coords = geometryCoords[ i ];
					DrawMapMarker( coords.ToArray(), geometryColor[ i ] );
				}
			} );
		}

		private LocationRectangle GetMapBounds()
		{
			GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate( new Point( 0, 0 ) );
			GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate( new Point( MyMap.Width, MyMap.Height ) );

			return LocationRectangle.CreateBoundingRectangle( new[] { topLeft, bottomRight } );
		}

		private Color ConvertHexStringToColour( string hexString )
		{
			byte a = 0;
			byte r = 0;
			byte g = 0;
			byte b = 0;
			if( hexString.StartsWith( "#" ) )
			{
				hexString = hexString.Substring( 1, 6 );
			}
			//a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
			//	System.Globalization.NumberStyles.AllowHexSpecifier));
			r = Convert.ToByte( Int32.Parse( hexString.Substring( 0, 2 ),
				System.Globalization.NumberStyles.AllowHexSpecifier ) );
			g = Convert.ToByte( Int32.Parse( hexString.Substring( 2, 2 ),
				System.Globalization.NumberStyles.AllowHexSpecifier ) );
			b = Convert.ToByte( Int32.Parse( hexString.Substring( 4, 2 ), System.Globalization.NumberStyles.AllowHexSpecifier ) );
			return Color.FromArgb( 255, r, g, b );
		}

		private void DrawMapMarker( GeoCoordinate[] coordinate, Color color )
		{
			// Create a map marker
			MapPolyline polygon = new MapPolyline();
			polygon.StrokeColor = color;
			polygon.StrokeThickness = 3;
			polygon.Path = new GeoCoordinateCollection();
			for( int i = 0; i < coordinate.Length; i++ )
			{
				//Point p = MyMap.ConvertGeoCoordinateToViewportPoint( coordinate[i] );
				polygon.Path.Add( coordinate[ i ] );
			}

			MyMap.MapElements.Add( polygon );
		}

		/*private void GetCoordinates()
		{
			// Get the phone's current location.
			Geolocator MyGeolocator = new Geolocator();
			MyGeolocator.DesiredAccuracyInMeters = 5;

			try
			{
				//MyGeoPosition = await MyGeolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
				MyCoordinates.Add( new GeoCoordinate( MyGeoPosition.Coordinate.Latitude, MyGeoPosition.Coordinate.Longitude ) );

				Mygeocodequery = new GeocodeQuery();
				Mygeocodequery.SearchTerm = "NG11LX";
				Mygeocodequery.GeoCoordinate = new GeoCoordinate( MyGeoPosition.Coordinate.Latitude, MyGeoPosition.Coordinate.Longitude );

				Mygeocodequery.QueryCompleted += Mygeocodequery_QueryCompleted;
				Mygeocodequery.QueryAsync();
			}
			catch( UnauthorizedAccessException )
			{
				MessageBox.Show( "Location is disabled in phone settings or capabilities are not checked." );
			}
			catch( Exception ex )
			{
				// Something else happened while acquiring the location.
				MessageBox.Show( ex.Message );
			}
		}*/

		/*void Mygeocodequery_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			if( e.Error == null )
			{
				MyQuery = new RouteQuery();
				MyCoordinates.Add( e.Result[0].GeoCoordinate );
				MyQuery.Waypoints = MyCoordinates;
				MyQuery.QueryCompleted += MyQuery_QueryCompleted;
				MyQuery.QueryAsync();
				Mygeocodequery.Dispose();
			}
		}*/

		void MyQuery_QueryCompleted( object sender, QueryCompletedEventArgs<Route> e )
		{
			if( e.Error == null )
			{
				Route MyRoute = e.Result;
				MapRoute MyMapRoute = new MapRoute( MyRoute );
				MyMap.AddRoute( MyMapRoute );
				MyQuery.Dispose();
			}
		}

		private void MyMap_ZoomLevelChanged( object sender, MapZoomLevelChangedEventArgs e )
		{



		}

		private void Button_Click_1( object sender, System.Windows.RoutedEventArgs e )
		{
			var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[ 0 ] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, ( (VisualState)sg.States[ 0 ] ).Name, true );
		}

		private void startPoint_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			start = startPoint.SelectedItem as SearchResult;
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
			//var sgs = ExtendedVisualStateManager.GetVisualStateGroups(LayoutRoot);
			//var sg = sgs[0] as VisualStateGroup;
			//ExtendedVisualStateManager.GoToElementState(LayoutRoot, ((VisualState)sg.States[sg.CurrentState == sg.States[0] ? 1 : 0]).Name, true);
			// Navigate to the new page
			NavigationService.Navigate( new Uri( "/Directions.xaml", UriKind.Relative ) );
		}

		private void ApplicationBarIconButton_TrackMe( object sender, EventArgs e )
		{
			trackMe = !trackMe;
		}

		private void startMyLocation_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 17 );

			start = new SearchResult();
			start.latitude = MyGeoPosition.Coordinate.Latitude;
			start.longitude = MyGeoPosition.Coordinate.Longitude;


		}

		private void cursorPos_click( object sender, RoutedEventArgs e )
		{
			GeoCoordinate coord = MyMap.Center;
			geoQ.GeoCoordinate = coord;
			geoQ.QueryAsync();

			SmartDispatcher.BeginInvoke( () =>
			{
				MyMap.SetView( coord, 16 );
				//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
			} );
		}

		private void gpsPos_click( object sender, RoutedEventArgs e )
		{
			geoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate );
			geoQ.QueryAsync();

			SmartDispatcher.BeginInvoke( () =>
			{
				MyMap.SetView( geoQ.GeoCoordinate, 16 );
				//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
			} );
		}

		private void usePos_Click( object sender, RoutedEventArgs e )
		{
			waypoints.Add( new GeoCoordinate( start.latitude, start.longitude ) );
			findLabel.Text = "Select finish point or waypoint";
			startPoint.Text = "";
			usePos.IsEnabled = false;
		}

		private void poiList_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/POIList.xaml?longitude="+MyMap.Center.Longitude+"&latitude="+MyMap.Center.Latitude, UriKind.Relative ) );
		}
	}
}