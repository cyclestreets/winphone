/// Satnav mode
/// Prompt feedback

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cyclestreets.Pages;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	public class CSResult
	{
		public string resultName;
		public GeoCoordinate coord;

		public override string ToString()
		{
			return resultName;
		}
	}

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
		public string Turn
		{
			get;
			set;
		}
		public bool Walk
		{
			get;
			set;
		}
		public string ProvisionName
		{
			get;
			set;
		}
		public string Name
		{
			get;
			set;
		}
		public string BGColour
		{
			get;
			set;
		}

		public GeoCoordinate location
		{
			get;
			set;
		}
		public double Bearing
		{
			get;
			set;
		}
	}

	public class RouteDetails
	{
		public int timeInSeconds
		{
			get;
			set;
		}

		public float quietness
		{
			get;
			set;
		}

		public int signalledJunctions
		{
			get;
			set;
		}

		public int signalledCrossings
		{
			get;
			set;
		}

		public int grammesCO2saved
		{
			get;
			set;
		}

		public int calories
		{
			get;
			set;
		}

		public int routeIndex
		{
			get;
			set;
		}

		public List<RouteSegment> segments = new List<RouteSegment>();

		public int distance
		{
			get;
			set;
		}
	}

	public class JourneyFactItem
	{
		public JourneyFactItem( string image )
		{
			BitmapImage src = new BitmapImage();
			src.UriSource = new Uri( image, UriKind.Relative );
			ItemImage = src;
		}

		public BitmapImage ItemImage
		{
			get;
			set;
		}
		public string Caption
		{
			get;
			set;
		}
		public string Value
		{
			get;
			set;
		}
	}

	public partial class DirectionsPage : PhoneApplicationPage
	{
		ReverseGeocodeQuery revGeoQ = null;
		GeocodeQuery geoQ = null;
		//List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
		MapLayer wayPointLayer = null;
		Stackish<Pushpin> waypoints = new Stackish<Pushpin>();
		Dictionary<Pushpin, POI> pinItems = new Dictionary<Pushpin, POI>();
		private MapLayer poiLayer;

		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();
		public static List<JourneyFactItem> facts = new List<JourneyFactItem>();

		private GeoCoordinate max = new GeoCoordinate( 90, -180 );
		private GeoCoordinate min = new GeoCoordinate( -90, 180 );

		public static RouteDetails route = new RouteDetails();

		private bool hideRouteOptions = false;

		GeoCoordinate current = null;

		private int currentStep = -1;

		public static String[] MapStyle = { "OpenStreetMap", "OpenCycleMap", "Nokia" };
		public static String[] RouteType = { "balanced route", "fastest route", "quietest route" };
		public static String[] CycleSpeed = { "10mph", "12mph", "15mph" };
		public static String[] EnabledDisabled = { "Enabled", "Disabled" };
		private string currentRouteData;
		private WebClient placeSearch;

		public DirectionsPage()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;

			LocationManager.instance.trackingGeolocator.PositionChanged += positionChangedHandler;

			// hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
			myPosition = ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			cursorPos = ApplicationBar.Buttons[1] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			confirmWaypoint = ApplicationBar.Buttons[2] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			findRoute = ApplicationBar.Buttons[3] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			saveRoute = ApplicationBar.MenuItems[0] as Microsoft.Phone.Shell.ApplicationBarMenuItem;

			findRoute.IsEnabled = false;
			saveRoute.IsEnabled = false;

			string plan = SettingManager.instance.GetStringValue( "defaultRouteType", "balanced route" );

			routeTypePicker.ItemsSource = RouteType;
			routeTypePicker.SelectedItem = plan;

			revGeoQ = new ReverseGeocodeQuery();
			revGeoQ.QueryCompleted += revGeoQ_QueryCompleted;

			geoQ = new GeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			clearCurrentPosition();

			startPoint.Populating += ( s, args ) =>
			{
				if( placeSearch != null )
				{
					System.Diagnostics.Debug.WriteLine( "Cancelling previous request" );
					placeSearch.CancelAsync();
				}

				args.Cancel = true;
				StartPlaceSearch( args.Parameter, s );
			};

			var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[0] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, "RoutePlanner", false );
		}

		private void StartPlaceSearch( string textEntry, object userObject )
		{
			App.networkStatus.networkIsBusy = true;
			System.Diagnostics.Debug.WriteLine( "Searching for " + textEntry );

			placeSearch = new WebClient();
			string prefix = HttpUtility.UrlEncode( textEntry );

			string myLocation = "";
			LocationRectangle rect = GetMapBounds();
			//myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
			if( MyMap.ZoomLevel < 12f && LocationManager.instance.MyGeoPosition != null )
				myLocation = LocationManager.instance.MyGeoPosition.Coordinate.Latitude + "," + LocationManager.instance.MyGeoPosition.Coordinate.Longitude;
			else
				myLocation = MyMap.Center.Latitude + "," + MyMap.Center.Longitude;
			myLocation = HttpUtility.UrlEncode( myLocation );

#if DEBUG
			Uri service = new Uri( "http://demo.places.nlp.nokia.com/places/v1/suggest?at=" + myLocation + "&q=" + prefix + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application/json" );
#else
			Uri service = new Uri( "http://places.nlp.nokia.com/places/v1/suggest?at=" + myLocation + "&q=" + prefix + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application/json" );
#endif

			placeSearch.DownloadStringCompleted += DownloadStringCompleted;
			placeSearch.DownloadStringAsync( service, userObject );
		}

		protected override void OnNavigatingFrom( System.Windows.Navigation.NavigatingCancelEventArgs e )
		{
			base.OnNavigatingFrom( e );

			if( PhoneApplicationService.Current.State.ContainsKey( "loadedRoute" ) && PhoneApplicationService.Current.State["loadedRoute"] != null )
			{
				PhoneApplicationService.Current.State.Remove( "loadedRoute" );
			}
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			if( NavigationContext.QueryString.ContainsKey( "plan" ) )
			{
				hideRouteOptions = ( NavigationContext.QueryString["plan"].Equals( "leisure" ) ) ? true : false;
			}

			if( !SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) )
			{
				routeTutorial1.Visibility = Visibility.Collapsed;
				routeTutorial2.Visibility = Visibility.Collapsed;
				routeTutorial3.Visibility = Visibility.Collapsed;
				routeTutorial4.Visibility = Visibility.Collapsed;
				routeTutorial5.Visibility = Visibility.Collapsed;
			}
			routeTutorial1.Visibility = Visibility.Collapsed;

			bool shownTutorial = SettingManager.instance.GetBoolValue( "shownTutorial", false );

			if( !shownTutorial )
			{
				bool shownTutorialQuestion = SettingManager.instance.GetBoolValue( "shownTutorialQuestion", false );

				if( !shownTutorialQuestion )
				{
					MessageBoxResult result = MessageBox.Show( "Would you like us to guide you through how to use the app with a tutorial?", "Tutorial", MessageBoxButton.OKCancel );
					if( result == MessageBoxResult.Cancel )
					{
						shownTutorial = true;
						SettingManager.instance.SetBoolValue( "shownTutorial", true );
						SettingManager.instance.SetBoolValue( "tutorialEnabled", false );
					}
					else
					{
						routeTutorial1.Visibility = Visibility.Visible;
						SettingManager.instance.SetBoolValue( "tutorialEnabled", true );
					}

					SettingManager.instance.SetBoolValue( "shownTutorialQuestion", true );
				}
				else
				{
					if( SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) )
						routeTutorial1.Visibility = Visibility.Visible;

				}
			}

			if( NavigationContext.QueryString.ContainsKey( "longitude" ) )
			{
				GeoCoordinate center = new GeoCoordinate();
				center.Longitude = float.Parse( NavigationContext.QueryString["longitude"] );
				center.Latitude = float.Parse( NavigationContext.QueryString["latitude"] );

				string plan = SettingManager.instance.GetStringValue( "defaultRouteType", "balanced route" );
				plan = plan.Replace( " route", "" );

				string speedSetting = SettingManager.instance.GetStringValue( "cycleSpeed", "12mph" );

				string itinerarypoints = LocationManager.instance.MyGeoPosition.Coordinate.Longitude + "," + LocationManager.instance.MyGeoPosition.Coordinate.Latitude + "|" + center.Longitude + "," + center.Latitude;// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
				int speed = Util.getSpeedFromString( speedSetting );
				int useDom = 0;		// 0=xml 1=gml

				AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.json?key=" + App.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
				_request.Start();

				Pushpin start = new Pushpin();
				start.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( LocationManager.instance.MyGeoPosition.Coordinate );
				Pushpin end = new Pushpin();
				end.GeoCoordinate = center;
				waypoints.Add( start );
				waypoints.Add( end );

				App.networkStatus.networkIsBusy = true;
			}

			if( hideRouteOptions )
				routeTypePicker.Visibility = Visibility.Collapsed;
			else
				routeTypePicker.Visibility = Visibility.Visible;


		}

		private void DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
		{
			AutoCompleteBox acb = e.UserState as AutoCompleteBox;
			if( acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty( e.Result ) )
			{
				System.Diagnostics.Debug.WriteLine( e.Result );
				JObject o = JObject.Parse( e.Result );
				JArray suggestions = (JArray)o["suggestions"];
				List<string> names = new List<string>();
				if( suggestions.Count > 0 )
				{
					foreach( string s in suggestions )
					{
						names.Add( s );
					}

					if( names.Count > 0 )
					{
						acb.ItemsSource = null;
						acb.ItemsSource = names;
						acb.PopulateComplete();
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine( "Starting backup search for " + startPoint.Text );
					placeSearch = new WebClient();
					string prefix = HttpUtility.UrlEncode( startPoint.Text );

					string myLocation = "";
					LocationRectangle rect = GetMapBounds();
					myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
					//myLocation = HttpUtility.UrlEncode( myLocation );

					Uri service = new Uri( "http://cambridge.cyclestreets.net/api/geocoder.json?key=" + App.apiKey + myLocation + "&street=" + prefix );

					placeSearch.DownloadStringCompleted += CSDownloadStringCompleted;
					placeSearch.DownloadStringAsync( service, e.UserState );
				}
				App.networkStatus.networkIsBusy = false;
			}
		}

		private void CSDownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
		{
			placeSearch = null;
			AutoCompleteBox acb = e.UserState as AutoCompleteBox;
			if( acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty( e.Result ) )
			{
				System.Diagnostics.Debug.WriteLine( e.Result );
				JObject o = JObject.Parse( e.Result );
				JArray suggestions = null;

				if( o["results"] != null && o["results"]["result"] != null )
				{
					if( o["results"]["result"] is JArray )
						suggestions = (JArray)o["results"]["result"];
					else
					{
						suggestions = new JArray();
						suggestions.Add( (JObject)o["results"]["result"] );
					}
					if( suggestions.Count > 0 )
					{
						List<CSResult> names = new List<CSResult>();
						foreach( JObject s in suggestions )
						{
							CSResult res = new CSResult();
							res.coord = new GeoCoordinate( double.Parse( s["latitude"].ToString() ), double.Parse( s["longitude"].ToString() ) );
							res.resultName = s["name"].ToString();
							if( !string.IsNullOrWhiteSpace( s["near"].ToString() ) )
								res.resultName += ", " + s["near"].ToString();
							names.Add( res );
						}
						acb.ItemsSource = null;
						acb.ItemsSource = names;
					}
				}

				else
				{
					List<string> names = new List<string>();
					names.Add( "No suggestions" );

					acb.ItemsSource = null;
					acb.ItemsSource = names;
				}
				acb.PopulateComplete();
			}
		}

		private LocationRectangle GetMapBounds()
		{
			GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate( new Point( 0, 0 ) );
			GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate( new Point( MyMap.ActualWidth, MyMap.ActualHeight ) );

			return LocationRectangle.CreateBoundingRectangle( new[] { topLeft, bottomRight } );
		}

		private void revGeoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			if( e.Result == null || e.Result.Count <= 0 )
				return;
			MapLocation loc = e.Result[0];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
		}

		private void startPoint_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			if( startPoint.SelectedItem != null )
			{
				if( startPoint.SelectedItem is CSResult )
				{
					CSResult res = startPoint.SelectedItem as CSResult;
					setCurrentPosition( res.coord );

					SmartDispatcher.BeginInvoke( () =>
					{
						MyMap.SetView( res.coord, 16 );
						this.Focus();
						//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
					} );
					App.networkStatus.networkIsBusy = false;
				}
				else
				{
					string start = startPoint.SelectedItem as string;

					if( !geoQ.IsBusy && !string.IsNullOrWhiteSpace( start ) && start != "No suggestions" )
					{
						geoQ.SearchTerm = start;
						geoQ.GeoCoordinate = MyMap.Center;
						geoQ.QueryAsync();
						App.networkStatus.networkIsBusy = true;

					}
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
				App.networkStatus.networkIsBusy = false;
				this.Focus();
			}
			else
			{
				GeocodeQuery geo = sender as GeocodeQuery;
				string searchString = geo.SearchTerm;

				WebClient wc = new WebClient();
				string myLocation = "";
				myLocation = geo.GeoCoordinate.Latitude + "," + geo.GeoCoordinate.Longitude;
				myLocation = HttpUtility.UrlEncode( myLocation );

#if DEBUG
				Uri service = new Uri( "http://demo.places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson" );
#else
				Uri service = new Uri( "http://places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson" );
#endif

				wc.DownloadStringCompleted += DiscoverStringCompleted;
				wc.DownloadStringAsync( service );
				//
			}
		}

		private void DiscoverStringCompleted( object sender, DownloadStringCompletedEventArgs e )
		{
			if( e.Error == null && !e.Cancelled && !string.IsNullOrEmpty( e.Result ) )
			{
				System.Diagnostics.Debug.WriteLine( e.Result );
				JObject o = JObject.Parse( e.Result );
				JArray suggestions = (JArray)o["results"]["items"];
				if( suggestions.Count > 0 )
				{
					JArray pos = (JArray)suggestions[0]["position"];
					GeoCoordinate g = new GeoCoordinate( (double)pos[0], (double)pos[1] );
					setCurrentPosition( g );

					SmartDispatcher.BeginInvoke( () =>
					{
						MyMap.SetView( g, 16 );
						//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
					} );
					App.networkStatus.networkIsBusy = false;
				}
			}
			App.networkStatus.networkIsBusy = false;
			this.Focus();
		}

		private void myPosition_Click( object sender, EventArgs e )
		{
			if( LocationManager.instance.MyGeoPosition != null )
			{
				if( !revGeoQ.IsBusy )
				{
					revGeoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( LocationManager.instance.MyGeoPosition.Coordinate );
					revGeoQ.QueryAsync();

					setCurrentPosition( revGeoQ.GeoCoordinate );

					SmartDispatcher.BeginInvoke( () =>
					{
						MyMap.SetView( revGeoQ.GeoCoordinate, 16 );
						//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
					} );
				}
			}
			else
			{
				Util.showLocationDialog();

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
				last.Style = Resources["Intermediate"] as Style;
				last.Content = "" + ( waypoints.Count - 1 );
			}

			Pushpin pp = new Pushpin();
			if( waypoints.Count == 0 )
				pp.Style = Resources["Start"] as Style;
			else
				pp.Style = Resources["Finish"] as Style;

			pp.Tap += pinTapped;

			MapOverlay overlay = new MapOverlay();
			overlay.Content = pp;
			pp.GeoCoordinate = current;
			overlay.GeoCoordinate = current;
			overlay.PositionOrigin = new Point( 0.3, 1.0 );
			wayPointLayer.Add( overlay );

			addWaypoint( pp );

			if( SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) )
			{
				bool shownTutorial = SettingManager.instance.GetBoolValue( "shownTutorialPin", false );
				if( !shownTutorial )
					routeTutorialPin.Visibility = Visibility.Visible;
			}
			// Clear box
			startPoint.Text = "";

			clearCurrentPosition();
		}

		private void pinTapped( object sender, System.Windows.Input.GestureEventArgs e )
		{
			removeWaypoint( sender as Pushpin );

			wayPointLayer.Clear();

			for( int i = 0; i < waypoints.Count; i++ )
			{
				MapOverlay overlay = new MapOverlay();
				overlay.Content = waypoints[i];
				overlay.GeoCoordinate = waypoints[i].GeoCoordinate;
				overlay.PositionOrigin = new Point( 0.3, 1.0 );
				wayPointLayer.Add( overlay );

				// Set pin styles
				Pushpin pp = waypoints[i];
				if( i == 0 )
					pp.Style = Resources["Start"] as Style;
				else if( i == waypoints.Count - 1 )
					pp.Style = Resources["Finish"] as Style;
				else
				{
					pp.Style = Resources["Intermediate"] as Style;
					pp.Content = "" + i;
				}
			}
		}

		private void addWaypoint( Pushpin pp )
		{
			waypoints.Push( pp );
			if( waypoints.Count >= 2 )
				findRoute.IsEnabled = true;
			else
				findRoute.IsEnabled = false;
		}

		private void removeWaypoint( Pushpin pp )
		{
			waypoints.Remove( pp );
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
			this.Focus();

			string plan = SettingManager.instance.GetStringValue( "defaultRouteType", "balanced route" );
			plan = plan.Replace( " route", "" );

			string speedSetting = SettingManager.instance.GetStringValue( "cycleSpeed", "12mph" );

			string itinerarypoints = "";// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
			int speed = Util.getSpeedFromString( speedSetting );
			int useDom = 0;		// 0=xml 1=gml

			foreach( Pushpin p in waypoints )
			{
				itinerarypoints = itinerarypoints + p.GeoCoordinate.Longitude + "," + p.GeoCoordinate.Latitude + "|";
			}
			itinerarypoints = itinerarypoints.TrimEnd( '|' );

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.json?key=" + App.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
			_request.Start();

			App.networkStatus.networkIsBusy = true;
		}

		private void RouteFound( byte[] data )
		{
			if( data == null )
			{
				return;
			}

			UTF8Encoding enc = new UTF8Encoding();

			RouteFound( enc.GetString( data, 0, data.Length ) );
		}

		private void RouteFound( string data )
		{
			currentRouteData = data;

			JObject o = null;
			if( currentRouteData != null )
				o = JObject.Parse( currentRouteData.Trim() );
			if( o == null || o["marker"] == null )
			{
				MessageBoxResult result = MessageBox.Show( "No route found. Try another search", "No Route", MessageBoxButton.OK );
				NavigationService.GoBack();
				return;
			}
			geometryCoords.Clear();
			facts.Clear();
			route.segments.Clear();
			MyMap.MapElements.Clear();

			max = new GeoCoordinate( 90, -180 );
			min = new GeoCoordinate( -90, 180 );

			currentStep = -1;

			arrowLeft.Opacity = 50;
			arrowRight.Opacity = 100;

			route = new RouteDetails();

			List<RouteManeuver> manouvers = new List<RouteManeuver>();
			JArray steps = o["marker"] as JArray;
			JArray pois = o["poi"] as JArray;
			string col1 = "#7F000000";
			string col2 = "#3F000000";
			bool swap = true;
			int totalTime = 0;
			foreach( JObject step in steps )
			{
				JObject p = (JObject)step["@attributes"];
				string markerType = (string)p["type"];
				if( markerType == "route" )
				{
					route.routeIndex = int.Parse( (string)p["itinerary"] );
					JourneyFactItem i = new JourneyFactItem( "Assets/picture.png" );
					i.Caption = "Route Number";
					i.Value = "" + route.routeIndex;
					facts.Add( i );
					route.timeInSeconds = int.Parse( (string)p["time"] );
					i = new JourneyFactItem( "Assets/clock.png" );
					i.Caption = "Journey time";
					i.Value = UtilTime.secsToLongDHMS( route.timeInSeconds );
					facts.Add( i );
					route.quietness = float.Parse( (string)p["quietness"] );
					i = new JourneyFactItem( "Assets/picture.png" );
					i.Caption = "Quietness";
					i.Value = route.quietness + "% " + getQuietnessString( route.quietness );
					facts.Add( i );
					route.signalledJunctions = int.Parse( (string)p["signalledJunctions"] );
					i = new JourneyFactItem( "Assets/traffic_signals.png" );
					i.Caption = "Signalled Junctions";
					i.Value = "" + route.signalledJunctions;
					facts.Add( i );
					route.signalledCrossings = int.Parse( (string)p["signalledCrossings"] );
					i = new JourneyFactItem( "Assets/traffic_signals.png" );
					i.Caption = "Signalled Crossings";
					i.Value = "" + route.signalledCrossings;
					facts.Add( i );
					route.grammesCO2saved = int.Parse( (string)p["grammesCO2saved"] );
					i = new JourneyFactItem( "Assets/world.png" );
					i.Caption = "CO2 avoided";
					i.Value = (float)route.grammesCO2saved / 1000f + " kg";
					facts.Add( i );
					route.calories = int.Parse( (string)p["calories"] );
					i = new JourneyFactItem( "Assets/heart.png" );
					i.Caption = "Calories";
					i.Value = route.calories + " kcal";
					facts.Add( i );
				}
				else if( markerType == "segment" )
				{
					string pointsText = (string)p["points"];
					string[] points = pointsText.Split( ' ' );
					List<GeoCoordinate> coords = new List<GeoCoordinate>();
					for( int i = 0; i < points.Length; i++ )
					{
						string[] xy = points[i].Split( ',' );

						double longitude = double.Parse( xy[0] );
						double latitude = double.Parse( xy[1] );
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
					geometryColor.Add( ConvertHexStringToColour( (string)p["color"] ) );

					RouteSegment s = new RouteSegment();
					s.location = coords[0];
					s.Bearing = Geodesy.Bearing( coords[0].Latitude, coords[0].Longitude, coords[coords.Count - 1].Latitude, coords[coords.Count - 1].Longitude );
					route.distance += (int)float.Parse( (string)p["distance"] );
					s.Distance = "" + route.distance;// p.Attribute( "distance" ).Value;
					s.DistanceMetres = (int)float.Parse( (string)p["distance"] );
					s.Name = (string)p["name"];
					s.ProvisionName = (string)p["provisionName"];
					int theLegOfTime = int.Parse( (string)p["time"] );
					s.Time = "" + ( totalTime + theLegOfTime );
					totalTime += theLegOfTime;
					s.Turn = (string)p["turn"];
					s.Walk = ( int.Parse( (string)p["walk"] ) == 1 ? true : false );
					if( swap )
						s.BGColour = col1;
					else
						s.BGColour = col2;
					swap = !swap;
					route.segments.Add( s );
				}
			}
			if( poiLayer == null )
			{
				poiLayer = new MapLayer();

				MyMap.Layers.Add( poiLayer );
			}
			else
			{
				poiLayer.Clear();
			}
			int id = 0;
			if( pois != null )
			{
				foreach( JObject poi in pois )
				{
					JObject p = (JObject)poi["@attributes"];
					POI poiItem = new POI();
					poiItem.Name = (string)p["name"];
					GeoCoordinate g = new GeoCoordinate();
					g.Longitude = float.Parse( (string)p["longitude"] );
					g.Latitude = float.Parse( (string)p["latitude"] );
					poiItem.Position = g;

					poiItem.PinID = "" + ( id++ );

					Pushpin pp = new Pushpin();
					pp.Content = poiItem.PinID;
					pp.Tap += poiTapped;

					pinItems.Add( pp, poiItem );

					MapOverlay overlay = new MapOverlay();
					overlay.Content = pp;
					pp.GeoCoordinate = poiItem.GetGeoCoordinate();
					overlay.GeoCoordinate = poiItem.GetGeoCoordinate();
					overlay.PositionOrigin = new Point( 0, 1.0 );
					poiLayer.Add( overlay );
				}
			}
			JourneyFactItem item = new JourneyFactItem( "Assets/bullet_go.png" );
			item.Caption = "Distance";
			float dist = (float)route.distance * 0.000621371192f;
			item.Value = dist.ToString( "0.00" ) + " miles";
			facts.Add( item );

			SmartDispatcher.BeginInvoke( () =>
			{
				if( geometryCoords.Count == 0 )
				{
					MessageBox.Show( "Could not calculate route" );
					App.networkStatus.networkIsBusy = false;
					return;
				}

				LocationRectangle rect;
				try
				{
					rect = new LocationRectangle( min, max );
					MyMap.SetView( rect );
				}
				catch( System.Exception ex )
				{
					System.Diagnostics.Debug.WriteLine( "Invalid box" );
				}

				//MyMap.Center = new GeoCoordinate(min.Latitude + ((max.Latitude - min.Latitude) / 2f), min.Longitude + ((max.Longitude - min.Longitude) / 2f));
				//MyMap.ZoomLevel = 10;
				int count = geometryCoords.Count;
				for( int i = 0; i < count; i++ )
				{
					List<GeoCoordinate> coords = geometryCoords[i];
					DrawMapMarker( coords.ToArray(), geometryColor[i] );
				}

				//NavigationService.Navigate( new Uri( "/Pages/DirectionsResults.xaml", UriKind.Relative ) );
				var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
				var sg = sgs[0] as VisualStateGroup;
				//ExtendedVisualStateManager.GoToElementState( LayoutRoot, "RouteFoundState", true );
				VisualStateManager.GoToState( this, "RouteFoundState", true );

				App.networkStatus.networkIsBusy = false;

				float f = (float)route.distance * 0.000621371192f;
				findLabel1.Text = f.ToString( "0.00" ) + "m\n" + UtilTime.secsToLongDHMS( route.timeInSeconds );

				if( SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) )
				{
					bool shownTutorial = SettingManager.instance.GetBoolValue( "shownTutorialRouteType", false );
					if( !shownTutorial )
						routeTutorialRouteType.Visibility = Visibility.Visible;
				}
			} );

			saveRoute.IsEnabled = true;
		}

		private void poiTapped( object sender, System.Windows.Input.GestureEventArgs e )
		{
			Pushpin pp = sender as Pushpin;
			POI p = pinItems[pp];
			foreach( KeyValuePair<Pushpin, POI> pair in pinItems )
			{
				Pushpin ppItem = pair.Key;
				POI pItem = pair.Value;
				ppItem.Content = pItem.PinID;
			}
			pp.Content = p.Name;

			//selected = p.GetGeoCoordinate();
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
				polygon.Path.Add( coordinate[i] );
			}

			MyMap.MapElements.Add( polygon );
		}

		private Color ConvertHexStringToColour( string hexString )
		{
			//byte a = 0;
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
			plan = plan.Replace( " route", "" );
			if( saveRoute.IsEnabled && !hideRouteOptions )
			{
				string itinerarypoints = "";// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
				int speed = 20;		//16 = 10mph 20 = 12mph 24 = 15mph
				int useDom = 0;		// 0=xml 1=gml

				foreach( Pushpin p in waypoints )
				{
					itinerarypoints = itinerarypoints + p.GeoCoordinate.Longitude + "," + p.GeoCoordinate.Latitude + "|";
				}
				itinerarypoints = itinerarypoints.TrimEnd( '|' );

				MyMap.MapElements.Clear();

				AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.json?key=" + App.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound );
				_request.Start();

				App.networkStatus.networkIsBusy = true;
			}
		}

		private void Image_Tap_1( object sender, System.Windows.Input.GestureEventArgs e )
		{
			PhoneApplicationService.Current.State["route"] = currentRouteData;
			PhoneApplicationService.Current.State["routeIndex"] = route.routeIndex;
			NavigationService.Navigate( new Uri( "/Pages/DirectionsResults.xaml", UriKind.Relative ) );
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

				SetMapStyle();
			}
			else
			{
				arrowLeft.Opacity = 100;

				MyMap.SetView( route.segments[currentStep].location, 20, route.segments[currentStep].Bearing, 75 );

				MyMap.TileSources.Clear();
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
				MyMap.SetView( route.segments[currentStep].location, 20, route.segments[currentStep].Bearing, 75 );

				findLabel1.Text = route.segments[currentStep].Turn + " at " + route.segments[currentStep].Name + "\n Continue for " + route.segments[currentStep].DistanceMetres + "m";

				MyMap.TileSources.Clear();
			}
		}

		private void routeTutorial1_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorial2.Visibility = Visibility.Visible;
			routeTutorial1.Visibility = Visibility.Collapsed;
		}

		private void routeTutorial2_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorial3.Visibility = Visibility.Visible;
			routeTutorial2.Visibility = Visibility.Collapsed;
		}

		private void routeTutorial3_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorial4.Visibility = Visibility.Visible;
			routeTutorial3.Visibility = Visibility.Collapsed;
		}

		private void routeTutorial4_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorial5.Visibility = Visibility.Visible;
			routeTutorial4.Visibility = Visibility.Collapsed;
		}

		private void routeTutorial5_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorial5.Visibility = Visibility.Collapsed;
			SettingManager.instance.SetBoolValue( "shownTutorial", true );
		}

		private void routeTutorialPin_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorialPin.Visibility = Visibility.Collapsed;
			SettingManager.instance.SetBoolValue( "shownTutorialPin", true );
		}

		private void routeTutorialRouteType_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorialRouteInfo.Visibility = Visibility.Visible;
			routeTutorialRouteType.Visibility = Visibility.Collapsed;
		}

		private void routeTutorialRouteInfo_Tap( object sender, System.Windows.Input.GestureEventArgs e )
		{
			routeTutorialRouteInfo.Visibility = Visibility.Collapsed;
			SettingManager.instance.SetBoolValue( "shownTutorialRouteType", true );
		}

		private void settings_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/Settings.xaml", UriKind.Relative ) );
		}

		private void privacy_Click( object sender, System.EventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://www.cyclestreets.net/privacy/" );
			url.Show();
		}

		private void MyMap_Loaded( object sender, RoutedEventArgs e )
		{
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";

			if( PhoneApplicationService.Current.State.ContainsKey( "loadedRoute" ) && PhoneApplicationService.Current.State["loadedRoute"] != null )
			{
				string routeData = (string)PhoneApplicationService.Current.State["loadedRoute"];
				hideRouteOptions = true;
				RouteFound( routeData );
				PhoneApplicationService.Current.State["loadedRoute"] = null;
			}

			routeTypePicker.Visibility = hideRouteOptions ? Visibility.Collapsed : Visibility.Visible;

			positionChangedHandler( null, null );

			SetMapStyle();
		}

		private void SetMapStyle()
		{
			MyTileSource ts;
			switch (SettingManager.instance.GetStringValue("mapStyle", MapStyle[0]))
			{
				case "OpenStreetMap":
					ts = new OSMTileSource();
					break;
				case "OpenCycleMap":
					ts = new OSMTileSource();
					break;
				default:
					ts = null;
					break;
			}
			MyMap.TileSources.Clear();
			if (ts != null)
				MyMap.TileSources.Add(ts);
		}

		private void startPoint_KeyUp( object sender, System.Windows.Input.KeyEventArgs e )
		{
			if( e.Key == Key.Enter )
			{
				AutoCompleteBox box = sender as AutoCompleteBox;
				string text = box.Text;
				if( text.Length == 6 || text.Length == 7 )
				{
					if( char.IsLetter( text[0] ) && char.IsLetter( text[text.Length - 1] ) && char.IsLetter( text[text.Length - 2] )
						 && char.IsNumber( text[text.Length - 3] ) && char.IsNumber( text[text.Length - 4] ) )
					{
						// This is a postcode
						string newPostcodeEnd = text.Substring( text.Length - 3, 3 );
						string newPostcodeStart = text.Substring( 0, text.Length - 3 );
						box.Text = newPostcodeStart + " " + newPostcodeEnd;
					}
				}
				StartPlaceSearch( box.Text, box );

				geoQ.SearchTerm = box.Text;
				geoQ.GeoCoordinate = MyMap.Center;
				geoQ.QueryAsync();
				App.networkStatus.networkIsBusy = true;

				this.Focus();
			}
		}

		private void saveRoute_Click( object sender, EventArgs e )
		{
			PhoneApplicationService.Current.State["route"] = currentRouteData;
			NavigationService.Navigate( new Uri( "/Pages/SaveRoute.xaml", UriKind.Relative ) );
		}

		private void loadRoute_Click( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/LoadRoute.xaml", UriKind.Relative ) );
		}

		private void startPoint_Populated( object sender, PopulatedEventArgs e )
		{
			/*foreach (object o in e.Data)
			{
				if( o is CSResult )
					Debug.WriteLine( ( (CSResult)o ).resultName );
				else
					Debug.WriteLine( (string)o );
			}*/

		}

		private MapOverlay myLocationOverlay = null;
		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			SmartDispatcher.BeginInvoke( () =>
				{
					if( LocationManager.instance.MyGeoPosition != null )
					{

						if( myLocationOverlay == null )
						{
							// 				Arc myArc = new Arc();
							// 				myArc.ArcThickness = 5;
							// 				myArc.ArcThicknessUnit = Microsoft.Expression.Media.UnitType.Pixel;
							// 				myArc.EndAngle = 360;
							// 				myArc.StartAngle = 0;
							// 				myArc.Height = 30;
							// Create a small circle to mark the current location.
							Ellipse myCircle = new Ellipse();
							myCircle.Fill = new SolidColorBrush( Colors.Black );
							myCircle.Height = 20;
							myCircle.Width = 20;
							myCircle.Opacity = 30;
							Binding myBinding = new Binding( "Visible" );
							myBinding.Source = new MyPositionDataSource( MyMap );
							myCircle.Visibility = Visibility.Visible;
							myCircle.SetBinding( Ellipse.VisibilityProperty, myBinding );

							// Create a MapOverlay to contain the circle.
							myLocationOverlay = new MapOverlay();
							myLocationOverlay.Content = myCircle;
							myLocationOverlay.PositionOrigin = new Point( 0.5, 0.5 );
							myLocationOverlay.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( LocationManager.instance.MyGeoPosition.Coordinate );

							// Create a MapLayer to contain the MapOverlay.
							MapLayer myLocationLayer = new MapLayer();
							myLocationLayer.Add( myLocationOverlay );

							MyMap.Layers.Add( myLocationLayer );
						}

						myLocationOverlay.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate( LocationManager.instance.MyGeoPosition.Coordinate );
					}
				} );
		}

		private void sendFeedback_Click( object sender, EventArgs e )
		{
			// 			EmailComposeTask task = new EmailComposeTask();
			// 			task.Subject = "CycleStreets [WP8] feedback";
			// 			task.To = "info@cyclestreets.net";
			// 			task.Show();
			NavigationService.Navigate( new Uri( "/Pages/Feedback.xaml", UriKind.Relative ) );
		}

	}

	public class MyPositionDataSource : INotifyPropertyChanged
	{
		private Map MyMap;

		public MyPositionDataSource( Map MyMap )
		{
			// TODO: Complete member initialization
			this.MyMap = MyMap;
			MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;
		}

		private void MyMap_ZoomLevelChanged( object sender, MapZoomLevelChangedEventArgs e )
		{
			this.Visible = _isVisible;
		}

		private Visibility _isVisible = Visibility.Visible;
		public Visibility Visible
		{
			set
			{
				if( MyMap.ZoomLevel > 12 && _isVisible == Visibility.Collapsed )
				{
					_isVisible = Visibility.Visible;
					NotifyPropertyChanged( "Visible" );
				}
				else if( MyMap.ZoomLevel <= 12 && _isVisible == Visibility.Visible )
				{
					_isVisible = Visibility.Collapsed;
					NotifyPropertyChanged( "Visible" );
				}
			}
			get
			{
				return _isVisible;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged( String propertyName )
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if( null != handler )
			{
				handler( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
	}
}