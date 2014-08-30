using Cyclestreets.Common;
using Cyclestreets.Objects;
using Cyclestreets.Pages;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Microsoft.Phone.Maps.Controls;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Cyclestreets.Managers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RouteManager : BindableBase
    {
        private readonly Stackish<GeoCoordinate> _waypoints = new Stackish<GeoCoordinate>();
        private List<RouteSection> _cachedRouteData;
        private bool _isBusy;
        private dynamic _currentParsedRoute;
        private Dictionary<string, string> _journeyMap = new Dictionary<string, string>();

        public RouteOverviewObject Overview
        {
            get { return _overview; }
            private set { SetProperty(ref _overview, value); }
        }

        public Dictionary<string, string> RouteCacheForSaving
        {
            get { return _journeyMap; }
            set { _journeyMap = value; OnPropertyChanged("ReadyToDisplayRoute"); }
        }

        public double BusyWidth
        {
            get { return App.RootFrame.ActualWidth; }
        }

        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                SetProperty(ref _isBusy, value);
            }
        }

        public bool ReadyToPlanRoute
        {
            get { return _waypoints.Count >= 2; }
        }

        public void AddWaypoint(GeoCoordinate c)
        {
            _waypoints.Push(c);
        }

        public void RemoveWaypoint(GeoCoordinate geoCoordinate)
        {
            _waypoints.Remove(geoCoordinate);
        }

        public List<RouteSection> CurrentRoute
        {
            get { return _cachedRouteData; }
            set
            {
                SetProperty(ref _cachedRouteData, value);

                OnPropertyChanged("HeightChart");
                OnPropertyChanged("HorizontalLabelInterval");
            }
        }

        private int _currentStep;
        private RouteOverviewObject _overview;

        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_cachedRouteData != null && value < _cachedRouteData.Count && value >= 0)
                {
                    _currentStep = value;
                    OnPropertyChanged("CurrentStepText");
                }
            }
        }

        public List<HeightData> HeightChart
        {
            get
            {
                List<HeightData> result = new List<HeightData>();
                int runningDistance = 0;
                foreach (var routeSection in _cachedRouteData)
                {
                    //for(int i=0; i < routeSection.Distances.Count; i++)
                    if (routeSection.Distances.Count > 0)
                    {
                        runningDistance += routeSection.Distance;
                        HeightData h = new HeightData
                        {
                            Distance = runningDistance,
                            Height = routeSection.Height[0]
                        };
                        result.Add(h);
                    }

                }

                return result;
            }
        }

        public int HorizontalLabelInterval
        {
            get
            {
                return 1;//(int)((float)HeightChart.Count / 10f);
            }
        }

        public string CurrentStepText
        {
            get
            {
                if (_cachedRouteData != null)
                {
                    return _cachedRouteData[CurrentStep].Description;
                }
                return "";
            }
        }

        public GeoCoordinate CurrentGeoCoordinate
        {
            get
            {
                if (_cachedRouteData != null && _cachedRouteData[CurrentStep].Points.Count > 0)
                {
                    return _cachedRouteData[CurrentStep].Points[0];
                }
                return null;
            }
        }

        public double CurrentBearing
        {
            get
            {
                if (_cachedRouteData != null)
                {
                    return _cachedRouteData[CurrentStep].Bearing;
                }
                return 0;
            }
        }

        public Task<bool> FindRoute(string routeType, bool newRoute = true)
        {
            TaskCompletionSource<bool> tcs1 = new TaskCompletionSource<bool>();
            Task<bool> t1 = tcs1.Task;

            // Clear the cache
            _cachedRouteData = null;

            if ((!newRoute && _journeyMap.ContainsKey(routeType)))
            {
                tcs1.SetResult(true);
                return t1;
            }

            string speedSetting = SettingManager.instance.GetStringValue(@"cycleSpeed", @"12mph");

            int speed = Util.getSpeedFromString(speedSetting);
            const int useDom = 0; // 0=xml 1=gml

            string itinerarypoints = _waypoints.Where(waypoint => waypoint != null).Aggregate("", (current, waypoint) => current + waypoint.Longitude + "," + waypoint.Latitude + "|");
            itinerarypoints = itinerarypoints.TrimEnd('|');

            var client = new RestClient("http://www.cyclestreets.net/api");
            var request = new RestRequest("journey.json", Method.GET);
            request.AddParameter("key", App.apiKey);
            request.AddParameter("useDom", useDom);
            request.AddParameter("plan", routeType);
            if (!newRoute)
            {
                //http://www.cyclestreets.net/api/journey.xml?key=registeredapikey&useDom=1&itinerary=345529&plan=fastest
                request.AddParameter("itinerary", Overview.RouteNumber);
            }
            else
            {
                request.AddParameter("itinerarypoints", itinerarypoints);
                request.AddParameter("speed", speed);
            }


            IsBusy = true;

            // execute the request
            client.ExecuteAsync(request, r =>
            {
                string result = r.Content;

                bool res = ParseRouteData(result, routeType, newRoute);

                IsBusy = false;
                tcs1.SetResult(res);
            });

            return t1;
        }

        public LocationRectangle GetRouteBounds()
        {
            List<GeoCoordinate> allPoints = new List<GeoCoordinate>();

            foreach (var segment in _cachedRouteData)
            {
                allPoints.AddRange(segment.Points);
            }

            return LocationRectangle.CreateBoundingRectangle(allPoints.ToArray());
        }

        public bool ParseRouteData(string currentRouteData, string routeType, bool newRoute)
        {
            if (newRoute)
            {
                _journeyMap.Clear();
                OnPropertyChanged("ReadyToDisplayRoute");
            }

            if (currentRouteData != null)
            {
                try
                {
                    _currentParsedRoute = JObject.Parse(currentRouteData);
                    if (_currentParsedRoute != null && !_journeyMap.ContainsKey(routeType))
                    {
                        _journeyMap.Add(routeType, currentRouteData);
                        OnPropertyChanged("ReadyToDisplayRoute");
                    }
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + currentRouteData.Trim() + " " + ex.Message);
                    MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                }
            }

            if (_currentParsedRoute != null && _currentParsedRoute.marker != null)
                return true;

            MessageBox.Show(AppResources.NoRouteFoundTryAnotherSearch, AppResources.NoRoute, MessageBoxButton.OK);
            return false;
        }

        internal IEnumerable<RouteSection> GetRouteSections(string routeType)
        {
            if (!_journeyMap.ContainsKey(routeType))
                return null;

            if (_cachedRouteData != null)
                return _cachedRouteData;

            List<RouteSection> result = new List<RouteSection>();
            if (!ParseRouteData(_journeyMap[routeType], routeType, false))
                return null;
            dynamic journeyObject = _currentParsedRoute;
            int lastDistance = 0;
            GeoCoordinate endPoint = null;
            foreach (var marker in journeyObject.marker)
            {
                if (marker["@attributes"].type == "route")
                {
                    var section = marker["@attributes"];
                    //RouteSection sectionObj = new RouteSection();
                    double longitude = double.Parse(section.finish_longitude.ToString());
                    double latitude = double.Parse(section.finish_latitude.ToString());
                    endPoint = new GeoCoordinate(latitude, longitude);
                    // sectionObj.Points.Add(new GeoCoordinate(latitude, longitude));
                    //sectionObj.Description = "Start " + section.start;

                    Overview = new RouteOverviewObject
                    {
                        Quietness = int.Parse(section.quietness.ToString()),
                        RouteNumber = int.Parse(section.itinerary.ToString()),
                        RouteLength = int.Parse(section.length.ToString()),
                        SignalledJunctions = int.Parse(section.signalledJunctions.ToString()),
                        SignalledCrossings = int.Parse(section.signalledCrossings.ToString()),
                        GrammesCo2Saved = int.Parse(section.grammesCO2saved.ToString()),
                        calories = int.Parse(section.calories.ToString()),
                        RouteDuration = int.Parse(section.time.ToString())
                    };

                    //result.Add(sectionObj);
                }
                else if (marker["@attributes"].type == "segment")
                {
                    var section = marker["@attributes"];
                    RouteSection sectionObj = null;
                    sectionObj = result.Count == 0 ? new StartPoint() : new RouteSection();
                    string[] points = section.points.ToString().Split(' ');
                    foreach (string t in points)
                    {
                        string[] xy = t.Split(',');

                        double longitude = double.Parse(xy[0]);
                        double latitude = double.Parse(xy[1]);
                        sectionObj.Points.Add(new GeoCoordinate(latitude, longitude));
                    }
                    string[] temp = section.elevations.ToString().Split(',');
                    int[] convertedItems = Util.ConvertAll(temp, int.Parse);
                    sectionObj.Height = new List<int>(convertedItems);
                    temp = section.distances.ToString().Split(',');
                    convertedItems = Util.ConvertAll(temp, int.Parse);
                    sectionObj.Distances = new List<int>(convertedItems);
                    sectionObj.Walking = int.Parse(section.walk.ToString()) == 1;
                    sectionObj.Description = section.name.ToString().Equals("lcn?") ? AppResources.UnknownStreet : section.name;
                    sectionObj.Distance = lastDistance;
                    lastDistance = int.Parse(section.distance.ToString());
                    sectionObj.Bearing = double.Parse(section.startBearing.ToString());
                    sectionObj.Time = int.Parse(section.time.ToString());
                    sectionObj.Turn = section.turn.ToString();
                    result.Add(sectionObj);
                }
            }

            EndPoint ep = new EndPoint
            {
                Distance = lastDistance,
                Turn = "straight on",
            };
            ep.Points.Add(endPoint);
            result.Add(ep);

            CurrentRoute = result;
            return result;
        }

        internal string HasCachedRoute(string defaultPlan)
        {
            if (defaultPlan != null && _journeyMap.ContainsKey(defaultPlan))
                return defaultPlan;
            return _journeyMap.Count > 0 ? _journeyMap.First().Key : null;
        }

        internal void GenerateDebugData()
        {

        }
    }
}
