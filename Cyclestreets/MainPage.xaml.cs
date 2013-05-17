using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	public class POIItem
	{
		public string POILabel
		{
			get;
			set;
		}
		public bool POIEnabled
		{
			get;
			set;
		}
		public string POIName
		{
			get;
			set;
		}
	}

	public partial class MainPage : PhoneApplicationPage
	{
		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();
		List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
		Dictionary<Pushpin, POI> pinItems = new Dictionary<Pushpin, POI>();

		private Geolocator trackingGeolocator;
		public static Geoposition MyGeoPosition = null;

		private GeoCoordinate max = new GeoCoordinate( 90, -180 );
		private GeoCoordinate min = new GeoCoordinate( -90, 180 );

		private GeoCoordinate _selected;
		public GeoCoordinate selected
		{
			get
			{
				return _selected;
			}
			set
			{
				if( value == null )
					navigateToAppBar.IsEnabled = false;
				else
					navigateToAppBar.IsEnabled = true;
				_selected = value;
			}
		}

		ReverseGeocodeQuery geoQ = null;
		private MapLayer poiLayer;
		private bool lockToMyPos = false;

		// Constructor
		public MainPage()
		{
			InitializeComponent();
			this.StartTracking();

			// hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
			/*findAppBar = ApplicationBar.Buttons[ 0 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;*/
			directionsAppBar = ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			navigateToAppBar = ApplicationBar.Buttons[1] as Microsoft.Phone.Shell.ApplicationBarIconButton;

			geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[0] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, ( (VisualState)sg.States[0] ).Name, true );
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			if( IsolatedStorageSettings.ApplicationSettings.Contains( "LocationConsent" ) && (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] )
			{
				if( poiLayer == null )
				{
					poiLayer = new MapLayer();

					MyMap.Layers.Add( poiLayer );
				}
				else
				{
					poiLayer.Clear();
				}
				if( NavigationContext.QueryString.ContainsKey( "longitude" ) )
				{
					GeoCoordinate center = new GeoCoordinate();
					center.Longitude = float.Parse( NavigationContext.QueryString["longitude"] );
					center.Latitude = float.Parse( NavigationContext.QueryString["latitude"] );
					MyMap.Center = center;
					MyMap.ZoomLevel = 16;
					lockToMyPos = false;

					selected = center;
				}
				else
				{
					lockToMyPos = true;
					if( MyGeoPosition != null )
						MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 14 );
				}

				if( POIResults.pois != null && POIResults.pois.Count > 0 )
				{
					pinItems.Clear();
					foreach( POI p in POIResults.pois )
					{
						Pushpin pp = new Pushpin();
						pinItems.Add( pp, p );
						pp.Content = p.PinID;
						pp.Tap += poiTapped;

						ContextMenu ctxt = new ContextMenu();


						MapOverlay overlay = new MapOverlay();
						overlay.Content = pp;
						pp.GeoCoordinate = p.GetGeoCoordinate();
						overlay.GeoCoordinate = p.GetGeoCoordinate();
						overlay.PositionOrigin = new Point( 0, 1.0 );
						poiLayer.Add( overlay );
					}

				}
			}
			else
			{
				MessageBoxResult result =
					MessageBox.Show( "CycleStreets requires access to your location in order to provide navigation and mapping information. Do you want to allow this?",
					"Location",
					MessageBoxButton.OKCancel );

				if( result == MessageBoxResult.OK )
				{
					IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
				}
				else
				{
					IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
				}

				IsolatedStorageSettings.ApplicationSettings.Save();
			}


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

			selected = p.GetGeoCoordinate();
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
			if( lockToMyPos )
			{
				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 14 );
				} );
			}
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

		private void ApplicationBarIconButton_NavigateTo( object sender, EventArgs e )
		{
			if( selected != null )
				NavigationService.Navigate( new Uri( "/Directions.xaml?longitude=" + selected.Longitude + "&latitude=" + selected.Latitude, UriKind.Relative ) );
		}

		private void poiList_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/POIList.xaml?longitude=" + MyMap.Center.Longitude + "&latitude=" + MyMap.Center.Latitude, UriKind.Relative ) );
		}

		private void settings_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Settings.xaml", UriKind.Relative ) );
		}

		private void MyMap_Loaded( object sender, RoutedEventArgs e )
		{
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";
		}

		private void privacy_Click( object sender, System.EventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://www.cyclestreets.net/privacy/" );
			url.Show();
		}
	}
}