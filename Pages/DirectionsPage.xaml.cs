// Satnav mode
// Prompt feedback

using Cyclestreets.Annotations;
using Cyclestreets.CustomClasses;
using Cyclestreets.Managers;
using Cyclestreets.Objects;
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Cyclestreets.Pages
{
    #region CLASSES TO BE MOVED/REMOVED

    public class CSResult
    {
        public string ResultName;
        public GeoCoordinate Coord;

        public override string ToString()
        {
            return ResultName;
        }
    }

    #endregion

    public partial class DirectionsPage
    {

        readonly GeocodeQuery _geoQ;

        readonly Stackish<Pushpin> _waypoints = new Stackish<Pushpin>();

        GeoCoordinate _current;

        public static ListBoxPair[] RouteType = 
		{ 
			new ListBoxPair(AppResources.BalancedRoute, @"balanced"),
			new ListBoxPair(AppResources.FastestRoute, @"fastest"), 
			new ListBoxPair(AppResources.QuietestRoute, @"quietest") 
		};

        [NotNull]
        public static readonly String[] CycleSpeed = { @"10mph", @"12mph", @"15mph" };
        public static readonly String[] EnabledDisabled = { AppResources.Enabled, AppResources.Disabled };
        private MapLayer _wayPointLayer;

        public DirectionsPage()
        {
            InitializeComponent();

            //progress.DataContext = App.networkStatus;



            // Setup route type dropdown


            _geoQ = new GeocodeQuery();
            _geoQ.QueryCompleted += geoQ_QueryCompleted;

            ClearCurrentPosition();

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

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                PhoneApplicationService.Current.State.Remove(@"loadedRoute");
            }

            //LocationManager.instance.StopTracking();
            //LocationManager.instance.StartTracking();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                SetupTutorial();

                if (LocationManager.Instance.MyGeoPosition != null)
                {
                    MyMap.Center = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                }
            }
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
                    SettingManager.instance.SetBoolValue(@"tutorialEnabled", false);
                }
                else
                {
                    routeTutorial1.Visibility = Visibility.Visible;
                    SettingManager.instance.SetBoolValue(@"tutorialEnabled", true);
                }

                SettingManager.instance.SetBoolValue(@"shownTutorialQuestion", true);
            }
            else
            {
                if (SettingManager.instance.GetBoolValue(@"tutorialEnabled", true))
                    routeTutorial1.Visibility = Visibility.Visible;
            }
        }

        private void startPoint_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (startPoint.SelectedItem != null)
            {
                if (startPoint.SelectedItem is CSResult)
                {
                    CSResult res = startPoint.SelectedItem as CSResult;
                    SetCurrentPosition(res.Coord);

                    SmartDispatcher.BeginInvoke(() =>
                    {
                        MyMap.Map.SetView(res.Coord, 16);
                        Focus();
                        //MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
                    });
                    App.networkStatus.NetworkIsBusy = false;
                }
                else
                {
                    string start = startPoint.SelectedItem as string;

                    if (!_geoQ.IsBusy && !string.IsNullOrWhiteSpace(start) && start != AppResources.NoSuggestions)
                    {
                        _geoQ.SearchTerm = start;
                        _geoQ.GeoCoordinate = MyMap.Center;
                        _geoQ.QueryAsync();
                        App.networkStatus.NetworkIsBusy = true;

                    }
                }
            }
        }

        private void geoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Result.Count > 0)
            {
                GeoCoordinate g = e.Result[0].GeoCoordinate;
                SetCurrentPosition(g);
                App.networkStatus.NetworkIsBusy = false;
                Focus();
            }
            else
            {
                GeocodeQuery geo = sender as GeocodeQuery;
                if (geo == null) return;
                string searchString = geo.SearchTerm;

                WebClient wc = new WebClient();
                string location = String.Format(@"{0},{1}", geo.GeoCoordinate.Latitude, geo.GeoCoordinate.Longitude);
                location = HttpUtility.UrlEncode(location);

#if DEBUG
                Uri service = new Uri("http://demo.places.nlp.nokia.com/places/v1/discover/search?at=" + location + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson");
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
                Debug.WriteLine(e.Result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(e.Result);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error(String.Format(AppResources.CouldNotParseJSON, e.Result, ex.Message));
                    MessageBox.Show(AppResources.JSONParseError);
                }
                Debug.Assert(o != null, @"o != null");
                JArray suggestions = (JArray)o[@"results"][@"items"];
                if (suggestions.Count > 0)
                {
                    JArray pos = (JArray)suggestions[0][@"position"];
                    GeoCoordinate g = new GeoCoordinate((double)pos[0], (double)pos[1]);
                    SetCurrentPosition(g);

                    App.networkStatus.NetworkIsBusy = false;
                }
            }
            App.networkStatus.NetworkIsBusy = false;
            Focus();
        }

        private void cursorPos_Click(object sender, EventArgs e)
        {
            //MapLocation loc = await GeoUtils.StartReverseGeocode(MyMap.Center);
            SetCurrentPosition(MyMap.Center);

            confirmWaypoint_Click();
        }

        private void confirmWaypoint_Click()
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            rm.AddWaypoint(_current);

            if (_wayPointLayer == null)
            {
                _wayPointLayer = new MapLayer();

                MyMap.Map.Layers.Add(_wayPointLayer);
            }

            // Change last push pin from finish to intermediate
            if (_waypoints.Count > 1)
            {
                Pushpin last = _waypoints.Peek();
                last.Style = Resources[@"Intermediate"] as Style;
                last.Content = "" + (_waypoints.Count - 1);
            }

            Pushpin pp = new Pushpin();
            if (_waypoints.Count == 0)
                pp.Style = Resources[@"Start"] as Style;
            else
                pp.Style = Resources[@"Finish"] as Style;

            pp.BorderBrush = new SolidColorBrush(Colors.Red);
            pp.BorderThickness = new Thickness(200);
            pp.Tap += PinTapped;

            MapOverlay overlay = new MapOverlay { Content = pp };
            pp.GeoCoordinate = _current;
            overlay.GeoCoordinate = _current;
            overlay.PositionOrigin = new Point(0.3, 1.0);
            _wayPointLayer.Add(overlay);

            AddWaypoint(pp);

            if (SettingManager.instance.GetBoolValue(@"tutorialEnabled", true))
            {
                bool shownTutorial = SettingManager.instance.GetBoolValue(@"shownTutorialPin", false);
                if (!shownTutorial)
                    routeTutorialPin.Visibility = Visibility.Visible;
            }
            // Clear box
            startPoint.Text = "";

            ClearCurrentPosition();
        }

        private void PinTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Pushpin pp = sender as Pushpin;
            if (pp == null)
                return;

            RemoveWaypoint(pp);

            _wayPointLayer.Clear();

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
                _wayPointLayer.Add(overlay);

                // Set pin styles
                pp = _waypoints[i];
                if (i == 0)
                    pp.Style = Resources[@"Start"] as Style;
                else if (i == _waypoints.Count - 1)
                    pp.Style = Resources[@"Finish"] as Style;
                else
                {
                    pp.Style = Resources[@"Intermediate"] as Style;
                    pp.Content = "" + i;
                }
            }
        }

        private void AddWaypoint(Pushpin pp)
        {
            _waypoints.Push(pp);
        }

        private void RemoveWaypoint(Pushpin pp)
        {
            _waypoints.Remove(pp);
        }

        private void ClearCurrentPosition()
        {
            _current = null;
            //confirmWaypoint.IsEnabled = false;
        }

        private void SetCurrentPosition(GeoCoordinate c)
        {
            _current = c;
            SmartDispatcher.BeginInvoke(() => MyMap.Map.SetView(_current, 16));
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
            SettingManager.instance.SetBoolValue(@"shownTutorial", true);
        }

        private void routeTutorialPin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialPin.Visibility = Visibility.Collapsed;
            SettingManager.instance.SetBoolValue(@"shownTutorialPin", true);
        }

        /*private void routeTutorialRouteType_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialRouteInfo.Visibility = Visibility.Visible;
            routeTutorialRouteType.Visibility = Visibility.Collapsed;
        }

        private void routeTutorialRouteInfo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            routeTutorialRouteInfo.Visibility = Visibility.Collapsed;
            SettingManager.instance.SetBoolValue(@"shownTutorialRouteType", true);
        }*/
        
        private void startPoint_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AutoCompleteBox box = sender as AutoCompleteBox;
                Debug.Assert(box != null, @"box != null");
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
                        box.Text = String.Format(@"{0} {1}", newPostcodeStart, newPostcodeEnd);
                    }
                }
                //StartPointOnPopulating(box.Text, box);

                if (!_geoQ.IsBusy && !string.IsNullOrWhiteSpace(box.Text) && box.Text != AppResources.NoSuggestions)
                {
                    _geoQ.SearchTerm = box.Text;
                    _geoQ.GeoCoordinate = MyMap.Center;
                    _geoQ.QueryAsync();
                    App.networkStatus.NetworkIsBusy = true;
                }
                Focus();
            }
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

        private void MyMap_Tap(object sender, System.Windows.Input.GestureEventArgs ev)
        {
            CycleStreetsMap map = sender as CycleStreetsMap;
            Debug.Assert(map != null, @"map != null");
            
            Point p = ev.GetPosition(map.Map);
            GeoCoordinate coord = map.Map.ConvertViewportPointToGeoCoordinate(p);

            if (coord == null)
            {
                return;
            }

            SetCurrentPosition(coord);

            confirmWaypoint_Click();
        }

        private void planRouteBorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {   
            NavigationService.Navigate(new Uri("/Pages/RouteOverview.xaml?mode=planroute", UriKind.Relative));
        }


        private void myLocationBorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (LocationManager.Instance.MyGeoPosition != null)
            {
                GeoCoordinate geo = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                SetCurrentPosition(geo);
                if (geo.HorizontalAccuracy < 60)
                    confirmWaypoint_Click();
                else
                {
                    MessageBox.Show(AppResources.PoorGPS,
                        AppResources.PoorGPSTitle, MessageBoxButton.OK);
                }
            }
            else
            {
                Util.showLocationDialog();
            }
        }
    }
}