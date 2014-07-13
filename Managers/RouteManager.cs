using System.Linq;
using Cyclestreets.Common;
using Cyclestreets.Pages;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Threading.Tasks;
using System.Windows;

namespace Cyclestreets.Managers
{
    public class RouteManager : BindableBase
    {
        private readonly Stackish<GeoCoordinate> _waypoints = new Stackish<GeoCoordinate>();
        private List<RouteSection> cachedRouteData = null;
        private bool _isBusy = false;
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

        private dynamic _journeyObject;

        public RouteManager()
        {

        }

        public void AddWaypoint(GeoCoordinate c)
        {
            _waypoints.Push(c);
        }

        public Task<bool> FindRoute()
        {
            TaskCompletionSource<bool> tcs1 = new TaskCompletionSource<bool>();
            Task<bool> t1 = tcs1.Task;

            // Clear the cache
            cachedRouteData = null;

            string plan = SettingManager.instance.GetStringValue("defaultRouteType", "balanced");
            plan = plan.Replace(" route", "");

            string speedSetting = SettingManager.instance.GetStringValue("cycleSpeed", "12mph");

            int speed = Util.getSpeedFromString(speedSetting);
            const int useDom = 0; // 0=xml 1=gml

            string itinerarypoints = _waypoints.Where(waypoint => waypoint != null).Aggregate("", (current, waypoint) => current + waypoint.Longitude + "," + waypoint.Latitude + "|");
            itinerarypoints = itinerarypoints.TrimEnd('|');

            var client = new RestClient("http://www.cyclestreets.net/api");
            var request = new RestRequest("journey.json", Method.GET);
            request.AddParameter("key", App.apiKey);
            request.AddParameter("plan", plan);
            request.AddParameter("itinerarypoints", itinerarypoints);
            request.AddParameter("speed", speed);
            request.AddParameter("useDom", useDom);

            IsBusy = true;

            // execute the request
            client.ExecuteAsync(request, r =>
            {
                string result = r.Content;

                bool res = ParseRouteData(result);

                IsBusy = false;
                tcs1.SetResult(res);
            });

            return t1;
        }

        public bool ParseRouteData(string currentRouteData)
        {
            _journeyObject = null;
            if (currentRouteData != null)
            {
                try
                {
                    _journeyObject = (dynamic)JObject.Parse(currentRouteData);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + currentRouteData.Trim() + " " + ex.Message);
                    MessageBox.Show("Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
                }
            }

            if (_journeyObject == null || _journeyObject.marker == null)
            {
                MessageBox.Show(AppResources.NoRouteFoundTryAnotherSearch, AppResources.NoRoute, MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        internal IEnumerable<RouteSection> GetRouteSections()
        {
            if (_journeyObject == null)
                return null;

            if (cachedRouteData != null)
                return cachedRouteData;

            List<RouteSection> result = new List<RouteSection>();
            foreach (var marker in _journeyObject.marker)
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
                sectionObj.Walking = int.Parse(section.walk.ToString())==1?true:false;
                result.Add(sectionObj);
            }

            cachedRouteData = result;
            return result;
        }
    }
}
