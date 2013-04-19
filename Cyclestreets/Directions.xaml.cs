using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace Cyclestreets
{
	public class RouteSegment
	{
		private int _time;
		public string Time
		{
			get
			{
				TimeSpan t = TimeSpan.FromSeconds(_time);

				string answer = string.Format("{0:D2}:{2:D2}",
											t.TotalMinutes,
											t.Seconds);
				return answer;
			}
			set
			{
				_time = int.Parse(value);
			}
		}
		private int _distance;
		public float Distance
		{
			get
			{
				return _distance * 0.000621371192f;
			}
			set
			{
				_distance = (int)value;
			}
		}
		public string Turn { get; set; }
		public bool Walk { get; set; }
		public string ProvisionName { get; set; }
		public string Name { get; set; }
	}

	public class RouteDetails
	{
		public int timeInSeconds { get; set; }

		public float quietness { get; set; }

		public int signalledJunctions { get; set; }

		public int signalledCrossings { get; set; }

		public int grammesCO2saved { get; set; }

		public int calories { get; set; }

		public List<RouteSegment> segments = new List<RouteSegment>();

		public int distance { get; set; }
	}

	public partial class Directions : PhoneApplicationPage
	{
		ReverseGeocodeQuery geoQ = null;
		//List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
		MapLayer wayPointLayer = null;
		Stack<Pushpin> waypoints = new Stack<Pushpin>();

		List<List<GeoCoordinate>> geometryCoords = new List<List<GeoCoordinate>>();
		List<Color> geometryColor = new List<Color>();

		private GeoCoordinate max = new GeoCoordinate(90, -180);
		private GeoCoordinate min = new GeoCoordinate(-90, 180);

		public static RouteDetails route = new RouteDetails();

		GeoCoordinate current = null;

		public Directions()
		{
			InitializeComponent();

			// hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
			myPosition = ApplicationBar.Buttons[0] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			cursorPos = ApplicationBar.Buttons[1] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			confirmWaypoint = ApplicationBar.Buttons[2] as Microsoft.Phone.Shell.ApplicationBarIconButton;
			findRoute = ApplicationBar.Buttons[3] as Microsoft.Phone.Shell.ApplicationBarIconButton;

			geoQ = new ReverseGeocodeQuery();
			geoQ.QueryCompleted += geoQ_QueryCompleted;

			findRoute.IsEnabled = false;
			clearCurrentPosition();

			startPoint.Populating += (s, args) =>
			{
				args.Cancel = true;
				WebClient wc = new WebClient();
				string prefix = HttpUtility.UrlEncode(args.Parameter);

				string myLocation = "";
				LocationRectangle rect = GetMapBounds();
				//myLocation = "&w=" + rect.West + "&s=" + rect.South + "&e=" + rect.East + "&n=" + rect.North + "&zoom=" + MyMap.ZoomLevel;
				myLocation = MyMap.Center.Latitude + "," + MyMap.Center.Longitude;
				myLocation = HttpUtility.UrlEncode(myLocation);

				//Uri service = new Uri("http://cambridge.cyclestreets.net/api/geocoder.xml?key=" + MainPage.apiKey + myLocation + "&street=" + prefix);
				Uri service = new Uri( "http://demo.places.nlp.nokia.com/places/v1/suggest?at=" + myLocation + "&q=" + prefix + "&app_id=" + MainPage.hereAppID + "&app_code=" + MainPage.hereAppToken + "&accept=application/json" );
				wc.DownloadStringCompleted += DownloadStringCompleted;
				wc.DownloadStringAsync(service, s);
			};
		}

		private void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			Console.WriteLine( e.Result );
			/*AutoCompleteBox acb = e.UserState as AutoCompleteBox;
			if (acb != null && e.Error == null && !e.Cancelled && !string.IsNullOrEmpty(e.Result))
			{
				List<SearchResult> suggestions = new List<SearchResult>();

				XDocument xml = XDocument.Parse(e.Result.Trim());

				var results = xml.Descendants("result")
										.Where(ev => (string)ev.Parent.Name.LocalName == "results");

				foreach (XElement p in results)
				{
					SearchResult result = new SearchResult();
					result.longitude = float.Parse(p.Element("longitude").Value);
					result.latitude = float.Parse(p.Element("latitude").Value);
					result.name = p.Element("name").Value;
					result.near = p.Element("near").Value;
					suggestions.Add(result);
				}

				if (suggestions.Count > 0)
				{
					acb.ItemsSource = suggestions;
					acb.PopulateComplete();
				}
			}*/
		}

		private LocationRectangle GetMapBounds()
		{
			GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
			GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate(new Point(MyMap.ActualWidth, MyMap.ActualHeight));

			return LocationRectangle.CreateBoundingRectangle(new[] { topLeft, bottomRight });
		}

		private void geoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
		{
			MapLocation loc = e.Result[0];
			startPoint.Text = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
		}

		private void startPoint_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			SearchResult start = startPoint.SelectedItem as SearchResult;
			if (start != null)
			{
				if (!geoQ.IsBusy)
				{
					geoQ.GeoCoordinate = new GeoCoordinate(start.latitude, start.longitude);
					geoQ.QueryAsync();

					setCurrentPosition(geoQ.GeoCoordinate);

					SmartDispatcher.BeginInvoke(() =>
					{
						MyMap.SetView(geoQ.GeoCoordinate, 16);
						//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
					});
				}
			}
		}

		private void myPosition_Click(object sender, EventArgs e)
		{
			if (!geoQ.IsBusy)
			{
				geoQ.GeoCoordinate = CoordinateConverter.ConvertGeocoordinate(MainPage.MyGeoPosition.Coordinate);
				geoQ.QueryAsync();

				setCurrentPosition(geoQ.GeoCoordinate);

				SmartDispatcher.BeginInvoke(() =>
				{
					MyMap.SetView(geoQ.GeoCoordinate, 16);
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				});
			}
		}

		private void cursorPos_Click(object sender, EventArgs e)
		{
			if (!geoQ.IsBusy)
			{
				geoQ.GeoCoordinate = MyMap.Center;
				geoQ.QueryAsync();

				setCurrentPosition(geoQ.GeoCoordinate);

				SmartDispatcher.BeginInvoke(() =>
				{
					MyMap.SetView(geoQ.GeoCoordinate, 16);
					//MyMap.Center = CoordinateConverter.ConvertGeocoordinate(MyGeoPosition.Coordinate);
				});
			}
		}

		private void confirmWaypoint_Click(object sender, EventArgs e)
		{
			if (wayPointLayer == null)
			{
				wayPointLayer = new MapLayer();

				MyMap.Layers.Add(wayPointLayer);
			}

			// Change last push pin from finish to intermediate
			if (waypoints.Count > 1)
			{
				Pushpin last = waypoints.Peek();
				last.Style = Resources["Intermediate"] as Style;
			}

			Pushpin pp = new Pushpin();
			if (waypoints.Count == 0)
				pp.Style = Resources["Start"] as Style;
			else
				pp.Style = Resources["Finish"] as Style;

			MapOverlay overlay = new MapOverlay();
			overlay.Content = pp;
			pp.GeoCoordinate = current;
			overlay.GeoCoordinate = current;
			overlay.PositionOrigin = new Point(0.3, 1.0);
			wayPointLayer.Add(overlay);

			addWaypoint(pp);

			clearCurrentPosition();
		}

		private void addWaypoint(Pushpin pp)
		{
			waypoints.Push(pp);
			if (waypoints.Count >= 2)
				findRoute.IsEnabled = true;
			else
				findRoute.IsEnabled = false;
		}

		private void clearCurrentPosition()
		{
			current = null;
			confirmWaypoint.IsEnabled = false;
		}

		private void setCurrentPosition(GeoCoordinate c)
		{
			current = c;
			if (c != null)
				confirmWaypoint.IsEnabled = true;
		}

		private void findRoute_Click(object sender, EventArgs e)
		{
			string plan = "balanced";
			string itinerarypoints = "";// = "-1.2487100362777,53.00143068427369,NG16+1HH|-1.1430546045303,52.95200365149319,NG1+1LL";
			int speed = 20;		//16 = 10mph 20 = 12mph 24 = 15mph
			int useDom = 0;		// 0=xml 1=gml

			foreach (Pushpin p in waypoints)
			{
				itinerarypoints += p.GeoCoordinate.Longitude + "," + p.GeoCoordinate.Latitude + "|";
			}
			itinerarypoints = itinerarypoints.TrimEnd('|');

			AsyncWebRequest _request = new AsyncWebRequest("http://www.cyclestreets.net/api/journey.xml?key=" + MainPage.apiKey + "&plan=" + plan + "&itinerarypoints=" + itinerarypoints + "&speed=" + speed + "&useDom=" + useDom, RouteFound);
			_request.Start();
		}

		private void RouteFound(byte[] data)
		{
			if (data == null)
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString(data, 0, data.Length);

			XDocument xml = XDocument.Parse(str.Trim());

			/*var fixtures = xml.Descendants( "waypoint" )
									.Where( e => (string)e.Parent.Name.LocalName == "markers" );

			foreach( XElement p in fixtures )
			{
				float longitude = float.Parse( p.Attribute( "longitude" ).Value );
				float latitude = float.Parse( p.Attribute( "latitude" ).Value );

				MyCoordinates.Add( new GeoCoordinate( latitude, longitude ) );
			}*/

			// 			MyQuery = new RouteQuery();
			// 			MyQuery.Waypoints = MyCoordinates;
			// 			MyQuery.TravelMode = TravelMode.Walking;
			// 			MyQuery.QueryCompleted += MyQuery_QueryCompleted;
			// 			MyQuery.QueryAsync();

			route = new RouteDetails();

			List<RouteManeuver> manouvers = new List<RouteManeuver>();
			var steps = xml.Descendants("marker")
									.Where(e => (string)e.Parent.Name.LocalName == "markers");

			foreach (XElement p in steps)
			{
				string markerType = p.Attribute("type").Value;
				if (markerType == "route")
				{
					route.timeInSeconds = int.Parse(p.Attribute("time").Value);
					route.quietness = float.Parse(p.Attribute("quietness").Value);
					route.signalledJunctions = int.Parse(p.Attribute("signalledJunctions").Value);
					route.signalledCrossings = int.Parse(p.Attribute("signalledCrossings").Value);
					route.grammesCO2saved = int.Parse(p.Attribute("grammesCO2saved").Value);
					route.calories = int.Parse(p.Attribute("calories").Value);
				}
				else if (markerType == "segment")
				{
					string pointsText = p.Attribute("points").Value;
					string[] points = pointsText.Split(' ');
					List<GeoCoordinate> coords = new List<GeoCoordinate>();
					for (int i = 0; i < points.Length; i++)
					{
						string[] xy = points[i].Split(',');

						double longitude = double.Parse(xy[0]);
						double latitude = double.Parse(xy[1]);
						coords.Add(new GeoCoordinate(latitude, longitude));

						if (max.Latitude > latitude)
							max.Latitude = latitude;
						if (min.Latitude < latitude)
							min.Latitude = latitude;
						if (max.Longitude < longitude)
							max.Longitude = longitude;
						if (min.Longitude > longitude)
							min.Longitude = longitude;
					}
					geometryCoords.Add(coords);
					geometryColor.Add(ConvertHexStringToColour(p.Attribute("color").Value));

					RouteSegment s = new RouteSegment();
					s.Distance = float.Parse(p.Attribute("distance").Value);
					route.distance += (int)float.Parse(p.Attribute("distance").Value);
					s.Name = p.Attribute("name").Value;
					s.ProvisionName = p.Attribute("provisionName").Value;
					s.Time = p.Attribute("time").Value;
					s.Turn = p.Attribute("turn").Value;
					s.Walk = (int.Parse(p.Attribute("walk").Value) == 1 ? true : false);

					route.segments.Add(s);
				}
			}

			SmartDispatcher.BeginInvoke(() =>
			{
				LocationRectangle rect = new LocationRectangle(min, max);
				MyMap.SetView(rect);
				//MyMap.Center = new GeoCoordinate(min.Latitude + ((max.Latitude - min.Latitude) / 2f), min.Longitude + ((max.Longitude - min.Longitude) / 2f));
				//MyMap.ZoomLevel = 10;
				int count = geometryCoords.Count;
				for (int i = 0; i < count; i++)
				{
					List<GeoCoordinate> coords = geometryCoords[i];
					DrawMapMarker(coords.ToArray(), geometryColor[i]);
				}

				NavigationService.Navigate(new Uri("/DirectionsResults.xaml", UriKind.Relative));
			});
		}

		private void DrawMapMarker(GeoCoordinate[] coordinate, Color color)
		{
			// Create a map marker
			MapPolyline polygon = new MapPolyline();
			polygon.StrokeColor = color;
			polygon.StrokeThickness = 3;
			polygon.Path = new GeoCoordinateCollection();
			for (int i = 0; i < coordinate.Length; i++)
			{
				//Point p = MyMap.ConvertGeoCoordinateToViewportPoint( coordinate[i] );
				polygon.Path.Add(coordinate[i]);
			}

			MyMap.MapElements.Add(polygon);
		}

		private Color ConvertHexStringToColour(string hexString)
		{
			byte a = 0;
			byte r = 0;
			byte g = 0;
			byte b = 0;
			if (hexString.StartsWith("#"))
			{
				hexString = hexString.Substring(1, 6);
			}
			//a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
			//	System.Globalization.NumberStyles.AllowHexSpecifier));
			r = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2),
				System.Globalization.NumberStyles.AllowHexSpecifier));
			g = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2),
				System.Globalization.NumberStyles.AllowHexSpecifier));
			b = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
			return Color.FromArgb(255, r, g, b);
		}
	}
}