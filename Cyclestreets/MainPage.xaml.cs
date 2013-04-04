using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	public partial class MainPage : PhoneApplicationPage
	{
		List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();

		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();

		RouteQuery MyQuery = null;
		GeocodeQuery Mygeocodequery = null;
		private Geolocator trackingGeolocator;
		Geoposition MyGeoPosition = null;
		private bool runOnce = false;

		private string apiKey = "63356ae9c48793e1";

		private GeoCoordinate max = new GeoCoordinate( -90, -180 );
		private GeoCoordinate min = new GeoCoordinate( 90, 180 );

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

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			if( !runOnce )
			{
				runOnce = true;

				MyGeoPosition = args.Position;
				SmartDispatcher.BeginInvoke( () =>
				{
					//this.GetCoordinates();



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

			var fixtures = xml.Descendants( "waypoint" )
									.Where( e => (string)e.Parent.Name.LocalName == "markers" );

			foreach( XElement p in fixtures )
			{
				float longitude = float.Parse( p.Attribute( "longitude" ).Value );
				float latitude = float.Parse( p.Attribute( "latitude" ).Value );

				MyCoordinates.Add( new GeoCoordinate( latitude, longitude ) );
			}

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
						string[] xy = points[i].Split( ',' );

						double longitude = double.Parse( xy[0] );
						double latitude = double.Parse( xy[1] );
						coords.Add( new GeoCoordinate( latitude, longitude ) );

						if( max.Latitude < latitude )
							max.Latitude = latitude;
						if( min.Latitude > latitude )
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
				MyMap.Center = new GeoCoordinate( min.Latitude + ( ( max.Latitude - min.Latitude ) / 2f ), min.Longitude + ( ( max.Longitude - min.Longitude ) / 2f ) );
				MyMap.ZoomLevel = 10;
				int count = geometryCoords.Count;
				for( int i = 0; i < count; i++ )
				{
					List<GeoCoordinate> coords = geometryCoords[i];
					DrawMapMarker( coords.ToArray(), geometryColor[i] );
				}
			} );
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
				polygon.Path.Add( coordinate[i] );
			}

			MyMap.MapElements.Add( polygon );
		}

		private void GetCoordinates()
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
		}

		void Mygeocodequery_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
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
		}

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
	}
}