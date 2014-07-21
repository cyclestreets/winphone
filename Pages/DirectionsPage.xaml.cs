/// Satnav mode
/// Prompt feedback

using Cyclestreets.Managers;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Util;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;

namespace Cyclestreets.Pages
{
    #region CLASSES TO BE MOVED/REMOVED
    public class ElevationPoint
    {
        public double Distance { get; set; }
        public double Height { get; set; }
    }
    public class CSResult
    {
        public string ResultName;
        public GeoCoordinate Coord;

        public override string ToString()
        {
            return ResultName;
        }
    }

    public class RouteSegment
    {
        private int _time;
        public string Time
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(_time);

                string answer = string.Format("{0:D2}:{1:D2}",
                                            t.Minutes + (t.Hours * 60),
                                            t.Seconds);
                return answer;
            }
            set
            {
                _time = int.Parse(value);
            }
        }

        public string Distance
        {
            get
            {
                float f = (float)TotalDistance * 0.000621371192f;
                return f.ToString("0.00") + AppResources.MetresShort;
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
        public string BgColour
        {
            get;
            set;
        }

        public GeoCoordinate Location
        {
            get;
            set;
        }
        public double Bearing
        {
            get;
            set;
        }

        public double TotalDistance { get; set; }
    }
    public class RouteSegmentCollection : List<RouteSegment> { }

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

        ObservableCollection<ElevationPoint> _heightChart = new ObservableCollection<ElevationPoint>();
        public ObservableCollection<ElevationPoint> HeightChart { get { return _heightChart; } set { _heightChart = value; } }

        public int distance
        {
            get;
            set;
        }
    }

    public class JourneyFactItem
    {
        public JourneyFactItem(string image)
        {
            BitmapImage src = new BitmapImage { UriSource = new Uri(image, UriKind.Relative) };
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

    public class ListBoxPair
    {
        public string DisplayName { get; set; }
        public string Value { get; set; }

        public ListBoxPair(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
    #endregion

    public partial class DirectionsPage
    {

        readonly GeocodeQuery _geoQ = null;

        Stackish<Pushpin> _waypoints = new Stackish<Pushpin>();

        readonly Dictionary<Pushpin, POI> _pinItems = new Dictionary<Pushpin, POI>();
        private MapLayer _poiLayer;

        readonly List<List<GeoCoordinate>> _geometryCoords = new List<List<GeoCoordinate>>();
        readonly List<Color> _geometryColor = new List<Color>();
        readonly List<bool> _geometryDashed = new List<bool>();

        public static List<JourneyFactItem> Facts = new List<JourneyFactItem>();

        private GeoCoordinate _max = new GeoCoordinate(90, -180);
        private GeoCoordinate _min = new GeoCoordinate(-90, 180);

        private bool _hideRouteOptions = false;

        GeoCoordinate _current = null;

        private int _currentStep = -1;

        public static String[] MapStyle = { "OpenStreetMap", "OpenCycleMap", "Nokia" };
        public static ListBoxPair[] RouteType = 
		{ 
			new ListBoxPair(AppResources.BalancedRoute, "balanced"),
			new ListBoxPair(AppResources.FastestRoute, "fastest"), 
			new ListBoxPair(AppResources.QuietestRoute, "quietest") 
		};
        public static String[] CycleSpeed = { "10mph", "12mph", "15mph" };
        public static String[] EnabledDisabled = { AppResources.Enabled, AppResources.Disabled };
        private string currentRouteData;

        private static WebClient placeSearch;
        private AsyncWebRequest _request;		// The request for directions

        public DirectionsPage()
        {
            InitializeComponent();

            //progress.DataContext = App.networkStatus;

            // hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
            saveRoute = ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
            saveRoute.Text = AppResources.SaveRoute;
            loadRoute = ApplicationBar.MenuItems[1] as ApplicationBarMenuItem;
            loadRoute.Text = AppResources.LoadRoute;
            settings = ApplicationBar.MenuItems[2] as ApplicationBarMenuItem;
            settings.Text = AppResources.Settings;
            privacy = ApplicationBar.MenuItems[3] as ApplicationBarMenuItem;
            privacy.Text = AppResources.PrivacyPolicy;
            sendFeedback = ApplicationBar.MenuItems[4] as ApplicationBarMenuItem;
            sendFeedback.Text = AppResources.FindRoute;

            saveRoute.IsEnabled = false;

            // Setup route type dropdown
            string plan = SettingManager.instance.GetStringValue("defaultRouteType", "balanced");
            routeTypePicker.ItemsSource = RouteType;
            routeTypePicker.DisplayMemberPath = "DisplayName";
            int idx = Array.FindIndex(RouteType, v => v.Value.Equals(plan));
            routeTypePicker.SelectedIndex = idx == -1 ? 0 : idx;

            currentPlan = plan.Replace(" route", "");

            _geoQ = new GeocodeQuery();
            _geoQ.QueryCompleted += geoQ_QueryCompleted;

            clearCurrentPosition();

            pleaseWait.Width = Application.Current.Host.Content.ActualWidth;

            startPoint.Populating += StartPointOnPopulating;

            ExtendedVisualStateManager.GoToElementState(LayoutRoot, @"RoutePlanner", false);
        }

        private async void StartPointOnPopulating(object sender, PopulatingEventArgs populatingEventArgs)
        {
            List<string> results = await GeoUtils.StartPlaceSearch(populatingEventArgs.Parameter, MyMap.Center);
            if (results == null) return;
            AutoCompleteBox acb = sender as AutoCompleteBox;
            if (acb == null) return;
            acb.ItemsSource = null;
            acb.ItemsSource = results;
            acb.PopulateComplete();
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                PhoneApplicationService.Current.State.Remove(@"loadedRoute");
            }

            LocationManager.instance.trackingGeolocator.PositionChanged -= positionChangedHandler;

            //LocationManager.instance.StopTracking();
            //LocationManager.instance.StartTracking();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LocationManager.instance.trackingGeolocator.PositionChanged += positionChangedHandler;

            if (NavigationContext.QueryString.ContainsKey(@"plan"))
            {
                _hideRouteOptions = (NavigationContext.QueryString[@"plan"].Equals(@"leisure"));
            }

            SetupTutorial();

            routeTypePicker.Visibility = _hideRouteOptions ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetupTutorial()
        {
            // Tutorial!? We dont need no stinkin tutorial!
            if (!SettingManager.instance.GetBoolValue(@"tutorialEnabled", true))
            {
                routeTutorial1.Visibility = Visibility.Collapsed;
                routeTutorial2.Visibility = Visibility.Collapsed;
                routeTutorial3.Visibility = Visibility.Collapsed;
                routeTutorial4.Visibility = Visibility.Collapsed;
                routeTutorial5.Visibility = Visibility.Collapsed;
            }
            routeTutorial1.Visibility = Visibility.Collapsed;

            bool shownTutorial = SettingManager.instance.GetBoolValue(@"shownTutorial", false);
            if (shownTutorial) return;
            bool shownTutorialQuestion = SettingManager.instance.GetBoolValue(@"shownTutorialQuestion", false);

            if (!shownTutorialQuestion)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.ShowTutorialMsg, AppResources.Tutorial,
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                {
                    SettingManager.instance.SetBoolValue(@"shownTutorial", true);
                    SettingManager.instance.SetBoolValue("tutorialEnabled", false);
                }
                else
                {
                    routeTutorial1.Visibility = Visibility.Visible;
                    SettingManager.instance.SetBoolValue("tutorialEnabled", true);
                }

                SettingManager.instance.SetBoolValue("shownTutorialQuestion", true);
            }
            else
            {
                if (SettingManager.instance.GetBoolValue("tutorialEnabled", true))
                    routeTutorial1.Visibility = Visibility.Visible;
            }
        }

        /*private void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            AutoCompleteBox acb = e.UserState as AutoCompleteBox;
            if (acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty(e.Result))
            {
                System.Diagnostics.Debug.WriteLine(e.Result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(e.Result);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + e.Result + " " + ex.Message);
                    MessageBox.Show("Could not parse location data information from server. Please let us know about this error with what you were typing in the search box to cause this problem.");
                }
                JArray suggestions = (JArray)o["suggestions"];
                List<string> names = new List<string>();
                if (suggestions.Count > 0)
                {
                    foreach (string s in suggestions)
                    {
                        names.Add(s);
                    }

                    if (names.Count > 0)
                    {
                        acb.ItemsSource = null;
                        acb.ItemsSource = names;
                        acb.PopulateComplete();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(@"Starting backup search for " + startPoint.Text);
                    placeSearch = new WebClient();
                    string prefix = HttpUtility.UrlEncode(startPoint.Text);

                    string myLocation = "";
                    LocationRectangle rect = GetMapBounds();
                    myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
                    //myLocation = HttpUtility.UrlEncode( myLocation );

                    Uri service = new Uri("http://cambridge.cyclestreets.net/api/geocoder.json?key=" + App.apiKey + myLocation + "&street=" + prefix);

                    placeSearch.DownloadStringCompleted += CSDownloadStringCompleted;
                    placeSearch.DownloadStringAsync(service, e.UserState);
                }
                App.networkStatus.networkIsBusy = false;
            }
        }

        private void CSDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            placeSearch = null;
            AutoCompleteBox acb = e.UserState as AutoCompleteBox;
            if (acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty(e.Result))
            {
                System.Diagnostics.Debug.WriteLine(e.Result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(e.Result);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + e.Result + " " + ex.Message);
                    MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                }
                JArray suggestions = null;

                if (o["results"] != null && o["results"]["result"] != null)
                {
                    if (o["results"]["result"] is JArray)
                        suggestions = (JArray)o["results"]["result"];
                    else
                    {
                        suggestions = new JArray();
                        suggestions.Add((JObject)o["results"]["result"]);
                    }
                    if (suggestions.Count > 0)
                    {
                        List<CSResult> names = new List<CSResult>();
                        foreach (JObject s in suggestions)
                        {
                            CSResult res = new CSResult();
                            res.Coord = new GeoCoordinate(double.Parse(s["latitude"].ToString()), double.Parse(s["longitude"].ToString()));
                            res.ResultName = s["name"].ToString();
                            if (!string.IsNullOrWhiteSpace(s["near"].ToString()))
                                res.ResultName += ", " + s["near"].ToString();
                            names.Add(res);
                        }
                        acb.ItemsSource = null;
                        acb.ItemsSource = names;
                    }
                }

                else
                {
                    List<string> names = new List<string>();
                    names.Add(AppResources.NoSuggestions);

                    acb.ItemsSource = null;
                    acb.ItemsSource = names;
                }
                acb.PopulateComplete();
            }
        }*/

        private LocationRectangle GetMapBounds()
        {
            if (MyMap == null)
                return new LocationRectangle();

            GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate(new Point(MyMap.ActualWidth, MyMap.ActualHeight));

            return LocationRectangle.CreateBoundingRectangle(new[] { topLeft, bottomRight });
        }

        private void revGeoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result == null || e.Result.Count <= 0)
                return;
            MapLocation loc = e.Result[0];
            startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
        }

        private void startPoint_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (startPoint.SelectedItem != null)
            {
                if (startPoint.SelectedItem is CSResult)
                {
                    CSResult res = startPoint.SelectedItem as CSResult;
                    setCurrentPosition(res.Coord);

                    SmartDispatcher.BeginInvoke(() =>
                    {
                        MyMap.SetView(res.Coord, 16);
                        this.Focus();
                        //MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
                    });
                    App.networkStatus.networkIsBusy = false;
                }
                else
                {
                    string start = startPoint.SelectedItem as string;

                    if (!_geoQ.IsBusy && !string.IsNullOrWhiteSpace(start) && start != AppResources.NoSuggestions)
                    {
                        _geoQ.SearchTerm = start;
                        _geoQ.GeoCoordinate = MyMap.Center;
                        _geoQ.QueryAsync();
                        App.networkStatus.networkIsBusy = true;

                    }
                }
            }
        }

        private void geoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result.Count > 0)
            {
                GeoCoordinate g = e.Result[0].GeoCoordinate;
                setCurrentPosition(g);
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
                myLocation = HttpUtility.UrlEncode(myLocation);

#if DEBUG
                Uri service = new Uri("http://demo.places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson");
#else
				Uri service = new Uri("http://places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson");
#endif

                wc.DownloadStringCompleted += DiscoverStringCompleted;
                wc.DownloadStringAsync(service);
                //
            }
        }

        private void DiscoverStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled && !string.IsNullOrEmpty(e.Result))
            {
                System.Diagnostics.Debug.WriteLine(e.Result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(e.Result);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + e.Result + " " + ex.Message);
                    MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                }
                JArray suggestions = (JArray)o["results"]["items"];
                if (suggestions.Count > 0)
                {
                    JArray pos = (JArray)suggestions[0]["position"];
                    GeoCoordinate g = new GeoCoordinate((double)pos[0], (double)pos[1]);
                    setCurrentPosition(g);

                    App.networkStatus.networkIsBusy = false;
                }
            }
            App.networkStatus.networkIsBusy = false;
            this.Focus();
        }

        private async void cursorPos_Click(object sender, EventArgs e)
        {
            MapLocation loc = await GeoUtils.StartReverseGeocode(MyMap.Center);
            setCurrentPosition(loc);



            confirmWaypoint_Click(sender, e);
        }

        private void confirmWaypoint_Click(object sender, EventArgs e)
        {
            if (currentRouteData != null)
                return;

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            rm.AddWaypoint(_current);

            if (wayPointLayer == null)
            {
                wayPointLayer = new MapLayer();

                MyMap.Layers.Add(wayPointLayer);
            }

            // Change last push pin from finish to intermediate
            if (_waypoints.Count > 1)
            {
                Pushpin last = _waypoints.Peek();
                last.Style = Resources["Intermediate"] as Style;
                last.Content = "" + (_waypoints.Count - 1);
            }

            Pushpin pp = new Pushpin();
            if (_waypoints.Count == 0)
                pp.Style = Resources["Start"] as Style;
            else
                pp.Style = Resources["Finish"] as Style;

            pp.BorderBrush = new SolidColorBrush(Colors.Red);
            pp.BorderThickness = new Thickness(200);
            pp.Tap += pinTapped;

            MapOverlay overlay = new MapOverlay();
            overlay.Content = pp;
            pp.GeoCoordinate = _current;
            overlay.GeoCoordinate = _current;
            overlay.PositionOrigin = new Point(0.3, 1.0);
            wayPointLayer.Add(overlay);

            addWaypoint(pp);

            if (SettingManager.instance.GetBoolValue("tutorialEnabled", true))
            {
                bool shownTutorial = SettingManager.instance.GetBoolValue("shownTutorialPin", false);
                if (!shownTutorial)
                    routeTutorialPin.Visibility = Visibility.Visible;
            }
            // Clear box
            startPoint.Text = "";

            clearCurrentPosition();
        }

        private void pinTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Pushpin pp = sender as Pushpin;
            if (pp == null)
                return;

            removeWaypoint(pp);

            wayPointLayer.Clear();

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            rm.RemoveWaypoint(pp.GeoCoordinate);

            for (int i = 0; i < _waypoints.Count; i++)
            {
                MapOverlay overlay = new MapOverlay
                {
                    Content = _waypoints[i],
                    GeoCoordinate = _waypoints[i].GeoCoordinate,
                    PositionOrigin = new Point(0.3, 1.0)
                };
                wayPointLayer.Add(overlay);

                // Set pin styles
                pp = _waypoints[i];
                if (i == 0)
                    pp.Style = Resources["Start"] as Style;
                else if (i == _waypoints.Count - 1)
                    pp.Style = Resources["Finish"] as Style;
                else
                {
                    pp.Style = Resources["Intermediate"] as Style;
                    pp.Content = "" + i;
                }
            }
        }

        private void addWaypoint(Pushpin pp)
        {
            _waypoints.Push(pp);
        }

        private void removeWaypoint(Pushpin pp)
        {
            _waypoints.Remove(pp);
        }

        private void clearCurrentPosition()
        {
            _current = null;
            //confirmWaypoint.IsEnabled = false;
        }

        private void setCurrentPosition(MapLocation loc)
        {
            if (loc == null)
            {
                MessageBox.Show("Invalid Location",
                    "Could not find location. Please try again", MessageBoxButton.OK);
                return;
            }
            setCurrentPosition(loc.GeoCoordinate);

            SmartDispatcher.BeginInvoke(() =>
            {
                // Set the name in the box without looking it up
                startPoint.Populating -= StartPointOnPopulating;
                startPoint.Text = loc.Information.Name + " " + loc.Information.Address.Street + ", " + loc.Information.Address.City + ", " + loc.Information.Address.PostalCode;
                startPoint.Populating += StartPointOnPopulating;
            });
        }

        private void setCurrentPosition(GeoCoordinate c)
        {
            _current = c;
            SmartDispatcher.BeginInvoke(() =>
            {
                MyMap.SetView(_current, 16);
            });
        }

        /*private void RouteFound(byte[] data)
        {
            if (data == null)
            {
                return;
            }

            UTF8Encoding enc = new UTF8Encoding();

            RouteFound(enc.GetString(data, 0, data.Length));
        }

        private void RouteFound(string data)
        {
            try
            {


                currentRouteData = data;

                if (pleaseWait.IsOpen)
                    pleaseWait.IsOpen = false;

                //confirmWaypoint.IsEnabled = false;
                //cursorPos.IsEnabled = false;
                SetPlanRouteAvailability(false);

                JObject o = null;
                if (currentRouteData != null)
                {
                    try
                    {
                        o = JObject.Parse(currentRouteData.Trim());
                    }
                    catch (Exception ex)
                    {
                        MarkedUp.AnalyticClient.Error("Could not parse JSON " + currentRouteData.Trim() + " " + ex.Message);
                        MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                    }
                }

                if (o == null || o["marker"] == null)
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.NoRouteFoundTryAnotherSearch, AppResources.NoRoute, MessageBoxButton.OK);
                    NavigationService.GoBack();
                    return;
                }
                _geometryCoords.Clear();
                Facts.Clear();
                MyMap.MapElements.Clear();

                _max = new GeoCoordinate(90, -180);
                _min = new GeoCoordinate(-90, 180);

                _currentStep = -1;

                arrowLeft.Opacity = 50;
                arrowRight.Opacity = 100;

                route = new RouteDetails();

                JArray steps = o["marker"] as JArray;
                JArray pois = o["poi"] as JArray;
                const string col1 = "#7F000000";
                const string col2 = "#3F000000";
                bool swap = true;
                int totalTime = 0;
                double totalDistanceMetres = 0;
                int totalDistance = 0;
                foreach (var jToken in steps)
                {
                    var step = (JObject)jToken;
                    JObject p = (JObject)step["@attributes"];
                    string markerType = (string)p["type"];
                    switch (markerType)
                    {
                        case "route":
                            {
                                route.routeIndex = JsonGetPropertyHelper<int>(p, "itinerary");
                                JourneyFactItem i = new JourneyFactItem("Assets/picture.png")
                                {
                                    Caption = AppResources.RouteNumber,
                                    Value = "" + route.routeIndex
                                };
                                Facts.Add(i);

                                route.timeInSeconds = JsonGetPropertyHelper<int>(p, "time");
                                i = new JourneyFactItem("Assets/clock.png")
                                {
                                    Caption = AppResources.JourneyTime,
                                    Value = UtilTime.secsToLongDHMS(route.timeInSeconds)
                                };
                                Facts.Add(i);

                                route.quietness = JsonGetPropertyHelper<float>(p, "quietness");
                                i = new JourneyFactItem("Assets/picture.png")
                                {
                                    Caption = AppResources.Quietness,
                                    Value = route.quietness + "% " + getQuietnessString(route.quietness)
                                };
                                Facts.Add(i);

                                route.signalledJunctions = JsonGetPropertyHelper<int>(p, "signalledJunctions");
                                i = new JourneyFactItem("Assets/traffic_signals.png")
                                {
                                    Caption = AppResources.SignaledJunctions,
                                    Value = "" + route.signalledJunctions
                                };
                                Facts.Add(i);

                                route.signalledCrossings = JsonGetPropertyHelper<int>(p, "signalledCrossings");
                                i = new JourneyFactItem("Assets/traffic_signals.png")
                                {
                                    Caption = AppResources.SignaledCrossings,
                                    Value = "" + route.signalledCrossings
                                };
                                Facts.Add(i);

                                route.grammesCO2saved = JsonGetPropertyHelper<int>(p, "grammesCO2saved");
                                i = new JourneyFactItem("Assets/world.png")
                                {
                                    Caption = AppResources.CO2Avoided,
                                    Value = (float)route.grammesCO2saved / 1000f + " kg"
                                };
                                Facts.Add(i);

                                route.calories = JsonGetPropertyHelper<int>(p, "calories");
                                i = new JourneyFactItem("Assets/heart.png")
                                {
                                    Caption = AppResources.Calories,
                                    Value = route.calories + AppResources.Kcal
                                };
                                Facts.Add(i);
                                break;
                            }
                        case "segment":
                            {
                                string pointsText = (string)p["points"];
                                string[] points = pointsText.Split(' ');
                                List<GeoCoordinate> coords = new List<GeoCoordinate>();
                                foreach (string t in points)
                                {
                                    string[] xy = t.Split(',');

                                    double longitude = double.Parse(xy[0]);
                                    double latitude = double.Parse(xy[1]);
                                    coords.Add(new GeoCoordinate(latitude, longitude));

                                    if (_max.Latitude > latitude)
                                        _max.Latitude = latitude;
                                    if (_min.Latitude < latitude)
                                        _min.Latitude = latitude;
                                    if (_max.Longitude < longitude)
                                        _max.Longitude = longitude;
                                    if (_min.Longitude > longitude)
                                        _min.Longitude = longitude;
                                }
                                _geometryCoords.Add(coords);

                                string elevationsText = (string)p["elevations"];
                                string[] elevations = elevationsText.Split(',');

                                RouteSegment s = new RouteSegment
                                {
                                    Location = coords[0],
                                    Bearing =
                                        Geodesy.Bearing(coords[0].Latitude, coords[0].Longitude,
                                            coords[coords.Count - 1].Latitude, coords[coords.Count - 1].Longitude)
                                };
                                route.distance += (int)float.Parse((string)p["distance"]);
                                s.DistanceMetres = (int)float.Parse((string)p["distance"]);
                                totalDistanceMetres += s.DistanceMetres;
                                totalDistance += route.distance;
                                s.TotalDistance = totalDistance;
                                s.Name = (string)p["name"];
                                s.ProvisionName = (string)p["provisionName"];
                                int theLegOfTime = int.Parse((string)p["time"]);
                                s.Time = "" + (totalTime + theLegOfTime);
                                totalTime += theLegOfTime;
                                s.Turn = (string)p["turn"];
                                s.Walk = (int.Parse((string)p["walk"]) == 1 ? true : false);
                                if (elevations.Length >= 1)
                                {
                                    ElevationPoint dp = new ElevationPoint { Distance = totalDistanceMetres, Height = float.Parse(elevations[0]) };
                                    route.HeightChart.Add(dp);
                                }
                                _geometryDashed.Add(s.Walk);
                                _geometryColor.Add(s.Walk ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 127, 0, 255));
                                s.BgColour = swap ? col1 : col2;
                                swap = !swap;
                                route.segments.Add(s);
                                break;
                            }
                    }
                }
                if (_poiLayer == null)
                {
                    _poiLayer = new MapLayer();

                    MyMap.Layers.Add(_poiLayer);
                }
                else
                {
                    _poiLayer.Clear();
                }
                int id = 0;
                if (pois != null)
                {
                    foreach (var jToken in pois)
                    {
                        var poi = (JObject)jToken;
                        JObject p = (JObject)poi["@attributes"];
                        POI poiItem = new POI { Name = (string)p["name"] };
                        GeoCoordinate g = new GeoCoordinate
                        {
                            Longitude = float.Parse((string)p["longitude"]),
                            Latitude = float.Parse((string)p["latitude"])
                        };
                        poiItem.Position = g;

                        poiItem.PinID = "" + (id++);

                        Pushpin pp = new Pushpin { Content = poiItem.PinID };
                        pp.Tap += poiTapped;

                        _pinItems.Add(pp, poiItem);

                        MapOverlay overlay = new MapOverlay { Content = pp };
                        pp.GeoCoordinate = poiItem.GetGeoCoordinate();
                        overlay.GeoCoordinate = poiItem.GetGeoCoordinate();
                        overlay.PositionOrigin = new Point(0, 1.0);
                        _poiLayer.Add(overlay);
                    }
                }
                JourneyFactItem item = new JourneyFactItem("Assets/bullet_go.png") { Caption = AppResources.Distance };
                float dist = route.distance * 0.000621371192f;
                item.Value = dist.ToString("0.00") + AppResources.Miles;
                Facts.Add(item);

                SmartDispatcher.BeginInvoke(() =>
                {
                    if (_geometryCoords.Count == 0)
                    {
                        MessageBox.Show(AppResources.CouldNotCalculateRoute);
                        App.networkStatus.networkIsBusy = false;
                        return;
                    }

                    try
                    {
                        LocationRectangle rect = new LocationRectangle(_min, _max);
                        MyMap.SetView(rect);
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine(@"Invalid box");
                    }

                    //MyMap.Center = new GeoCoordinate(min.Latitude + ((max.Latitude - min.Latitude) / 2f), min.Longitude + ((max.Longitude - min.Longitude) / 2f));
                    //MyMap.ZoomLevel = 10;
                    int count = _geometryCoords.Count;
                    for (int i = 0; i < count; i++)
                    {
                        List<GeoCoordinate> coords = _geometryCoords[i];
                        DrawMapMarker(coords.ToArray(), _geometryColor[i], _geometryDashed[i]);
                    }

                    //NavigationService.Navigate( new Uri( "/Pages/DirectionsResults.xaml", UriKind.Relative ) );
                    var sgs = ExtendedVisualStateManager.GetVisualStateGroups(LayoutRoot);
                    var sg = sgs[0] as VisualStateGroup;
                    bool res = ExtendedVisualStateManager.GoToElementState(LayoutRoot, "RouteFoundState", true);
                    //VisualStateManager.GoToState(this, "RouteFoundState", true);

                    App.networkStatus.networkIsBusy = false;

                    float f = (float)route.distance * 0.000621371192f;
                    findLabel1.Text = f.ToString("0.00") + AppResources.MetresShort + UtilTime.secsToLongDHMS(route.timeInSeconds);

                    if (!SettingManager.instance.GetBoolValue("tutorialEnabled", true)) return;
                    bool shownTutorial = SettingManager.instance.GetBoolValue("shownTutorialRouteType", false);
                    if (!shownTutorial)
                        routeTutorialRouteType.Visibility = Visibility.Visible;
                });

                saveRoute.IsEnabled = true;
            }
            catch (Exception e)
            {
                MarkedUp.AnalyticClient.Error("Could not parse direction results " + e.Message);
                MessageBox.Show(
                    "There was a problem reading the direction data. Please pass this message on to info@cyclestreets.net. " +
                    e.Message);
            }
        }*/

        private static T JsonGetPropertyHelper<T>(JObject p, string key)
        {

            if (p[key] == null)
                throw new JsonException("Missing key " + key + " from result");

            try
            {
                return p.Value<T>(key);
            }
            catch (Exception)
            {
                throw new JsonException(string.Format("Unexpected value for {0} {1}", key, p[key]));
            }

        }

        private void poiTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Pushpin pp = sender as Pushpin;
            POI p = _pinItems[pp];
            foreach (KeyValuePair<Pushpin, POI> pair in _pinItems)
            {
                Pushpin ppItem = pair.Key;
                POI pItem = pair.Value;
                ppItem.Content = pItem.PinID;
            }
            pp.Content = p.Name;

            //selected = p.GetGeoCoordinate();
        }

        private string getQuietnessString(float p)
        {
            if (p > 80)
                return AppResources.Quiet;
            else if (p > 60)
                return AppResources.QuiteQuiet;
            else if (p > 40)
                return AppResources.QuiteBusy;
            else if (p > 20)
                return AppResources.Busy;
            else
                return AppResources.VeryBusy;
        }

        private void DrawMapMarker(GeoCoordinate[] coordinate, Color color, bool dashed)
        {
            // Create a map marker
            MapPolyline polygon = new MapPolyline();
            polygon.StrokeColor = color;
            polygon.StrokeThickness = 3;
            polygon.StrokeDashed = dashed;
            polygon.Path = new GeoCoordinateCollection();
            for (int i = 0; i < coordinate.Length; i++)
            {
                //Point p = MyMap.ConvertGeoCoordinateToViewportPoint( coordinate[i] );
                polygon.Path.Add(coordinate[i]);
            }

            MyMap.MapElements.Add(polygon);
        }

        private Color ConvertHexStringToColour(string hexString)
        {
            //byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            if (hexString.StartsWith("#"))
            {
                hexString = hexString.Substring(1, 6);
            }
            //a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
            //	System.Globalization.NumberStyles.AllowHexSpecifier));
            r = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            g = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2),
                System.Globalization.NumberStyles.AllowHexSpecifier));
            b = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            return Color.FromArgb(255, r, g, b);
        }

        private async void routeTypePicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ListPicker picker = sender as ListPicker;
            if (picker == null) return;
            currentPlan = ((ListBoxPair)picker.SelectedItem).Value;

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (!rm.ReadyToPlanRoute) return;

            bool result = await rm.FindRoute(currentPlan, false);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error("Route Planning Error");

                MessageBox.Show(
                    "Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
            }
            else
            {
                MyMap.MapElements.Clear();
                PlotCachedRoute();
            }
        }

        private void routeInfo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //PhoneApplicationService.Current.State["route"] = currentRouteData;
            //PhoneApplicationService.Current.State["routeIndex"] = route.routeIndex;
            NavigationService.Navigate(new Uri("/Pages/DirectionsResults.xaml", UriKind.Relative));
        }

        private void arrowLeft_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            rm.CurrentStep--;
            if (rm.CurrentStep <= 0)
            {
                MyMap.SetView(rm.GetRouteBounds());
                MyMap.Pitch = 0;
                MyMap.Heading = 0;

                SetMapStyle();
            }
            else
            {
                MyMap.SetView(rm.CurrentGeoCoordinate, 20, rm.CurrentBearing, 75);

                MyMap.TileSources.Clear();
            }

        }

        private void arrowRight_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            rm.CurrentStep++;
            
            MyMap.SetView(rm.CurrentGeoCoordinate, 20, rm.CurrentBearing, 75);

            MyMap.TileSources.Clear();
        }

        private void routeTutorial1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorial2.Visibility = Visibility.Visible;
            routeTutorial1.Visibility = Visibility.Collapsed;
        }

        private void routeTutorial2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorial3.Visibility = Visibility.Visible;
            routeTutorial2.Visibility = Visibility.Collapsed;
        }

        private void routeTutorial3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorial4.Visibility = Visibility.Visible;
            routeTutorial3.Visibility = Visibility.Collapsed;
        }

        private void routeTutorial4_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorial5.Visibility = Visibility.Visible;
            routeTutorial4.Visibility = Visibility.Collapsed;
        }

        private void routeTutorial5_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorial5.Visibility = Visibility.Collapsed;
            SettingManager.instance.SetBoolValue("shownTutorial", true);
        }

        private void routeTutorialPin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialPin.Visibility = Visibility.Collapsed;
            SettingManager.instance.SetBoolValue("shownTutorialPin", true);
        }

        private void routeTutorialRouteType_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialRouteInfo.Visibility = Visibility.Visible;
            routeTutorialRouteType.Visibility = Visibility.Collapsed;
        }

        private void routeTutorialRouteInfo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialRouteInfo.Visibility = Visibility.Collapsed;
            SettingManager.instance.SetBoolValue("shownTutorialRouteType", true);
        }

        private void settings_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/Settings.xaml", UriKind.Relative));
        }

        private void privacy_Click(object sender, System.EventArgs e)
        {
            WebBrowserTask url = new WebBrowserTask();
            url.Uri = new System.Uri("http://www.cyclestreets.net/privacy/");
            url.Show();
        }

        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";

            if (PhoneApplicationService.Current.State.ContainsKey("loadedRoute") && PhoneApplicationService.Current.State["loadedRoute"] != null)
            {
                string routeData = (string)PhoneApplicationService.Current.State["loadedRoute"];
                _hideRouteOptions = true;
                RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
                rm.ParseRouteData(routeData, currentPlan, false);
                PhoneApplicationService.Current.State["loadedRoute"] = null;
            }

            routeTypePicker.Visibility = _hideRouteOptions ? Visibility.Collapsed : Visibility.Visible;

            positionChangedHandler(null, null);

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
                    ts = new OCMTileSource();
                    break;
                default:
                    ts = null;
                    break;
            }
            MyMap.TileSources.Clear();
            if (ts != null)
                MyMap.TileSources.Add(ts);
        }

        private void startPoint_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AutoCompleteBox box = sender as AutoCompleteBox;
                string text = box.Text;

                if (string.IsNullOrWhiteSpace(text))
                    return;

                if (text.Length == 6 || text.Length == 7)
                {
                    if (char.IsLetter(text[0]) && char.IsLetter(text[text.Length - 1]) && char.IsLetter(text[text.Length - 2])
                         && char.IsNumber(text[text.Length - 3]) && char.IsNumber(text[text.Length - 4]))
                    {
                        // This is a postcode
                        string newPostcodeEnd = text.Substring(text.Length - 3, 3);
                        string newPostcodeStart = text.Substring(0, text.Length - 3);
                        box.Text = newPostcodeStart + " " + newPostcodeEnd;
                    }
                }
                //StartPointOnPopulating(box.Text, box);

                if (!_geoQ.IsBusy && !string.IsNullOrWhiteSpace(box.Text) && box.Text != AppResources.NoSuggestions)
                {
                    _geoQ.SearchTerm = box.Text;
                    _geoQ.GeoCoordinate = MyMap.Center;
                    _geoQ.QueryAsync();
                    App.networkStatus.networkIsBusy = true;
                }
                this.Focus();
            }
        }

        private void saveRoute_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State["route"] = currentRouteData;
            NavigationService.Navigate(new Uri("/Pages/SaveRoute.xaml", UriKind.Relative));
        }

        private void loadRoute_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LoadRoute.xaml", UriKind.Relative));
        }

        private void startPoint_Populated(object sender, PopulatedEventArgs e)
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
        private MapOverlay myLocationOverlay2 = null;
        private Ellipse accuracyEllipse = null;
        private bool planRouteAvailable;
        private MapLayer wayPointLayer;
        private string currentPlan;
        private void positionChangedHandler(Geolocator sender, PositionChangedEventArgs args)
        {
            SmartDispatcher.BeginInvoke(() =>
                {
                    if (LocationManager.instance.MyGeoPosition != null)
                    {
                        double myAccuracy = LocationManager.instance.MyGeoPosition.Coordinate.Accuracy;
                        GeoCoordinate myCoordinate = CoordinateConverter.ConvertGeocoordinate(LocationManager.instance.MyGeoPosition.Coordinate);
                        if (myLocationOverlay == null)
                        {
                            Ellipse myCircle = new Ellipse();
                            myCircle.Fill = new SolidColorBrush(Colors.Black);
                            myCircle.Height = 20;
                            myCircle.Width = 20;
                            myCircle.Opacity = 30;
                            Binding myBinding = new Binding("Visible");
                            myBinding.Source = new MyPositionDataSource(MyMap);
                            myCircle.Visibility = Visibility.Visible;
                            myCircle.SetBinding(Ellipse.VisibilityProperty, myBinding);

                            MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;

                            accuracyEllipse = new Ellipse();
                            accuracyEllipse.Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0));
                            accuracyEllipse.Visibility = Visibility.Visible;
                            accuracyEllipse.SetBinding(Ellipse.VisibilityProperty, myBinding);

                            // Create a MapOverlay to contain the circle.
                            myLocationOverlay = new MapOverlay();
                            myLocationOverlay.Content = myCircle;
                            myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
                            myLocationOverlay.GeoCoordinate = myCoordinate;

                            myLocationOverlay2 = new MapOverlay();
                            myLocationOverlay2.Content = accuracyEllipse;
                            myLocationOverlay2.PositionOrigin = new Point(0.5, 0.5);
                            myLocationOverlay2.GeoCoordinate = myCoordinate;

                            // Create a MapLayer to contain the MapOverlay.
                            MapLayer myLocationLayer = new MapLayer();
                            myLocationLayer.Add(myLocationOverlay);
                            myLocationLayer.Add(myLocationOverlay2);

                            MyMap.Layers.Add(myLocationLayer);
                        }

                        myLocationOverlay.GeoCoordinate = myCoordinate;
                        myLocationOverlay2.GeoCoordinate = myCoordinate;

                        double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
                        double radius = myAccuracy / metersPerPixels;
                        accuracyEllipse.Width = radius * 2;
                        accuracyEllipse.Height = radius * 2;
                    }
                });
        }

        private void MyMap_ZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            double myAccuracy = LocationManager.instance.MyGeoPosition.Coordinate.Accuracy;
            GeoCoordinate myCoordinate = CoordinateConverter.ConvertGeocoordinate(LocationManager.instance.MyGeoPosition.Coordinate);
            double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = myAccuracy / metersPerPixels;
            accuracyEllipse.Width = radius * 2;
            accuracyEllipse.Height = radius * 2;
        }

        private void sendFeedback_Click(object sender, EventArgs e)
        {
            // 			EmailComposeTask task = new EmailComposeTask();
            // 			task.Subject = "CycleStreets [WP8] feedback";
            // 			task.To = "info@cyclestreets.net";
            // 			task.Show();
            NavigationService.Navigate(new Uri("/Pages/Feedback.xaml", UriKind.Relative));
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            pleaseWait.IsOpen = false;
            App.networkStatus.networkIsBusy = false;
            if (_request != null)
            {
                _request.Stop();
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            /*if (pleaseWait.IsOpen)
            {
                pleaseWait.IsOpen = false;
                e.Cancel = true;
            }
            else if (currentStep >= 0)
            {
                currentStep = -1;

                LocationRectangle rect = new LocationRectangle(min, max);
                MyMap.SetView(rect);
                MyMap.Pitch = 0;
                MyMap.Heading = 0;

                float f = (float)route.distance * 0.000621371192f;
                findLabel1.Text = f.ToString("0.00") + AppResources.MetresShort + UtilTime.secsToLongDHMS(route.timeInSeconds);

                SetMapStyle();
                e.Cancel = true;
            }
            else if (currentRouteData != null)
            {
                currentRouteData = null;

                geometryCoords.Clear();
                facts.Clear();
                route.segments.Clear();
                MyMap.MapElements.Clear();
                waypoints.Clear();

                max = new GeoCoordinate(90, -180);
                min = new GeoCoordinate(-90, 180);

                currentStep = -1;

                arrowLeft.Opacity = 50;
                arrowRight.Opacity = 100;

                route = null;

                //cursorPos.IsEnabled = true;

                ExtendedVisualStateManager.GoToElementState(LayoutRoot, "RoutePlanner", true);

                MyMap.Layers.Clear();

                wayPointLayer = null;


                e.Cancel = true;
            }*/
        }

        private async void MyMap_Tap(object sender, System.Windows.Input.GestureEventArgs ev)
        {
            Map map = sender as Map;
            Point p = ev.GetPosition(map);
            GeoCoordinate coord = map.ConvertViewportPointToGeoCoordinate(p);

            if (coord == null)
            {
                return;
            }

            App.networkStatus.networkIsBusy = true;

            MapLocation loc = await GeoUtils.StartReverseGeocode(coord);

            // Add a waypoint automatically
            _current = loc.GeoCoordinate;
            confirmWaypoint_Click(null, null);
        }

        private async void planRouteBorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            bool result = await rm.FindRoute(currentPlan);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error("Route Planning Error");

                MessageBox.Show(
                    "Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
            }
            else
            {
                PlotCachedRoute();
            }
        }

        private void PlotCachedRoute()
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            _geometryCoords.Clear();
            MyMap.MapElements.Clear();
            _max = new GeoCoordinate(90, -180);
            _min = new GeoCoordinate(-90, 180);
            _currentStep = -1;

            IEnumerable<RouteSection> sections = rm.GetRouteSections(currentPlan);
            foreach (var routeSection in sections)
            {
                DrawMapMarker(routeSection.Points.ToArray(), routeSection.Walking ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 127, 0, 255), routeSection.Walking);
            }
            ExtendedVisualStateManager.GoToElementState(LayoutRoot, "RouteFoundState", true);

            MyMap.SetView(rm.GetRouteBounds());

            if (!SettingManager.instance.GetBoolValue("tutorialEnabled", true)) return;
            bool shownTutorial = SettingManager.instance.GetBoolValue("shownTutorialRouteType", false);
            if (!shownTutorial)
                routeTutorialRouteType.Visibility = Visibility.Visible;
        }

        private async void myLocationBorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (LocationManager.instance.MyGeoPosition != null)
            {
                GeoCoordinate geo = CoordinateConverter.ConvertGeocoordinate(LocationManager.instance.MyGeoPosition.Coordinate);
                MapLocation loc = await GeoUtils.StartReverseGeocode(geo);

                setCurrentPosition(loc);

                confirmWaypoint_Click(sender, e);
            }
            else
            {
                Util.showLocationDialog();
            }
        }
    }
}