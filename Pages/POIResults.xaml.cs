using System;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cyclestreets.Objects;
using Cyclestreets.Resources;

namespace Cyclestreets.Pages
{
    public partial class PoiResults
	{
		GeoCoordinate _center;
		public static ObservableCollection<POI> Pois;

		public PoiResults()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			_center = new GeoCoordinate();
			_center.Longitude = float.Parse( NavigationContext.QueryString[ @"longitude" ] );
			_center.Latitude = float.Parse( NavigationContext.QueryString[ @"latitude" ] );

			string poiName = NavigationContext.QueryString[ @"POIName" ];

			AsyncWebRequest request = new AsyncWebRequest( string.Format(@"http://www.cyclestreets.net/api/pois.xml?key={0}&type={1}&longitude={2}&latitude={3}&radius=25", App.apiKey, poiName, _center.Longitude, _center.Latitude), PoiResultsFound );
			request.Start();

			App.networkStatus.NetworkIsBusy = true;
		}

		private void PoiResultsFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			var poi = xml.Descendants( @"poi" )
									.Where( e => e.Parent != null && e.Parent.Name.LocalName == "pois" );
			Pois = new ObservableCollection<POI>();
			int id = 1;
			const string col1 = "#7F000000";
			const string col2 = "#3F000000";
			bool swap = true;
			foreach( XElement p in poi )
			{
				POI item = new POI();
			    var xElement = p.Element( @"name" );
			    if (xElement != null) item.Name = xElement.Value;
			    GeoCoordinate g = new GeoCoordinate();
			    var element = p.Element( @"longitude" );
			    if (element != null) g.Longitude = float.Parse( element.Value );
			    var xElement1 = p.Element( @"latitude" );
			    if (xElement1 != null) g.Latitude = float.Parse( xElement1.Value );
			    item.Position = g;
				double dist = _center.GetDistanceTo( item.Position ) * 0.000621371192;
				item.Distance = dist.ToString( @"0.00" ) + AppResources.MetresShort;
				item.PinID = "" + ( id++ );
				item.BgColour = swap ? col1 : col2;
				swap = !swap;
				Pois.Add( item );
			}

			poiList.ItemsSource = Pois;

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