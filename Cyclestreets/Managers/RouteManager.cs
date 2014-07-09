//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using Cyclestreets.Common;
using Cyclestreets.Objects;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Device.Location;
using System.Threading.Tasks;
using System.Windows;
namespace Cyclestreets.Managers
{
    public class RouteManager : BindableBase
    {
        private readonly Stackish<GeoCoordinate> _waypoints = new Stackish<GeoCoordinate>();

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

            string plan = SettingManager.instance.GetStringValue("defaultRouteType", "balanced");
            plan = plan.Replace(" route", "");

            string speedSetting = SettingManager.instance.GetStringValue("cycleSpeed", "12mph");

            string itinerarypoints = "";
            int speed = Util.getSpeedFromString(speedSetting);
            int useDom = 0; // 0=xml 1=gml

            foreach (var p in _waypoints)
            {
                itinerarypoints = itinerarypoints + p.Longitude + "," + p.Latitude + "|";
            }
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

                bool r = ParseJSON(result);

                IsBusy = false;
                tcs1.SetResult(r);
            });

            return t1;
        }

        private bool ParseJSON(string currentRouteData)
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
    }
}
