using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using System.Xml.Linq;
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
using RestSharp;

namespace Cyclestreets
{
	public partial class MainPage : PhoneApplicationPage
	{
		readonly Dictionary<Pushpin, POI> pinItems = new Dictionary<Pushpin, POI>();

		private GeoCoordinate _selected;

		private GeoCoordinate selected
		{
			get
			{
				return _selected;
			}
			set
			{
				navigateToAppBar.IsEnabled = value != null;
				_selected = value;
			}
		}

		private MapLayer poiLayer;

		// Declare the MarketplaceDetailTask object with page scope
		// so we can access it from event handlers.
		readonly MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();

		// Constructor
		public MainPage()
		{
			InitializeComponent();

			// hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
			/*findAppBar = ApplicationBar.Buttons[ 0 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;*/
			directionsAppBar = ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			navigateToAppBar = ApplicationBar.Buttons[1] as Microsoft.Phone.Shell.ApplicationBarIconButton;

			if( LocationManager.instance == null )
			{
				LocationManager l = new LocationManager( MyMap );
			}

			ReverseGeocodeQuery geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			var sgs = VisualStateManager.GetVisualStateGroups( LayoutRoot );
			var sg = sgs[0] as VisualStateGroup;
			ExtendedVisualStateManager.GoToElementState( LayoutRoot, ( (VisualState)sg.States[0] ).Name, true );
		}

		void m_Click( object sender, EventArgs e )
		{
			_marketPlaceDetailTask.Show();

		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			var app = Application.Current as App;
			if( app != null && app.IsTrial )
			{
				//if( ( (App)App.Current ).trialExpired )
				//	( (App)App.Current ).showTrialExpiredMessage();


				String udid = Util.GetHardwareId();

				String serverUrl = "http://www.rwscripts.com/cyclestreets/trial.php";

				var client = new RestClient( serverUrl );

				var request = new RestRequest( "", Method.POST );
				request.AddParameter( "Hardware", udid ); // adds to POST or URL querystring based on Method

				// easily add HTTP Headers
				request.AddHeader( "header", "value" );

				// execute the request
				//RestResponse response = client.ExecuteAsync(request, sendPushURLCallback);
				//var content = response.Content; // raw content as string

				// easy async support
				client.ExecuteAsync( request, response =>
				{
					if( Util.ResultContainsErrors( response.Content, "sendPushToken" ) )
					{
						Util.dataFailure();
					}
					else if( response.StatusCode != HttpStatusCode.OK )
					{
						Util.networkFailure();
					}
					else
					{
						try
						{
							XDocument xml = XDocument.Parse( response.Content.Trim() );
							var session = xml.Descendants( "root" );
							foreach( XElement s in session )
							{
								if( s.Element( "trialID" ) != null )
								{
									( (App)App.Current ).trialID = int.Parse( s.Element( "trialID" ).Value );
								}
								if( s.Element( "result" ) != null )
								{
									if( int.Parse( s.Element( "result" ).Value ) == 0 )
									{
										NavigationService.Navigate( new Uri( "/Pages/TrialExpired.xaml", UriKind.Relative ) );

										( (App)App.Current ).trialExpired = true;
										//( (App)App.Current ).showTrialExpiredMessage();
										//if( NavigationService.CanGoBack )
										//	NavigationService.GoBack();
									}
									else
									{
										MarkedUp.AnalyticClient.TrialConversionOfferShown();
										MessageBoxResult result = MessageBox.Show( "Thank you for installing the CycleStreets trial. This trial lasts 24 hours and allows access to all the features of the full app. You can purchase the full version at any time from the action panel.", "Hello", MessageBoxButton.OK );
										ApplicationBarMenuItem m = new ApplicationBarMenuItem( "buy full version" );
										m.Click += m_Click;
										ApplicationBar.MenuItems.Add( m );
										MarkedUp.AnalyticClient.TrialConversionOfferDismissed();
										SettingManager.instance.SetBoolValue( "appIsTrial", true );
									}
								}

							}
						}
						catch( Exception ex )
						{
							FlurryWP8SDK.Api.LogError( "Failed to parse login xml", ex );
						}
					}
				} );
			}
			else
			{
				// Conversion tracking
				if( SettingManager.instance.GetBoolValue( "appIsTrial", false ) )
				{
					MarkedUp.AnalyticClient.TrialConversionComplete();
					SettingManager.instance.SetBoolValue( "appIsTrial", false );
				}

				if( !SettingManager.instance.GetBoolValue( "RatedApp", false ) )
				{
					if( SettingManager.instance.GetIntValue( "LaunchCount", 0 ) > 0 && ( SettingManager.instance.GetIntValue( "LaunchCount", 0 ) % 5 ) == 0 )
					{
						CustomMessageBox msg = new CustomMessageBox();
						msg.Message = "Would you be kind enough to help our app get noticed by rating us in the store? If you don't think our app is worth 4 or 5 stars then please consider sending us feedback instead so we can improve our app.";
						msg.IsLeftButtonEnabled = true;
						msg.IsRightButtonEnabled = true;
						msg.LeftButtonContent = "Rate";
						msg.RightButtonContent = "Don't ask again";
						msg.Title = "Rate CycleStreets";

						FlurryWP8SDK.Api.LogEvent( "Rate App Shown" );

						msg.Dismissed += ( s1, e1 ) =>
						{
							switch( e1.Result )
							{
								case CustomMessageBoxResult.LeftButton:
									FlurryWP8SDK.Api.LogEvent( "Chosen to rate" );
									MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
									marketplaceReviewTask.Show();
									SettingManager.instance.SetBoolValue( "RatedApp", true );
									break;
								case CustomMessageBoxResult.RightButton:
									FlurryWP8SDK.Api.LogEvent( "Dont ever rate" );
									SettingManager.instance.SetBoolValue( "RatedApp", true );
									break;
								case CustomMessageBoxResult.None:
									FlurryWP8SDK.Api.LogEvent( "Skipped rate" );
									// Do nothing.
									break;
								default:
									break;
							}
						};
						msg.Show();
					}
				}
			}

			if( SettingManager.instance.GetBoolValue( "LocationConsent", true ) )
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
					LocationManager.instance.LockToMyPos( false );

					selected = center;
				}
				else
				{
					if( SettingManager.instance.GetBoolValue( "LocationConsent", true ) )
					{
						LocationManager.instance.StartTracking();

						LocationManager.instance.LockToMyPos( true );
						if( LocationManager.instance.MyGeoPosition != null )
							MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( LocationManager.instance.MyGeoPosition.Coordinate ), 14 );
					}
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
					SettingManager.instance.SetBoolValue( "LocationConsent", true );
				}
				else
				{
					SettingManager.instance.SetBoolValue( "LocationConsent", false );
				}
			}

			if( PhoneApplicationService.Current.State.ContainsKey( "loadedRoute" ) && PhoneApplicationService.Current.State["loadedRoute"] != null )
			{
				NavigationService.Navigate( new Uri( "/pages/DirectionsPage.xaml", UriKind.Relative ) );
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



		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			throw new NotImplementedException();
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
			NavigationService.Navigate( new Uri( "/Pages/DirectionsPage.xaml", UriKind.Relative ) );

		}

		private void ApplicationBarIconButton_NavigateTo( object sender, EventArgs e )
		{
			if( selected != null )
				NavigationService.Navigate( new Uri( "/Pages/DirectionsPage.xaml?longitude=" + selected.Longitude + "&latitude=" + selected.Latitude, UriKind.Relative ) );
		}

		private void poiList_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/POIList.xaml?longitude=" + MyMap.Center.Longitude + "&latitude=" + MyMap.Center.Latitude, UriKind.Relative ) );
		}

		private void settings_Click( object sender, System.EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/Settings.xaml", UriKind.Relative ) );
		}

		private void MyMap_Loaded( object sender, RoutedEventArgs e )
		{
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";

			MyTileSource ts;
			switch( SettingManager.instance.GetStringValue( "mapStyle", DirectionsPage.MapStyle[0] ) )
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
			if ( ts != null )
				MyMap.TileSources.Add( ts );
		}

		private void privacy_Click( object sender, System.EventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://www.cyclestreets.net/privacy/" );
			url.Show();
		}

		private void leisureRouting_Click( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/LeisureRouting.xaml", UriKind.Relative ) );
		}

		private void sendFeedback_Click( object sender, EventArgs e )
		{
			// 			EmailComposeTask task = new EmailComposeTask();
			// 			task.Subject = "CycleStreets [WP8] feedback";
			// 			task.To = "info@cyclestreets.net";
			// 			task.Show();
			NavigationService.Navigate( new Uri( "/Pages/Feedback.xaml", UriKind.Relative ) );
		}

		private void loadRoute_Click( object sender, EventArgs e )
		{
			NavigationService.Navigate( new Uri( "/Pages/LoadRoute.xaml", UriKind.Relative ) );
		}
	}
}