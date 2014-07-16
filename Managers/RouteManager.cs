using Cyclestreets.Common;
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
        private Dictionary<string, dynamic> _journeyMap = new Dictionary<string, dynamic>();
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
                if (marker["@attributes"].type != "segment") continue;
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
                sectionObj.Distance = new List<int>(convertedItems);
                sectionObj.Walking = int.Parse(section.walk.ToString()) == 1;
                result.Add(sectionObj);
            }

            _cachedRouteData = result;
            return result;
        }


    }
}
