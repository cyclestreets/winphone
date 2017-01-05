using Cyclestreets.Common;
using Cyclestreets.Objects;
using Cyclestreets.Pages;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
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
		private JObject _currentParsedRoute;
		private Dictionary<string, string> _journeyMap = new Dictionary<string, string>();

		public RouteOverviewObject Overview
		{
			get { return _overview; }
			private set { SetProperty( ref _overview, value ); }
		}

		public Dictionary<string, string> RouteCacheForSaving
		{
			get { return _journeyMap; }
			set { _journeyMap = value; OnPropertyChanged( @"ReadyToDisplayRoute" ); }
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
				SetProperty( ref _isBusy, value );
			}
		}

		public bool ReadyToPlanRoute
		{
			get { return _waypoints.Count >= 2; }
		}

		public void AddWaypoint( GeoCoordinate c )
		{
			_waypoints.Push( c );
			OnPropertyChanged( @"ReadyToPlanRoute" );

		}

		public void RemoveWaypoint( GeoCoordinate geoCoordinate )
		{
			_waypoints.Remove( geoCoordinate );
			OnPropertyChanged( @"ReadyToPlanRoute" );
		}

		public void ClearAllWaypoints()
		{
			_waypoints.Clear();
			OnPropertyChanged( @"ReadyToPlanRoute" );
		}

		public List<RouteSection> CurrentRoute
		{
			get { return _cachedRouteData; }
			set
			{
				SetProperty( ref _cachedRouteData, value );

				OnPropertyChanged( @"HeightChart" );
				OnPropertyChanged( @"HorizontalLabelInterval" );
			}
		}

		private int _currentStep;
		private RouteOverviewObject _overview;

		public int CurrentStep
		{
			get { return _currentStep; }
			set
			{
				if( _cachedRouteData != null && value < _cachedRouteData.Count && value >= 0 )
				{
					_currentStep = value;
					OnPropertyChanged( @"CurrentStepText" );
				}
			}
		}

		public List<HeightData> HeightChart
		{
			get
			{
				List<HeightData> result = new List<HeightData>();
				int runningDistance = 0;
				foreach( var routeSection in _cachedRouteData )
				{
					//for(int i=0; i < routeSection.Distances.Count; i++)
					if( routeSection.Distances.Count > 0 )
					{
						runningDistance += routeSection.Distance;
						HeightData h = new HeightData
						{
							Distance = runningDistance,
							Height = routeSection.Height[0]
						};
						result.Add( h );
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
				if( _cachedRouteData != null )
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
				if( _cachedRouteData != null && _cachedRouteData[CurrentStep].Points.Count > 0 )
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
				if( _cachedRouteData != null )
				{
					return _cachedRouteData[CurrentStep].Bearing;
				}
				return 0;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetTimeSeconds"></param>
		/// <param name="targetMiles"></param>
		/// <param name="poiNames">Comma seperated list of points of interest to try and apss through</param>
		/// <returns></returns>
		public Task<bool> FindLeisureRoute( int targetTimeSeconds = -1, int targetMiles = -1, string poiNames = null )
		{
			TaskCompletionSource<bool> tcs1 = new TaskCompletionSource<bool>();
			Task<bool> t1 = tcs1.Task;

			// Clear the cache
			_cachedRouteData = null;

			string speedSetting = SettingManager.instance.GetStringValue( @"cycleSpeed", DirectionsPage.CycleSpeed[1] );

			int speed = Util.getSpeedFromString( speedSetting );
			const int useDom = 0; // 0=xml 1=gml

			var client = new RestClient( @"http://www.cyclestreets.net/api" );
			var request = new RestRequest( @"journey.json", Method.GET );
			request.AddParameter( @"key", App.apiKey );
			request.AddParameter( @"useDom", useDom );
			request.AddParameter( @"plan", @"leisure" );
			request.AddParameter( @"itinerarypoints", String.Format( @"{0},{1}", LocationManager.Instance.MyGeoPosition.Coordinate.Point.Position.Longitude, LocationManager.Instance.MyGeoPosition.Coordinate.Point.Position.Latitude ) );
			request.AddParameter( @"speed", speed.ToString() );
			if( targetTimeSeconds != -1 )
				request.AddParameter( @"duration", ( targetTimeSeconds ).ToString() );
			else
				request.AddParameter( @"distance", ( (int)( (double)targetMiles * 1609.344 ) ).ToString() );
			if( !string.IsNullOrWhiteSpace( poiNames ) )
				request.AddParameter( @"poitypes", poiNames );

			IsBusy = true;

			// execute the request
			client.ExecuteAsync( request, async r =>
			 {
				 string result = r.Content;

				 bool res = await ParseRouteData( result, @"balanced", true );

				 IsBusy = false;
				 tcs1.SetResult( res );
			 } );

			return t1;
		}

		public Task<bool> FindRoute( string routeType, bool newRoute = true )
		{
			BugSense.BugSenseHandler.Instance.LeaveBreadCrumb( string.Format( @"Finding route type {0}. IsNew = {1}", routeType, newRoute ) );

			TaskCompletionSource<bool> tcs1 = new TaskCompletionSource<bool>();
			Task<bool> t1 = tcs1.Task;

			// Clear the cache
			_cachedRouteData = null;

			if( newRoute )
			{
				_journeyMap.Clear();
			}

			if( ( !newRoute && _journeyMap.ContainsKey( routeType ) ) )
			{
				tcs1.SetResult( true );
				return t1;
			}

			string speedSetting = SettingManager.instance.GetStringValue( @"cycleSpeed", DirectionsPage.CycleSpeed[1] );

			int speed = Util.getSpeedFromString( speedSetting );
			const int useDom = 0; // 0=xml 1=gml

			string itinerarypoints = _waypoints.Where( waypoint => waypoint != null ).Aggregate( "", ( current, waypoint ) => current + waypoint.Longitude + @"," + waypoint.Latitude + @"|" );
			itinerarypoints = itinerarypoints.TrimEnd( '|' );

			var client = new RestClient( @"http://www.cyclestreets.net/api" );
			var request = new RestRequest( @"journey.json", Method.GET );
			request.AddParameter( @"key", App.apiKey );
			request.AddParameter( @"plan", routeType );
			if( !newRoute )
			{
				//http://www.cyclestreets.net/api/journey.xml?key=registeredapikey&useDom=1&itinerary=345529&plan=fastest
				request.AddParameter( @"itinerary", Overview.RouteNumber );
			}
			else
			{
				request.AddParameter( @"itinerarypoints", itinerarypoints );
				request.AddParameter( @"speed", speed );
			}


			IsBusy = true;

			// execute the request
			client.ExecuteAsync( request, async r =>
			 {
				 string result = r.Content;

				 bool res = await ParseRouteData( result, routeType, newRoute );

				 IsBusy = false;
				 tcs1.SetResult( res );
			 } );

			return t1;
		}

		public LocationRectangle GetRouteBounds()
		{
			List<GeoCoordinate> allPoints = new List<GeoCoordinate>();

			foreach( var segment in _cachedRouteData )
			{
				allPoints.AddRange( segment.Points );
			}

			return LocationRectangle.CreateBoundingRectangle( allPoints.ToArray() );
		}

		public async Task<bool> ParseRouteData( string currentRouteData, string routeType, bool newRoute )
		{
			if( newRoute )
			{
				_journeyMap.Clear();
				OnPropertyChanged( @"ReadyToDisplayRoute" );
			}

			if( currentRouteData != null )
			{
				try
				{
					_currentParsedRoute = JObject.Parse( currentRouteData );
					if( _currentParsedRoute != null && _currentParsedRoute.HasValues && !_journeyMap.ContainsKey( routeType ) )
					{
						_journeyMap.Add( routeType, currentRouteData );
						OnPropertyChanged( @"ReadyToDisplayRoute" );
					}
				}
				catch( Exception ex )
				{
					AnalyticClient.Error( @"Could not parse JSON " + currentRouteData.Trim() + @" " + ex.Message );
					MessageBox.Show( AppResources.CouldNotParse );
				}
			}

			if( _journeyMap.ContainsKey( routeType ) )
				return true;

			MessageBox.Show( AppResources.NoRouteFoundTryAnotherSearch, AppResources.NoRoute, MessageBoxButton.OK );
			return false;
		}

		internal async Task<IEnumerable<RouteSection>> GetRouteSections( string routeType )
		{
			if( _journeyMap == null || !_journeyMap.ContainsKey( routeType ) )
				return null;

			if( _cachedRouteData != null )
				return _cachedRouteData;

			List<RouteSection> result = new List<RouteSection>();
			bool parseResult = await ParseRouteData( _journeyMap[routeType], routeType, false );
			if( !parseResult )
				return null;
			JObject journeyObject = _currentParsedRoute;
			int lastDistance = 0;
			GeoCoordinate endPoint = null;
			if( journeyObject[@"marker"] is JArray )
			{
				foreach( var marker in journeyObject[@"marker"] )
				{
					var attributes = marker["@attributes"];
					//if (!attributes.Contains("type"))
					//    continue;

					var type = attributes["type"];
					if( type.ToString() == @"route" )
					{
						var section = marker[@"@attributes"];
						if( section == null )
							continue;
						//RouteSection sectionObj = new RouteSection();
						double longitude = section.Value<double>( @"finish_longitude" );
						double latitude = section.Value<double>( @"finish_latitude" );
						endPoint = new GeoCoordinate( latitude, longitude );
						// sectionObj.Points.Add(new Geocoordinate(latitude, longitude));
						//sectionObj.Description = "Start " + section.start;

						Overview = new RouteOverviewObject
						{
							Quietness = section.Value<int>( @"quietness" ),
							RouteNumber = section.Value<int>( @"itinerary" ),
							RouteLength = section.Value<int>( @"length" ),
							SignalledJunctions = section.Value<int>( @"signalledJunctions" ),
							SignalledCrossings = section.Value<int>( @"signalledCrossings" ),
							GrammesCo2Saved = section.Value<int>( @"grammesCO2saved" ),
							calories = section.Value<int>( @"calories" ),
							RouteDuration = section.Value<int>( @"time" )
						};

						//result.Add(sectionObj);
					}
					else if( type.ToString() == @"segment" )
					{
						var section = marker[@"@attributes"];
						if( section == null )
							continue;
						RouteSection sectionObj = result.Count == 0 ? new StartPoint() : new RouteSection();
						string[] points = section[@"points"].ToString().Split( ' ' );
						foreach( string t in points )
						{
							string[] xy = t.Split( ',' );

							double longitude = double.Parse( xy[0] );
							double latitude = double.Parse( xy[1] );
							sectionObj.Points.Add( new GeoCoordinate( latitude, longitude ) );
						}
						string[] temp = section[@"elevations"].ToString().Split( ',' );
						int[] convertedItems = temp.Select( int.Parse ).ToArray();
						sectionObj.Height = new List<int>( convertedItems );
						temp = section[@"distances"].ToString().Split( ',' );
						convertedItems = temp.Select( int.Parse ).ToArray();
						sectionObj.Distances = new List<int>( convertedItems );
						sectionObj.Walking = int.Parse( section[@"walk"].ToString() ) == 1;
						sectionObj.Description = section[@"name"].ToString().Equals( @"lcn?" )
							? AppResources.UnknownStreet
							: section[@"name"].ToString();
						sectionObj.Distance = lastDistance;
						lastDistance = int.Parse( section[@"distance"].ToString() );
						sectionObj.Bearing = double.Parse( section[@"startBearing"].ToString() );
						sectionObj.Time = int.Parse( section[@"time"].ToString() );
						sectionObj.Turn = section[@"turn"].ToString();
						result.Add( sectionObj );
					}
				}
			}
			else
			{
				MessageBox.Show( AppResources.RouteParseError, AppResources.Error, MessageBoxButton.OK );
				return null;
			}

			EndPoint ep = new EndPoint
			{
				Distance = lastDistance,
				Turn = AppResources.straightOn,
			};
			result.Add( ep );

			CurrentRoute = result;
			return result;
		}

		internal string HasCachedRoute( string defaultPlan )
		{
			if( defaultPlan != null && _journeyMap.ContainsKey( defaultPlan ) )
				return defaultPlan;
			return _journeyMap.Count > 0 ? _journeyMap.First().Key : null;
		}

		internal void GenerateDebugData()
		{

		}

		internal Task<bool> RouteTo( double longitude, double latitude, string routeType )
		{
			if( LocationManager.Instance.MyGeoPosition == null )
			{
				Util.showLocationDialog();
				return null;
			}

			GeoCoordinate target = new GeoCoordinate( latitude, longitude );
			_waypoints.Clear();
			AddWaypoint( GeoUtils.ConvertGeocoordinate( LocationManager.Instance.MyGeoPosition.Coordinate ) );
			AddWaypoint( target );
			return FindRoute( routeType );
		}
	}
}
