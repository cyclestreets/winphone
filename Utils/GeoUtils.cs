using Cyclestreets.Resources;
using Microsoft.Phone.Maps.Services;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Cyclestreets.Utils
{
    public static class GeoUtils
    {
        // Find geo coord 
        static readonly ReverseGeocodeQuery _revGeoQ = new ReverseGeocodeQuery();

        // extension method for async await ReverseGeoCode
        public static Task<IList<MapLocation>> QueryTaskAsync(this ReverseGeocodeQuery reverseGeocode)
        {
            TaskCompletionSource<IList<MapLocation>> tcs = new TaskCompletionSource<IList<MapLocation>>();
            EventHandler<QueryCompletedEventArgs<IList<MapLocation>>> queryCompleted = null;
            queryCompleted = (send, arg) =>
            {
                //Unregister event so that QuerryTaskAsync can be called several time on same object
                reverseGeocode.QueryCompleted -= queryCompleted;

                if (arg.Error != null)
                {
                    tcs.SetException(arg.Error);
                }
                else if (arg.Cancelled)
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

        public enum PlaceSearchPriority
        {
            kNone,  // Should be used internally only
            kCycleStreetsOnly,
            kHEREMapsOnly,
            kCycleStreetsThenHERE,
            kHEREThenCycleStreets,
        };

        /// <summary>
        /// Look up a list of possible places from a search term. 
        /// Bias results towards location passed in
        /// </summary>
        /// <param name="searchTerm">place to search for</param>
        /// <param name="centerOfSearch">where to center the search</param>
        /// <param name="priority">Which geocoder to use for resolving place names</param>
        /// <returns></returns>
        public static Task<List<string>> StartPlaceSearch(string searchTerm, GeoCoordinate centerOfSearch, PlaceSearchPriority priority)
        {
            TaskCompletionSource<List<string>> tcs1 = new TaskCompletionSource<List<string>>();
            Task<List<string>> t1 = tcs1.Task;

            App.networkStatus.NetworkIsBusy = true;
            Debug.WriteLine(@"Searching for " + searchTerm);

            switch (priority)
            {
                case PlaceSearchPriority.kCycleStreetsOnly:
                    StartCycleStreetsPlaceSearch(searchTerm, centerOfSearch, tcs1, PlaceSearchPriority.kNone);
                    break;
                case PlaceSearchPriority.kCycleStreetsThenHERE:
                    StartCycleStreetsPlaceSearch(searchTerm, centerOfSearch, tcs1, PlaceSearchPriority.kHEREMapsOnly);
                    break;
                case PlaceSearchPriority.kHEREMapsOnly:
                    StartHereMapsPlaceSearch(searchTerm, centerOfSearch, tcs1, PlaceSearchPriority.kNone);
                    break;
                case PlaceSearchPriority.kHEREThenCycleStreets:
                    StartHereMapsPlaceSearch(searchTerm,centerOfSearch,tcs1,PlaceSearchPriority.kHEREThenCycleStreets);
                    break;
            }

            return t1;
        }

        private class NoResultsException : Exception
        {
            public NoResultsException(string searchTerm) : base(searchTerm)
            {
                
            }
        }

        #region Cyclestreets geocoding
        private static void StartCycleStreetsPlaceSearch(string searchTerm, GeoCoordinate centerOfSearch, TaskCompletionSource<List<string>> tcs1, PlaceSearchPriority priority)
        {
            string searchTermSafe = HttpUtility.UrlEncode(searchTerm);
            
            var client = new RestClient(@"http://www.cyclestreets.net/api/");

            var request = new RestRequest(@"geocoder.json", Method.GET);
            request.AddParameter(@"key", App.apiKey);
            request.AddParameter(@"n", centerOfSearch.Latitude);
            request.AddParameter(@"s", centerOfSearch.Latitude);
            request.AddParameter(@"e", centerOfSearch.Longitude);
            request.AddParameter(@"w", centerOfSearch.Longitude);
            request.AddParameter(@"street", searchTermSafe);
            // execute the request
            client.ExecuteAsync(request, a => cyclestreetsGeocoderResponse(a, searchTerm, tcs1, centerOfSearch,priority));

        }

        private static void cyclestreetsGeocoderResponse(IRestResponse response, string searchTerm, TaskCompletionSource<List<string>> tcs1, GeoCoordinate centerOfSearch, PlaceSearchPriority priority)
        {
            string result = response.Content;
            try
            {
                if (string.IsNullOrWhiteSpace(result))
                {
                    Debug.WriteLine(@"Cyclestreets search produced no results " + searchTerm);
                    throw new NoResultsException(searchTerm);
                }

                Debug.WriteLine(result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(result);
                }
                catch (Exception ex)
                {
                    AnalyticClient.Error(string.Format(@"Could not parse JSON {0} {1}", result, ex.Message));
                    MessageBox.Show(AppResources.CouldNotParse);
                    throw new NoResultsException(searchTerm);
                }

                if (o == null)
                {
                    throw new NoResultsException(searchTerm);
                };

                JObject results = (JObject)o[@"results"];
                JToken resultArray;
                if (results.TryGetValue(@"result", out resultArray))
                {
                    JArray suggestions = resultArray as JArray;
                    List<string> names = new List<string>();
                    if (suggestions == null)
                    {
                        var suggestion = resultArray as JObject;
                        if (suggestion == null) return;
                        names.Add(suggestion[@"name"].ToString());
                    }
                    else
                    {
                        if (suggestions.Count <= 0)
                            throw new NoResultsException(searchTerm);
                        names.AddRange(suggestions.Select(x => x[@"name"].ToString()).ToArray());
                    }

                    tcs1.SetResult(names);
                }
                else
                {
                    throw new NoResultsException(searchTerm);
                }
            }
            catch (NoResultsException ex)
            {
                switch (priority)
                {
                    case PlaceSearchPriority.kHEREMapsOnly:
                        StartHereMapsPlaceSearch(searchTerm,centerOfSearch,tcs1,PlaceSearchPriority.kNone);
                        break;
                    default:
                    {
                        tcs1.SetResult(null);
                        break;
                    }
                }
                //throw;
            }
        }
#endregion

        #region HERE Maps geocoding
        public static void StartHereMapsPlaceSearch(string searchTerm, GeoCoordinate centerOfSearch, TaskCompletionSource<List<string>> tcs1, PlaceSearchPriority priority)
        {
            string searchTermSafe = HttpUtility.UrlEncode(searchTerm);

#if DEBUG
            //Uri service = new Uri("http://demo.places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson");
            var client = new RestClient(@"http://places.cit.api.here.com/places/v1/");
#else
               // Uri service = new Uri("http://places.nlp.nokia.com/places/v1/discover/search?at=" + myLocation + "&q=" + searchString + "&app_id=" + App.hereAppID + "&app_code=" + App.hereAppToken + "&accept=application%2Fjson");
            var client = new RestClient(@"http://places.api.here.com/places/v1/");
#endif
            var request = new RestRequest(@"suggest", Method.GET);
            request.AddParameter(@"at", string.Format(@"{0},{1}", centerOfSearch.Latitude, centerOfSearch.Longitude));
            request.AddParameter(@"q", searchTermSafe);
            request.AddParameter(@"app_id", App.hereAppID);
            request.AddParameter(@"app_code", App.hereAppToken);
            request.AddParameter(@"accept", @"application/json");

            // execute the request
            client.ExecuteAsync(request, a => hereMapsGeocoderResponse(a, searchTerm, tcs1, centerOfSearch,priority));
        }

        private static void hereMapsGeocoderResponse(IRestResponse response, string searchTerm, TaskCompletionSource<List<string>> tcs1, GeoCoordinate centerOfSearch, PlaceSearchPriority priority)
        {
            string result = response.Content;
            try
            {
                if (string.IsNullOrWhiteSpace(result))
                {
                    Debug.WriteLine(@"Cyclestreets search produced no results " + searchTerm);
                    throw new NoResultsException(searchTerm);
                }

                Debug.WriteLine(result);
                JObject o = null;
                try
                {
                    o = JObject.Parse(result);
                }
                catch (Exception ex)
                {
                    AnalyticClient.Error(string.Format(@"Could not parse JSON {0} {1}", result, ex.Message));
                    MessageBox.Show(AppResources.CouldNotParse);
                    throw new NoResultsException(searchTerm);
                }

                if (o == null)
                {
                    throw new NoResultsException(searchTerm);
                };

                JArray resultArray = o.Value<JArray>(@"suggestions");
                if ( resultArray == null || resultArray.Count == 0)
                    throw new NoResultsException(searchTerm);

                List<string> names = new List<string>();
                names.AddRange(resultArray.Select(x=>x.Value<string>()));
                tcs1.SetResult(names);
                
            }
            catch (NoResultsException ex)
            {
                switch (priority)
                {
                    case PlaceSearchPriority.kCycleStreetsOnly:
                        StartHereMapsPlaceSearch(searchTerm, centerOfSearch, tcs1, PlaceSearchPriority.kNone);
                        break;
                    default:
                        {
                            tcs1.SetResult(null);
                            break;
                        }
                }
                //throw;
            }
        }
        #endregion

        public async static Task<MapLocation> StartReverseGeocode(GeoCoordinate center)
        {
            if (_revGeoQ.IsBusy) return null;
            try
            {
                _revGeoQ.GeoCoordinate = center;
                IList<MapLocation> results = await _revGeoQ.QueryTaskAsync();
                return results.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static GeoCoordinate ConvertGeocoordinate(Geocoordinate geocoordinate)
        {
            return new GeoCoordinate
                (
                geocoordinate.Point.Position.Latitude,
                geocoordinate.Point.Position.Longitude,
                geocoordinate.Point.Position.Altitude,
                geocoordinate.Accuracy,
                geocoordinate.Accuracy,
                /*geocoordinate.AltitudeAccuracy ?? Double.NaN,*/
                geocoordinate.Speed ?? Double.NaN,
                geocoordinate.Heading ?? Double.NaN
                );
        }
    }
}
