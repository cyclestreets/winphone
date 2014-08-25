using System;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cyclestreets.Common;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets
{
	public class POI : BindableBase, ReverseGeocodeHandler
	{
		public string Name { get; set; }

		GeoCoordinate _position;
		public GeoCoordinate Position
		{
			get { return _position; }
			set
			{
				_position = value;

				ReverseGeocodeQueryManager.Instance.Add( this );
			}
		}

		public GeoCoordinate GetGeoCoordinate()
		{
			return _position;
		}

		public void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<System.Collections.Generic.IList<MapLocation>> e )
		{
			MapLocation loc = e.Result[ 0 ];
			Location = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
		}

		public string Distance { get; set; }
		private string _location = "...";
		public string Location
		{
			get
			{ return _location; }

			set
			{
				this.SetProperty( ref this._location, value.TrimStart( new char[] { ',', ' ' } ) );
			}
		}

		public string PinID { get; set; }

		public string BGColour { get; set; }
	}

	public partial class POIResults : PhoneApplicationPage
	{
		GeoCoordinate center;
		public static ObservableCollection<POI> pois = null;

		public POIResults()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			center = new GeoCoordinate();
			center.Longitude = float.Parse( NavigationContext.QueryString[ "longitude" ] );
			center.Latitude = float.Parse( NavigationContext.QueryString[ "latitude" ] );

			string poiName = NavigationContext.QueryString[ "POIName" ];

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/pois.xml?key=" + App.apiKey + "&type=" + poiName + "&longitude=" + center.Longitude + "&latitude=" + center.Latitude + "&radius=25", POIResultsFound );
			_request.Start();

			App.networkStatus.NetworkIsBusy = true;
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
			int id = 1;
			string col1 = "#7F000000";
			string col2 = "#3F000000";
			bool swap = true;
			foreach( XElement p in poi )
			{
				POI item = new POI();
				item.Name = p.Element( "name" ).Value;
				GeoCoordinate g = new GeoCoordinate();
				g.Longitude = float.Parse( p.Element( "longitude" ).Value );
				g.Latitude = float.Parse( p.Element( "latitude" ).Value );
				item.Position = g;
				double dist = center.GetDistanceTo( item.Position ) * 0.000621371192;
				item.Distance = dist.ToString( "0.00" ) + "m";
				item.PinID = "" + ( id++ );
				if( swap )
					item.BGColour = col1;
				else
					item.BGColour = col2;
				swap = !swap;
				pois.Add( item );
			}

			if( pois.Count == 0 )
			{
				POI item = new POI();
				item.Name = "No results in nearby area";
			}
			poiList.ItemsSource = pois;

			App.networkStatus.NetworkIsBusy = true;
		}

		private void poiList_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			NavigationService.RemoveBackEntry();
			NavigationService.RemoveBackEntry();
			POI p = (POI)e.AddedItems[ 0 ];
			NavigationService.Navigate( new Uri( "/Pages/MainPage.xaml?longitude=" + p.Position.Longitude + "&latitude=" + p.Position.Latitude, UriKind.Relative ) );
		}
	}
}