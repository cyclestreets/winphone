using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace Cyclestreets
{
	public class RouteSegment
	{
		private int _time;
		public string Time
		{
			get
			{
				TimeSpan t = TimeSpan.FromSeconds( _time );

				string answer = string.Format( "{0:D2}:{1:D2}",
											t.Minutes + ( t.Hours * 60 ),
											t.Seconds );
				return answer;
			}
			set
			{
				_time = int.Parse( value );
			}
		}
		private int _distance;
		public string Distance
		{
			get
			{
				float f = (float)_distance * 0.000621371192f;
				return f.ToString( "0.00" ) + "m";
			}
			set
			{
				_distance = int.Parse( value );
			}
		}

		private int _distanceShort;
		public int DistanceMetres
		{
			set
			{
				_distanceShort = value;
			}
			get
			{
				return _distanceShort;
			}
		}
		public string Turn { get; set; }
		public bool Walk { get; set; }
		public string ProvisionName { get; set; }
		public string Name { get; set; }
		public string BGColour { get; set; }

		public GeoCoordinate location { get; set; }
		public double Bearing { get; set; }
	}

	public class RouteDetails
	{
		public int timeInSeconds { get; set; }

		public float quietness { get; set; }

		public int signalledJunctions { get; set; }

		public int signalledCrossings { get; set; }

		public int grammesCO2saved { get; set; }

		public int calories { get; set; }

		public List<RouteSegment> segments = new List<RouteSegment>();

		public int distance { get; set; }
	}

	public class JourneyFactItem
	{
		public JourneyFactItem( string image )
		{
			BitmapImage src = new BitmapImage();
			src.UriSource = new Uri( image, UriKind.Relative );
			ItemImage = src;
		}

		public BitmapImage ItemImage { get; set; }
		public string Caption { get; set; }
		public string Value { get; set; }
	}

	public partial class Directions : PhoneApplicationPage
	{
		bool routeFound = false;
		ReverseGeocodeQuery revGeoQ = null;
		GeocodeQuery geoQ = null;
		//List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
		MapLayer wayPointLayer = null;
		Stack<Pushpin> waypoints = new Stack<Pushpin>();

		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();
		public static List<JourneyFactItem> facts = new List<JourneyFactItem>();

		private GeoCoordinate max = new GeoCoordinate( 90, -180 );
		private GeoCoordinate min = new GeoCoordinate( -90, 180 );

		public static RouteDetails route = new RouteDetails();

		GeoCoordinate current = null;

		private int currentStep = -1;

		String[] RouteType = { "balanced", "fastest", "quietest" };

		public Directions()
		{
			InitializeComponent();

			// hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
			myPosition = ApplicationBar.Buttons[ 0 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			cursorPos = ApplicationBar.Buttons[ 1 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			confirmWaypoint = ApplicationBar.Buttons[ 2 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			findRoute = ApplicationBar.Buttons[ 3 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;

			routeTypePicker.ItemsSource = RouteType;
			routeTypePicker.SelectedItem = RouteType[ 0 ];

			revGeoQ = new ReverseGeocodeQuery();
			revGeoQ.QueryCompleted += revGeoQ_QueryCompleted;

			geoQ = new GeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			findRoute.IsEnabled = false;
			clearCurrentPosition();

			startPoint.Populating += ( s, args ) =>
			{
				if( progress != null )
					progress.Opacity = 100;

				args.Cancel = true;
				WebClient wc = new WebClient();
				string prefix = HttpUtility.UrlEncode( args.Parameter );

				string myLocation = "";
				LocationRectangle rect = GetMapBounds();
				//myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
				myLocation = MyMap.Center.Latitude + "," + MyMap.Center.Longitude;
				myLocation = HttpUtility.UrlEncode( myLocation );

				//Uri service = new Uri( "http://cambridge.cyclestreets.net/api/geocoder.xml?key=" + MainPage.apiKey + myLocation + "&street=" + prefix );
#if DEBUG
				Uri service = new Uri( "http://demo.places.nlp.nokia.com/places/v1/suggest?at=" + myLocation + "&q=" + prefix + "&app_id=" + MainPage.hereAppID + "&app_code=" + MainPage.hereAppToken + "&accept=application/json" );
#else
				Uri service = new Uri( "http://places.nlp.nokia.com/places/v1/suggest?at=" + myLocation + "&q=" + prefix + "&app_id=" + MainPage.hereAppID + "&app_code=" + MainPage.hereAppToken + "&accept=application/json" );
#endif

				wc.DownloadStringCompleted += DownloadStringCompleted;
				wc.DownloadStringAsync( service, s );
			};

			var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[ 0 ] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, "RoutePlanner", false );
		}

		private void DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
		{
			AutoCompleteBox acb = e.UserState as AutoCompleteBox;
			if( acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty( e.Result ) )
			{
				Console.WriteLine( e.Result );
				JObject o = JObject.Parse( e.Result );
				JArray suggestions = (JArray)o[ "suggestions" ];
				List<string> names = new List<string>();
				foreach( string s in suggestions )
				{
					names.Add( s );
				}

				if( suggestions.Count > 0 )
				{
					acb.ItemsSource = suggestions;
					acb.PopulateComplete();
				}
			}
			progress.Opacity = 0;
		}

		private LocationRectangle GetMapBounds()
		{
			GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate( new Point( 0, 0 ) );
			GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate( new Point( MyMap.ActualWidth, MyMap.ActualHeight ) );

			return LocationRectangle.CreateBoundingRectangle( new[] { topLeft, bottomRight } );
		}

		private void revGeoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			MapLocation loc = e.Result[ 0 ];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
		}

		private void startPoint_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			/*SearchResult start = startPoint.SelectedItem as SearchResult;
			if( start != null )
			{
				if( !geoQ.IsBusy )
				{
					geoQ.GeoCoordinate = new GeoCoordinate( start.latitude, start.longitude );
					geoQ.QueryAsync();

					setCurrentPosition( geoQ.GeoCoordinate );

					SmartDispatcher.BeginInvoke( () =>
					{
						MyMap.SetView( geoQ.GeoCoordinate, 16 );
						//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
					} );
				}
			}*/
			if ( startPoint.SelectedItem != null )
			{
				string start = ( (JValue)startPoint.SelectedItem ).Value as string;

				if( !geoQ.IsBusy && !string.IsNullOrWhiteSpace( start ) )
				{
					geoQ.SearchTerm = start;
					geoQ.GeoCoordinate = MyMap.Center;
					geoQ.QueryAsync();
				}
			}
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			if( e.Result.Count > 0 )
			{
				GeoCoordinate g = e.Result[0].GeoCoordinate;
				setCurrentPosition( g );

				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( g, 16 );
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				} );
			}
		}

		private void myPosition_Click( object sender, EventArgs e )
		{
			if( !revGeoQ.IsBusy )
			{
				revGeoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( MainPage.MyGeoPosition.Coordinate );
				revGeoQ.QueryAsync();

				setCurrentPosition( revGeoQ.GeoCoordinate );

				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( revGeoQ.GeoCoordinate, 16 );
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				} );
			}
		}

		private void cursorPos_Click( object sender, EventArgs e )
		{
			if( !revGeoQ.IsBusy )
			{
				revGeoQ.GeoCoordinate = MyMap.Center;
				revGeoQ.QueryAsync();

				setCurrentPosition( revGeoQ.GeoCoordinate );

				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( revGeoQ.GeoCoordinate, 16 );
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				} );
			}
		}

		private void confirmWaypoint_Click( object sender, EventArgs e )
		{
			if( wayPointLayer == null )
			{
				wayPointLayer = new MapLayer();

				MyMap.Layers.Add( wayPointLayer );
			}

			// Change last push pin from finish to intermediate
			if( waypoints.Count > 1 )
			{
				Pushpin last = waypoints.Peek();
				last.Style = Resources[ "Intermediate" ] as Style;
			}

			Pushpin pp = new Pushpin();
			if( waypoints.Count == 0 )
				pp.Style = Resources[ "Start" ] as Style;
			else
				pp.Style = Resources[ "Finish" ] as Style;

			MapOverlay overlay = new MapOverlay();
			overlay.Content = pp;
			pp.GeoCoordinate = current;
			overlay.GeoCoordinate = current;
			overlay.PositionOrigin = new Point( 0.3, 1.0 );
			wayPointLayer.Add( overlay );

			addWaypoint( pp );

			clearCurrentPosition();
		}

		private void addWaypoint( Pushpin pp )
		{
			waypoints.Push( pp );
			if( waypoints.Count >= 2 )
				findRoute.IsEnabled = true;
			else
				findRoute.IsEnabled = false;
		}

		private void clearCurrentPosition()
		{
			current = null;
			confirmWaypoint.IsEnabled = false;
		}

		private void setCurrentPosition( GeoCoordinate c )
		{
			current = c;
			if( c != null )
				confirmWaypoint.IsEnabled = true;
		}

		private void findRoute_Click( object sender, EventArgs e )
		{
			string plan = "balanced";
			string itinerarypoints = "";// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
			int speed = 20;		//16 = 10mph 20 = 12mph 24 = 15mph
			int useDom = 0;		// 0=xml 1=gml

			foreach( Pushpin p in waypoints )
			{
				itinerarypoints = p.GeoCoordinate.Longitude + "," + p.GeoCoordinate.Latitude + "|" + itinerarypoints;
			}
			itinerarypoints = itinerarypoints.TrimEnd( '|' );

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.xml?key=" + MainPage.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
			_request.Start();

			progress.Opacity = 100;
		}

		private void RouteFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			geometryCoords.Clear();
			currentStep = -1;

			arrowLeft.Opacity = 50;
			arrowRight.Opacity = 100;

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

			route = new RouteDetails();

			List<RouteManeuver> manouvers = new List<RouteManeuver>();
			var steps = xml.Descendants( "marker" )
									.Where( e => (string)e.Parent.Name.LocalName == "markers" );

			string col1 = "#7F000000";
			string col2 = "#3F000000";
			bool swap = true;
			int totalTime = 0;
			foreach( XElement p in steps )
			{
				string markerType = p.Attribute( "type" ).Value;
				if( markerType == "route" )
				{
					route.timeInSeconds = int.Parse( p.Attribute( "time" ).Value );
					JourneyFactItem i = new JourneyFactItem( "Assets/clock.png" );
					i.Caption = "Journey time";
					i.Value = ( route.timeInSeconds / 60 ) + " minutes";
					facts.Add( i );
					route.quietness = float.Parse( p.Attribute( "quietness" ).Value );
					i = new JourneyFactItem( "Assets/picture.png" );
					i.Caption = "Quietness";
					i.Value = route.quietness + "% " + getQuietnessString( route.quietness );
					facts.Add( i );
					route.signalledJunctions = int.Parse( p.Attribute( "signalledJunctions" ).Value );
					i = new JourneyFactItem( "Assets/traffic_signals.png" );
					i.Caption = "Signaled Junctions";
					i.Value = "" + route.signalledJunctions;
					facts.Add( i );
					route.signalledCrossings = int.Parse( p.Attribute( "signalledCrossings" ).Value );
					i = new JourneyFactItem( "Assets/traffic_signals.png" );
					i.Caption = "Signaled Crossings";
					i.Value = "" + route.signalledCrossings;
					facts.Add( i );
					route.grammesCO2saved = int.Parse( p.Attribute( "grammesCO2saved" ).Value );
					i = new JourneyFactItem( "Assets/world.png" );
					i.Caption = "CO2 avoided";
					i.Value = (float)route.grammesCO2saved / 1000f + " kg";
					facts.Add( i );
					route.calories = int.Parse( p.Attribute( "calories" ).Value );
					i = new JourneyFactItem( "Assets/heart.png" );
					i.Caption = "Calories";
					i.Value = route.calories + " kcal";
					facts.Add( i );
				}
				else if( markerType == "segment" )
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

					RouteSegment s = new RouteSegment();
					s.location = coords[ 0 ];
					s.Bearing = Geodesy.Bearing( coords[ 0 ].Latitude, coords[ 0 ].Longitude, coords[ coords.Count - 1 ].Latitude, coords[ coords.Count - 1 ].Longitude );
					route.distance += (int)float.Parse( p.Attribute( "distance" ).Value );
					s.Distance = "" + route.distance;// p.Attribute( "distance" ).Value;
					s.DistanceMetres = (int)float.Parse( p.Attribute( "distance" ).Value );
					s.Name = p.Attribute( "name" ).Value;
					s.ProvisionName = p.Attribute( "provisionName" ).Value;
					int theLegOfTime = int.Parse( p.Attribute( "time" ).Value );
					s.Time = "" + ( totalTime + theLegOfTime );
					totalTime += theLegOfTime;
					s.Turn = p.Attribute( "turn" ).Value;
					s.Walk = ( int.Parse( p.Attribute( "walk" ).Value ) == 1 ? true : false );
					if( swap )
						s.BGColour = col1;
					else
						s.BGColour = col2;
					swap = !swap;
					route.segments.Add( s );
				}
			}

			JourneyFactItem item = new JourneyFactItem( "Assets/bullet_go.png" );
			item.Caption = "Distance";
			item.Value = (float)route.distance * 0.000621371192f + " miles";
			facts.Add( item );

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

				//NavigationService.Navigate( new Uri( "/DirectionsResults.xaml", UriKind.Relative ) );
				var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
				var sg = sgs[ 0 ] as VisualStateGroup;
				//ExtendedVisualStateManager.GoToElementState( LayoutRoot, "RouteFoundState", true );
				VisualStateManager.GoToState( this, "RouteFoundState", true );

				routeFound = true;

				progress.Opacity = 0;

				float f = (float)route.distance * 0.000621371192f;
				findLabel1.Text = f.ToString( "0.00" ) + "m\n" + ( route.timeInSeconds / 60 ) + " minutes";

			} );


		}

		private string getQuietnessString( float p )
		{
			if( p > 80 )
				return "Quiet";
			else if( p > 60 )
				return "Quite Quiet";
			else if( p > 40 )
				return "Quite Busy";
			else if( p > 20 )
				return "Busy";
			else
				return "Very Busy";
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

		private void routeTypePicker_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			ListPicker picker = sender as ListPicker;
			string plan = (string)picker.SelectedItem;
			if( routeFound )
			{
				string itinerarypoints = "";// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
				int speed = 20;		//16 = 10mph 20 = 12mph 24 = 15mph
				int useDom = 0;		// 0=xml 1=gml

				foreach( Pushpin p in waypoints )
				{
					itinerarypoints = p.GeoCoordinate.Longitude + "," + p.GeoCoordinate.Latitude + "|" + itinerarypoints;
				}
				itinerarypoints = itinerarypoints.TrimEnd( '|' );

				MyMap.MapElements.Clear();

				AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.xml?key=" + MainPage.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
				_request.Start();

				progress.Opacity = 100;
			}
		}

		private void Image_Tap_1( object sender, System.Windows.Input.GestureEventArgs e )
		{
			NavigationService.Navigate( new Uri( "/DirectionsResults.xaml", UriKind.Relative ) );
		}

		private void arrowLeft_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			currentStep--;
			if( currentStep < 0 )
			{
				arrowLeft.Opacity = 50;
				currentStep++;

				LocationRectangle rect = new LocationRectangle( min, max );
				MyMap.SetView( rect );
				MyMap.Pitch = 0;
				MyMap.Heading = 0;
			}
			else
			{
				arrowLeft.Opacity = 100;

				MyMap.SetView( route.segments[ currentStep ].location, 20, route.segments[ currentStep ].Bearing, 75 );
			}
			if( currentStep >= route.segments.Count )
				arrowRight.Opacity = 50;
			else
				arrowRight.Opacity = 100;


		}

		private void arrowRight_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			currentStep++;
			if( currentStep < 0 )
				arrowLeft.Opacity = 50;
			else
				arrowLeft.Opacity = 100;
			if( currentStep >= route.segments.Count )
			{
				arrowRight.Opacity = 50;
				currentStep--;
				// 				LocationRectangle rect = new LocationRectangle( min, max );
				// 				MyMap.SetView( rect );
				// 				MyMap.Pitch = 0;
			}
			else
			{
				arrowRight.Opacity = 100;
				MyMap.SetView( route.segments[ currentStep ].location, 20, route.segments[ currentStep ].Bearing, 75 );

				findLabel1.Text = route.segments[ currentStep ].Turn + " at " + route.segments[ currentStep ].Name + "\n Continue for " + route.segments[ currentStep ].DistanceMetres + "m";
			}
		}
	}
}