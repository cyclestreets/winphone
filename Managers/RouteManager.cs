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
    public class RouteManager : BindableBase
    {
        private readonly Stackish<GeoCoordinate> _waypoints = new Stackish<GeoCoordinate>();
        private List<RouteSection> _cachedRouteData;
        private bool _isBusy;
        private readonly Dictionary<string, dynamic> _journeyMap = new Dictionary<string, dynamic>();

        public RouteOverview overview { get; set; }

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
            }
        }

        private int _currentStep;
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

        public IEnumerable<HeightData> HeightChart
        {
            get
            {
                List<HeightData> result = new List<HeightData>();
                int runningDistance = 0;
                foreach (var routeSection in _cachedRouteData)
                {
                    //for(int i=0; i < routeSection.Distances.Count; i++)
                    if ( routeSection.Distances.Count > 0 )
                    {
                        runningDistance += routeSection.Distances[0];
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

            string speedSetting = SettingManager.instance.GetStringValue("cycleSpeed", "12mph");

            int speed = Util.getSpeedFromString(speedSetting);
            const int useDom = 0; // 0=xml 1=gml

            string itinerarypoints = _waypoints.Where(waypoint => waypoint != null).Aggregate("", (current, waypoint) => current + waypoint.Longitude + "," + waypoint.Latitude + "|");
            itinerarypoints = itinerarypoints.TrimEnd('|');

            var client = new RestClient("http://www.cyclestreets.net/api");
            var request = new RestRequest("journey.json", Method.GET);
            request.AddParameter("key", App.apiKey);
            request.AddParameter("plan", routeType);
            request.AddParameter("itinerarypoints", itinerarypoints);
            request.AddParameter("speed", speed);
            request.AddParameter("useDom", useDom);

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
                _journeyMap.Clear();

            dynamic journeyObject = null;
            if (currentRouteData != null)
            {
                try
                {
                    journeyObject = JObject.Parse(currentRouteData);
                    if (journeyObject != null)
                        _journeyMap.Add(routeType, journeyObject);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + currentRouteData.Trim() + " " + ex.Message);
                    MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                }
            }

            if (journeyObject != null && journeyObject.marker != null) return true;
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
            dynamic journeyObject = _journeyMap[routeType];
            foreach (var marker in journeyObject.marker)
            {
                if (marker["@attributes"].type == "route")
                {
                    var section = marker["@attributes"];
                    RouteSection sectionObj = new RouteSection();
                    double longitude = double.Parse(section.start_longitude.ToString());
                    double latitude = double.Parse(section.start_latitude.ToString());
                    sectionObj.Points.Add(new GeoCoordinate(latitude, longitude));
                    sectionObj.Description = "Start " + section.start;

                    overview = new RouteOverview
                    {
                        Quietness = int.Parse(section.quietness.ToString()),
                        RouteNumber = int.Parse(section.itinerary.ToString()),
                        RouteLength = int.Parse(section.length.ToString()),
                        signalledJunctions = int.Parse(section.signalledJunctions.ToString()),
                        signalledCrossings = int.Parse(section.signalledCrossings.ToString()),
                        grammesCO2saved = int.Parse(section.grammesCO2saved.ToString()),
                        calories = int.Parse(section.calories.ToString())
                    };

                    result.Add(sectionObj);
                }
                else if (marker["@attributes"].type == "segment")
                {
                    var section = marker["@attributes"];
                    RouteSection sectionObj = new RouteSection();
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
                    sectionObj.Description = section.turn.ToString() + " " + section.name;
                    sectionObj.Distance = int.Parse(section.distance.ToString());
                    sectionObj.Bearing = double.Parse(section.startBearing.ToString());
                    sectionObj.Time = int.Parse(section.time.ToString());
                    result.Add(sectionObj);
                }
            }

            CurrentRoute = result;
            return result;
        }


    }
}
