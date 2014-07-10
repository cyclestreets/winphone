using Microsoft.Phone.Maps.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Cyclestreets.Utils
{
    public static class GeoUtils
    {
        // Find geo coord 
        static readonly ReverseGeocodeQuery _revGeoQ = null;
        
        // extension method for async await ReverseGeoCode
        public static Task<IList<MapLocation>> QuerryTaskAsync(this ReverseGeocodeQuery reverseGeocode)
        {
            TaskCompletionSource<IList<MapLocation> > tcs=new TaskCompletionSource<IList<MapLocation>>();
            EventHandler<QueryCompletedEventArgs<IList<MapLocation>>> queryCompleted = null;
            queryCompleted=(send, arg) =>
            {
                //Unregister event so that QuerryTaskAsync can be called several time on same object
                reverseGeocode.QueryCompleted -= queryCompleted;

                if (arg.Error != null)
                {
                    tcs.SetException(arg.Error);
                }else if (arg.Cancelled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(arg.Result);
                }
            };

            reverseGeocode.QueryCompleted += queryCompleted;

            reverseGeocode.QueryAsync();

            return tcs.Task;
        }

        /// <summary>
        /// Look up a list of possible places from a search term. 
        /// Bias results towards location passed in
        /// </summary>
        /// <param name="searchTerm">place to search for</param>
        /// <param name="centerOfSearch">where to center the search</param>
        /// <returns></returns>
        public static Task<List<string>> StartPlaceSearch(string searchTerm, GeoCoordinate centerOfSearch)
        {
            TaskCompletionSource<List<string>> tcs1 = new TaskCompletionSource<List<string>>();
            Task<List<string>> t1 = tcs1.Task;

            App.networkStatus.networkIsBusy = true;
            System.Diagnostics.Debug.WriteLine(@"Searching for " + searchTerm);

            
            string searchTermSafe = HttpUtility.UrlEncode(searchTerm);
            string myLocation = HttpUtility.UrlEncode(centerOfSearch.Latitude + "," + centerOfSearch.Longitude);


            var client = new RestClient("http://www.cyclestreets.net/api/places/v1/");
#endif
            var request = new RestRequest("suggest", Method.GET);
            request.AddParameter("at", myLocation);
            request.AddParameter("q", searchTermSafe);
            request.AddParameter("app_id", App.hereAppID);
            request.AddParameter("app_code", App.hereAppToken);
            request.AddParameter("accept", "application/json");

            // execute the request
            client.ExecuteAsync(request, delegate(IRestResponse response) 
            { 
                string result = response.Content;
                System.Diagnostics.Debug.WriteLine(result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(result);
                }
                catch (Exception ex)
                {
                    MarkedUp.AnalyticClient.Error("Could not parse JSON " + result + " " + ex.Message);
                    MessageBox.Show("Could not parse location data information from server. Please let us know about this error with what you were typing in the search box to cause this problem.");
                }

                if (o == null) return;
                JArray suggestions = (JArray)o["suggestions"];
                List<string> names = new List<string>();
                if (suggestions.Count > 0)
                {
                    names.AddRange(suggestions.Cast<string>());
                    tcs1.SetResult(names);
                }
                else
                {
                    /*System.Diagnostics.Debug.WriteLine(@"Starting backup search for " + searchTermSafe);
                        
                        placeSearch = new WebClient();
                        string prefix = HttpUtility.UrlEncode(startPoint.Text);

                        string myLocation = "";
                        LocationRectangle rect = GetMapBounds();
                        myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
                        //myLocation = HttpUtility.UrlEncode( myLocation );

                        Uri service = new Uri("http://cambridge.cyclestreets.net/api/geocoder.json?key=" + App.apiKey + myLocation + "&street=" + prefix);

                        placeSearch.DownloadStringCompleted += CSDownloadStringCompleted;
                        placeSearch.DownloadStringAsync(service, e.UserState);*/

                    MarkedUp.AnalyticClient.Error("Could not find place named " + result);
                    tcs1.SetResult(null);
                }
            });
            return t1;
        }


        public static Task<MapLocation> StartReverseGeocode(GeoCoordinate center)
        {
            if (!_revGeoQ.IsBusy)
            {
                _revGeoQ.GeoCoordinate = center;
                IList<MapLocation> results = await _revGeoQ.QueryTaskAsync();
                return results.FirstOrDefault();
            }
        }

        
    }
}
