using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets
{
	public class POI
	{
		private ReverseGeocodeQuery geoQ = null;

		public string Name { get; set; }
		private GeoCoordinate _position;
		public GeoCoordinate Position 
		{
			get { return _position; }
			set 
			{
				geoQ = new ReverseGeocodeQuery();
				geoQ.QueryCompleted += geoQ_QueryCompleted;
				geoQ.GeoCoordinate = value;
				geoQ.QueryAsync();

				_position = value; 
			}
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<System.Collections.Generic.IList<MapLocation>> e )
		{
			MapLocation loc = e.Result[ 0 ];
			Location = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
		}

		public string Distance { get; set; }
		public string Location { get; set; }
	}

	public partial class POIResults : PhoneApplicationPage
	{
		GeoCoordinate center;
		ObservableCollection<POI> pois;

		public POIResults()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			center = new GeoCoordinate();
			center.Longitude = float.Parse( NavigationContext.QueryString[ "longitude" ] );
			center.Latitude = float.Parse( NavigationContext.QueryString[ "latitude" ] );

			string poiName = NavigationContext.QueryString[ "POIName" ];

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/pois.xml?key=" + MainPage.apiKey + "&type=" + poiName + "&longitude=" + center.Longitude + "&latitude=" + center.Latitude + "&radius=5", POIResultsFound );
			_request.Start();
		}

		private void POIResultsFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			var poi = xml.Descendants( "poi" )
									.Where( e => (string)e.Parent.Name.LocalName == "pois" );
			pois = new ObservableCollection<POI>();
			foreach( XElement p in poi )
			{
				POI item = new POI();
				item.Name = p.Element( "name" ).Value;
				GeoCoordinate g = new GeoCoordinate();
				g.Longitude = float.Parse( p.Element( "longitude" ).Value );
				g.Latitude = float.Parse( p.Element( "latitude" ).Value );
				item.Position = g;
				double dist = center.GetDistanceTo( item.Position ) * 0.000621371192;
				item.Distance =  dist.ToString( "0.00" ) + "m";

				pois.Add( item );
			}

			poiList.ItemsSource = pois;
		}
	}
}