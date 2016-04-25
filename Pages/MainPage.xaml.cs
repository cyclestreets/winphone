using Cyclestreets.Annotations;
using Cyclestreets.CustomClasses;
using Cyclestreets.Managers;
using Cyclestreets.Objects;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Helpers;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Xml.Linq;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class MainPage
    {
        readonly Dictionary<Pushpin, POI> _pinItems = new Dictionary<Pushpin, POI>();

        private GeoCoordinate _selected;

        private GeoCoordinate Selected
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

        private MapLayer _poiLayer;

        // Declare the MarketplaceDetailTask object with page scope
        // so we can access it from event handlers.
        readonly MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
        private bool viewingPOI;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
            /*findAppBar = ApplicationBar.Buttons[ 0 ] as Microsoft.Phone.Shell.ApplicationBarIconButton;*/
            directionsAppBar = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
            navigateToAppBar = ApplicationBar.Buttons[1] as ApplicationBarIconButton;
            pointOfInterest = ApplicationBar.Buttons[3] as ApplicationBarIconButton;

            if (pointOfInterest != null)
                pointOfInterest.IconUri = new Uri("/Assets/icons/dark/appbar.location.round.png", UriKind.RelativeOrAbsolute);

            var sgs = VisualStateManager.GetVisualStateGroups(LayoutRoot);
            var sg = sgs[0] as VisualStateGroup;
            if (sg != null) ExtendedVisualStateManager.GoToElementState(LayoutRoot, ((VisualState)sg.States[0]).Name, true);
        }

        void m_Click(object sender, EventArgs e)
        {
            _marketPlaceDetailTask.Show();

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            MyMap.Center = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);

            var app = Application.Current as App;
            if (app != null && app.IsTrial)
            {
                //if( ( (App)App.Current ).trialExpired )
                //	( (App)App.Current ).showTrialExpiredMessage();


                String udid = Util.GetHardwareId();

                const string serverUrl = "http://www.rwscripts.com/cyclestreets/trial.php";

                var client = new RestClient(serverUrl);

                var request = new RestRequest("", Method.POST);
                request.AddParameter(@"Hardware", udid); // adds to POST or URL querystring based on Method

                // easily add HTTP Headers
                request.AddHeader(@"header", @"value");

                // execute the request
                //RestResponse response = client.ExecuteAsync(request, sendPushURLCallback);
                //var content = response.Content; // raw content as string

                // easy async support
                client.ExecuteAsync(request, response =>
                {
                    if (Util.ResultContainsErrors(response.Content, @"sendPushToken"))
                    {
                        Util.dataFailure();
                    }
                    else if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Util.networkFailure();
                    }
                    else
                    {
                        try
                        {
                            XDocument xml = XDocument.Parse(response.Content.Trim());
                            var session = xml.Descendants(@"root");
                            foreach (XElement s in session)
                            {
                                if (s.Element(@"trialID") != null)
                                {
                                    var xElement = s.Element(@"trialID");
                                    if (xElement != null)
                                        ((App)Application.Current).TrialID = int.Parse(xElement.Value);
                                }
                                if (s.Element(@"result") != null)
                                {
                                    var xElement = s.Element(@"result");
                                    if (xElement != null && int.Parse(xElement.Value) == 0)
                                    {
                                        NavigationService.Navigate(new Uri("/Pages/TrialExpired.xaml", UriKind.Relative));

                                        //( (App)App.Current ).showTrialExpiredMessage();
                                        //if( NavigationService.CanGoBack )
                                        //	NavigationService.GoBack();
                                    }
                                    else
                                    {
                                        if (e.NavigationMode == NavigationMode.New)
                                        {
                                            CustomMessageBox messageBox = new CustomMessageBox()
                                            {
                                                Title = AppResources.MainPage_OnNavigatedTo_Hello,
                                                Message = AppResources.TrialWelcome,
                                                RightButtonContent = AppResources.Buy_Now, // you can change this right and left button content
                                                LeftButtonContent = AppResources.Continue_Trial,
                                            };

                                            messageBox.Dismissed += (s2, e2) =>
                                            {
                                                switch (e2.Result)
                                                {
                                                    case CustomMessageBoxResult.RightButton:
                                                        _marketPlaceDetailTask.Show();
                                                        break;
                                                    case CustomMessageBoxResult.LeftButton:
                                                        break;
                                                }
                                            };

                                            messageBox.Show();
                                            ApplicationBarMenuItem m =
                                                new ApplicationBarMenuItem(AppResources.MainPage_OnNavigatedTo_buy_full_version);
                                            m.Click += m_Click;
                                            ApplicationBar.MenuItems.Add(m);
                                            SettingManager.instance.SetBoolValue(@"appIsTrial", true);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            FlurryWP8SDK.Api.LogError(@"Failed to parse login xml", ex);
                        }
                    }
                });
            }
            else
            {
                // Conversion tracking
                if (SettingManager.instance.GetBoolValue(@"appIsTrial", false))
                {
                    var tc = new TrialConversion
                    {
                        ProductId = @"FreeToPaid",
                        ProductName = @"Free Trial to Full version",
                        CurrentMarket = RegionInfo.CurrentRegion.TwoLetterISORegionName,
                        CommerceEngine = @"Custom Engine",
                        Currency = RegionInfo.CurrentRegion.ISOCurrencySymbol,
                        Price = 0.99
                    };
                    AnalyticClient.TrialConversionComplete(tc);
                    SettingManager.instance.SetBoolValue(@"appIsTrial", false);
                }

                /*if( !SettingManager.instance.GetBoolValue( "RatedApp", false ) )
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
                }*/
            }

            if (SettingManager.instance.GetBoolValue(@"LocationConsent", true))
            {
                if (_poiLayer == null)
                {
                    _poiLayer = new MapLayer();

                    MyMap.AddLayer(_poiLayer);
                }
                else
                {
                    _poiLayer.Clear();
                }
                if (NavigationContext.QueryString.ContainsKey(@"longitude"))
                {
                    GeoCoordinate center = new GeoCoordinate
                    {
                        Longitude = float.Parse(NavigationContext.QueryString[@"longitude"]),
                        Latitude = float.Parse(NavigationContext.QueryString[@"latitude"])
                    };
                    MyMap.Center = center;
                    MyMap.ZoomLevel = 16;

                    Selected = center;
                }
                else
                {
                    if (SettingManager.instance.GetBoolValue(@"LocationConsent", true))
                    {
                        LocationManager.Instance.StartTracking();

                        //FIXME
                        //if( LocationManager.Instance.MyGeoPosition != null )
                        //MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( LocationManager.Instance.MyGeoPosition.Coordinate ), 14 );
                    }
                }

                if (PoiResults.Pois != null && PoiResults.Pois.Count > 0)
                {
                    viewingPOI = true;
                    pointOfInterest.IconUri = new Uri("/Assets/icons/dark/appbar.location.round.png", UriKind.RelativeOrAbsolute);

                    _pinItems.Clear();
                    foreach (POI p in PoiResults.Pois)
                    {
                        Pushpin pp = new Pushpin();
                        _pinItems.Add(pp, p);
                        pp.Content = p.PinID;
                        pp.Tap += PoiTapped;

                        MapOverlay overlay = new MapOverlay();
                        overlay.Content = pp;
                        pp.GeoCoordinate = p.GetGeoCoordinate();
                        overlay.GeoCoordinate = p.GetGeoCoordinate();
                        overlay.PositionOrigin = new Point(0, 1.0);
                        _poiLayer.Add(overlay);
                    }

                }
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show(AppResources.LocationConsent,
                    AppResources.MainPage_OnNavigatedTo_Location,
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    SettingManager.instance.SetBoolValue(@"LocationConsent", true);
                }
                else
                {
                    SettingManager.instance.SetBoolValue(@"LocationConsent", false);
                }
            }

			if ( Util.IsWindows10() && !StorageHelper.GetSetting<bool>("BETAPrompted", false ) && StorageHelper.GetSetting<int>("LAUNCH_COUNT") > 3 )
			{
				MessageBoxResult result =
					MessageBox.Show("We are testing a Windows 10 version of this app. Would you like to try the new version and help us improve it before release?", "Join the BETA?",
					MessageBoxButton.OKCancel);

				StorageHelper.StoreSetting("BETAPrompted", true, true );

				if (result == MessageBoxResult.OK)
				{
					WebBrowserTask url = new WebBrowserTask { Uri = new Uri("https://www.microsoft.com/store/apps/9nblggh5lj1n") };
					url.Show();
				}
				else
				{
					SettingManager.instance.SetBoolValue(@"LocationConsent", false);
				}
			}

            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                NavigationService.Navigate(new Uri("/pages/DirectionsPage.xaml", UriKind.Relative));
            }
        }

        private void PoiTapped(object sender, GestureEventArgs e)
        {
            Pushpin pp = sender as Pushpin;
            if (pp == null) return;
            POI p = _pinItems[pp];
            foreach (KeyValuePair<Pushpin, POI> pair in _pinItems)
            {
                Pushpin ppItem = pair.Key;
                POI pItem = pair.Value;
                ppItem.Content = pItem.PinID;
            }
            pp.Content = p.Name;

            Selected = p.GetGeoCoordinate();

            e.Handled = true;
        }


        private void ApplicationBarIconButton_Directions(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/DirectionsPage.xaml", UriKind.Relative));

        }

        private void ApplicationBarIconButton_NavigateTo(object sender, EventArgs e)
        {
            if (Selected != null)
                NavigationService.Navigate(new Uri("/Pages/RouteOverview.xaml?mode=routeTo&longitude=" + Selected.Longitude + "&latitude=" + Selected.Latitude, UriKind.Relative));
        }

        private void poiList_Click(object sender, EventArgs e)
        {
            if (viewingPOI)
            {
                viewingPOI = false;
                _poiLayer.Clear();
                pointOfInterest.IconUri = new Uri("/Assets/icons/dark/appbar.location.round.png", UriKind.RelativeOrAbsolute);
            }
            else
            {
                NavigationService.Navigate(new Uri("/Pages/POIList.xaml?longitude=" + MyMap.Center.Longitude + "&latitude=" + MyMap.Center.Latitude, UriKind.Relative));
            }

        }

        private void settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/Settings.xaml", UriKind.Relative));
        }

        private void privacy_Click(object sender, EventArgs e)
        {
            WebBrowserTask url = new WebBrowserTask { Uri = new Uri("http://www.cyclestreets.net/privacy/") };
            url.Show();
        }

        private void leisureRouting_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LeisureRouting.xaml", UriKind.Relative));
        }

        private void sendFeedback_Click(object sender, EventArgs e)
        {
            // 			EmailComposeTask task = new EmailComposeTask();
            // 			task.Subject = "CycleStreets [WP8] feedback";
            // 			task.To = "info@cyclestreets.net";
            // 			task.Show();
            NavigationService.Navigate(new Uri("/Pages/Feedback.xaml", UriKind.Relative));
        }

        private void loadRoute_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LoadRoute.xaml", UriKind.Relative));
        }

        private void MyMap_Tap(object sender, GestureEventArgs e)
        {
            if (viewingPOI)
                return;

            CycleStreetsMap csmap = ((CycleStreetsMap)sender);
            Map map = ((CycleStreetsMap)sender).Map;
            csmap.LockToMyLocation = CycleStreetsMap.LocationLock.NoLock;

            Point p = e.GetPosition(map);
            GeoCoordinate coord = map.ConvertViewportPointToGeoCoordinate(p);

            if (_poiLayer == null)
            {
                _poiLayer = new MapLayer();

                MyMap.AddLayer(_poiLayer);
            }
            else
            {
                _poiLayer.Clear();
            }

            GeoLocationPin pp = new GeoLocationPin(coord);
            Selected = coord;

            MapOverlay overlay = new MapOverlay
            {
                Content = pp,
                GeoCoordinate = coord,
                PositionOrigin = new Point(0, 1.0)
            };

            _poiLayer.Add(overlay);
        }

        private void MyMap_MouseMove(object sender, MouseEventArgs e)
        {
            CycleStreetsMap csmap = ((CycleStreetsMap)sender);
            Map map = ((CycleStreetsMap)sender).Map;
            csmap.LockToMyLocation = CycleStreetsMap.LocationLock.NoLock;
        }

        private void MyMap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CycleStreetsMap csmap = ((CycleStreetsMap)sender);
            Map map = ((CycleStreetsMap)sender).Map;
            csmap.LockToMyLocation = CycleStreetsMap.LocationLock.NoLock;
        }

    }
}