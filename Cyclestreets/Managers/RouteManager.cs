//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using System.Device.Location;
using System.Threading.Tasks;
using Cyclestreets.Utils;
using CycleStreets.Util;
using RestSharp;
using Cyclestreets.Common;
using Newtonsoft.Json.Linq;
using System.Windows;
using Cyclestreets.Resources;
using Cyclestreets.Pages;
namespace Cyclestreets.Managers
{
	public class RouteManager : BindableBase
	{
		private Stackish<GeoCoordinate> _waypoints = new Stackish<GeoCoordinate>();

		private bool _isBusy = false;
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

		public RouteManager()
		{

		}

		public void AddWaypoint( GeoCoordinate c )
		{
			_waypoints.Push( c );
		}

		public Task<bool> FindRoute()
		{
			TaskCompletionSource<bool> tcs1 = new TaskCompletionSource<bool>();
			Task<bool> t1 = tcs1.Task;

			string plan = SettingManager.instance.GetStringValue( "defaultRouteType", "balanced" );
			plan = plan.Replace( " route", "" );

			string speedSetting = SettingManager.instance.GetStringValue( "cycleSpeed", "12mph" );

			string itinerarypoints = "";
			int speed = Util.getSpeedFromString( speedSetting );
			int useDom = 0; // 0=xml 1=gml

			foreach( var p in _waypoints )
			{
				itinerarypoints = itinerarypoints + p.Longitude + "," + p.Latitude + "|";
			}
			itinerarypoints = itinerarypoints.TrimEnd( '|' );

			var client = new RestClient( "http://www.cyclestreets.net/api" );
			var request = new RestRequest( "journey.json?key={key}", Method.GET );
			request.AddUrlSegment( "key", App.apiKey );
			request.AddParameter( "plan", plan );
			request.AddParameter( "itinerarypoints", itinerarypoints );
			request.AddParameter( "speed", speed );
			request.AddParameter( "useDom", useDom );

			IsBusy = true;

			// execute the request
			RestResponse response = client.ExecuteAsync( request, response =>
			{
				string result = response.Content;

				ParseJSON( result );

				IsBusy = false;
				tcs1.SetResult( true );
			} );

			return t1;
		}

		private bool ParseJSON( string result )
		{
			JObject o = null;
			if( currentRouteData != null )
			{
				try
				{
					o = JObject.Parse( currentRouteData.Trim() );
				}
				catch( Exception ex )
				{
					MarkedUp.AnalyticClient.Error( "Could not parse JSON " + currentRouteData.Trim() + " " + ex.Message );
					MessageBox.Show( "Could not parse route data information from server. Please let us know about this error with the route you were trying to plan" );
				}
			}

			if( o == null || o["marker"] == null )
			{
				MessageBoxResult result = MessageBox.Show( AppResources.NoRouteFoundTryAnotherSearch, AppResources.NoRoute, MessageBoxButton.OK );
				return false;
			}

			route = new RouteDetails();

			JArray steps = o["marker"] as JArray;
			JArray pois = o["poi"] as JArray;
			const string col1 = "#7F000000";
			const string col2 = "#3F000000";
			bool swap = true;
			int totalTime = 0;
			double totalDistanceMetres = 0;
			int totalDistance = 0;
			foreach( var jToken in steps )
			{
				var step = (JObject)jToken;
				JObject p = (JObject)step["@attributes"];
				string markerType = (string)p["type"];
				switch( markerType )
				{
					case "route":
						{
							route.routeIndex = JsonGetPropertyHelper<int>( p, "itinerary" );
							JourneyFactItem i = new JourneyFactItem( "Assets/picture.png" )
							{
								Caption = AppResources.RouteNumber,
								Value = "" + route.routeIndex
							};
							facts.Add( i );

							route.timeInSeconds = JsonGetPropertyHelper<int>( p, "time" );
							i = new JourneyFactItem( "Assets/clock.png" )
							{
								Caption = AppResources.JourneyTime,
								Value = UtilTime.secsToLongDHMS( route.timeInSeconds )
							};
							facts.Add( i );

							route.quietness = JsonGetPropertyHelper<float>( p, "quietness" );
							i = new JourneyFactItem( "Assets/picture.png" )
							{
								Caption = AppResources.Quietness,
								Value = route.quietness + "% " + getQuietnessString( route.quietness )
							};
							facts.Add( i );

							route.signalledJunctions = JsonGetPropertyHelper<int>( p, "signalledJunctions" );
							i = new JourneyFactItem( "Assets/traffic_signals.png" )
							{
								Caption = AppResources.SignaledJunctions,
								Value = "" + route.signalledJunctions
							};
							facts.Add( i );

							route.signalledCrossings = JsonGetPropertyHelper<int>( p, "signalledCrossings" );
							i = new JourneyFactItem( "Assets/traffic_signals.png" )
							{
								Caption = AppResources.SignaledCrossings,
								Value = "" + route.signalledCrossings
							};
							facts.Add( i );

							route.grammesCO2saved = JsonGetPropertyHelper<int>( p, "grammesCO2saved" );
							i = new JourneyFactItem( "Assets/world.png" )
							{
								Caption = AppResources.CO2Avoided,
								Value = (float)route.grammesCO2saved / 1000f + " kg"
							};
							facts.Add( i );

							route.calories = JsonGetPropertyHelper<int>( p, "calories" );
							i = new JourneyFactItem( "Assets/heart.png" )
							{
								Caption = AppResources.Calories,
								Value = route.calories + AppResources.Kcal
							};
							facts.Add( i );
							break;
						}
					case "segment":
						{
							string pointsText = (string)p["points"];
							string[] points = pointsText.Split( ' ' );
							List<GeoCoordinate> coords = new List<GeoCoordinate>();
							foreach( string t in points )
							{
								string[] xy = t.Split( ',' );

								double longitude = double.Parse( xy[0] );
								double latitude = double.Parse( xy[1] );
								coords.Add( new GeoCoordinate( latitude, longitude ) );

								if( max.Latitude > latitude )
									max.Latitude = latitude;
								if( min.Latitude < latitude )
									min.Latitude = latitude;
								if( max.Longitude < longitude )
									max.Longitude = longitude;
								if( min.Longitude > longitude )
									min.Longitude = longitude;
							}
							geometryCoords.Add( coords );

							string elevationsText = (string)p["elevations"];
							string[] elevations = elevationsText.Split( ',' );

							RouteSegment s = new RouteSegment
							{
								Location = coords[0],
								Bearing =
									Geodesy.Bearing( coords[0].Latitude, coords[0].Longitude,
										coords[coords.Count - 1].Latitude, coords[coords.Count - 1].Longitude )
							};
							route.distance += (int)float.Parse( (string)p["distance"] );
							s.DistanceMetres = (int)float.Parse( (string)p["distance"] );
							totalDistanceMetres += s.DistanceMetres;
							totalDistance += route.distance;
							s.TotalDistance = totalDistance;
							s.Name = (string)p["name"];
							s.ProvisionName = (string)p["provisionName"];
							int theLegOfTime = int.Parse( (string)p["time"] );
							s.Time = "" + ( totalTime + theLegOfTime );
							totalTime += theLegOfTime;
							s.Turn = (string)p["turn"];
							s.Walk = ( int.Parse( (string)p["walk"] ) == 1 ? true : false );
							if( elevations.Length >= 1 )
							{
								ElevationPoint dp = new ElevationPoint
								{
									Distance = totalDistanceMetres,
									Height = float.Parse( elevations[0] )
								};
								route.HeightChart.Add( dp );
							}
							geometryDashed.Add( s.Walk );
							geometryColor.Add( s.Walk ? Color.FromArgb( 255, 0, 0, 0 ) : Color.FromArgb( 255, 127, 0, 255 ) );
							s.BgColour = swap ? col1 : col2;
							swap = !swap;
							route.segments.Add( s );
							break;
						}
				}
			}
			if( poiLayer == null )
			{
				poiLayer = new MapLayer();

				MyMap.Layers.Add( poiLayer );
			}
			else
			{
				poiLayer.Clear();
			}
			int id = 0;
			if( pois != null )
			{
				foreach( var jToken in pois )
				{
					var poi = (JObject)jToken;
					JObject p = (JObject)poi["@attributes"];
					POI poiItem = new POI
					{
						Name = (string)p["name"]
					};
					GeoCoordinate g = new GeoCoordinate
					{
						Longitude = float.Parse( (string)p["longitude"] ),
						Latitude = float.Parse( (string)p["latitude"] )
					};
					poiItem.Position = g;

					poiItem.PinID = "" + ( id++ );

					Pushpin pp = new Pushpin
					{
						Content = poiItem.PinID
					};
					pp.Tap += poiTapped;

					pinItems.Add( pp, poiItem );

					MapOverlay overlay = new MapOverlay
					{
						Content = pp
					};
					pp.GeoCoordinate = poiItem.GetGeoCoordinate();
					overlay.GeoCoordinate = poiItem.GetGeoCoordinate();
					overlay.PositionOrigin = new Point( 0, 1.0 );
					poiLayer.Add( overlay );
				}
			}
			JourneyFactItem item = new JourneyFactItem( "Assets/bullet_go.png" )
			{
				Caption = AppResources.Distance
			};
			float dist = route.distance * 0.000621371192f;
			item.Value = dist.ToString( "0.00" ) + AppResources.Miles;
			facts.Add( item );

			SmartDispatcher.BeginInvoke( () =>
			{
				if( geometryCoords.Count == 0 )
				{
					MessageBox.Show( AppResources.CouldNotCalculateRoute );
					App.networkStatus.networkIsBusy = false;
					return;
				}

				try
				{
					LocationRectangle rect = new LocationRectangle( min, max );
					MyMap.SetView( rect );
				}
				catch( Exception )
				{
					System.Diagnostics.Debug.WriteLine( @"Invalid box" );
				}

				//MyMap.Center = new GeoCoordinate(min.Latitude + ((max.Latitude - min.Latitude) / 2f), min.Longitude + ((max.Longitude - min.Longitude) / 2f));
				//MyMap.ZoomLevel = 10;
				int count = geometryCoords.Count;
				for( int i = 0; i < count; i++ )
				{
					List<GeoCoordinate> coords = geometryCoords[i];
					DrawMapMarker( coords.ToArray(), geometryColor[i], geometryDashed[i] );
				}

				//NavigationService.Navigate( new Uri( "/Pages/DirectionsResults.xaml", UriKind.Relative ) );
				var sgs = ExtendedVisualStateManager.GetVisualStateGroups( LayoutRoot );
				var sg = sgs[0] as VisualStateGroup;
				bool res = ExtendedVisualStateManager.GoToElementState( LayoutRoot, "RouteFoundState", true );
				//VisualStateManager.GoToState(this, "RouteFoundState", true);

				App.networkStatus.networkIsBusy = false;

				float f = (float)route.distance * 0.000621371192f;
				findLabel1.Text = f.ToString( "0.00" ) + AppResources.MetresShort + UtilTime.secsToLongDHMS( route.timeInSeconds );

				if( !SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) )
					return;
				bool shownTutorial = SettingManager.instance.GetBoolValue( "shownTutorialRouteType", false );
				if( !shownTutorial )
					routeTutorialRouteType.Visibility = Visibility.Visible;
			} );
		}
	}
}
