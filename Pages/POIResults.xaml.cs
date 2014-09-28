using System;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cyclestreets.Objects;
using Cyclestreets.Resources;
using Newtonsoft.Json.Linq;
using MarkedUp;
using System.Windows;

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

			AsyncWebRequest request = new AsyncWebRequest( string.Format(@"http://www.cyclestreets.net/api/pois.json?key={0}&type={1}&longitude={2}&latitude={3}&radius=25", App.apiKey, poiName, _center.Longitude, _center.Latitude), PoiResultsFound );
			request.Start();

			App.networkStatus.NetworkIsBusy = true;
		}

		private void PoiResultsFound( byte[] data )
		{
			if( data == null )
				return;


			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

            JObject o = null;
            try
            {
                o = JObject.Parse(str);
            }
            catch (Exception ex)
            {
                AnalyticClient.Error(@"Could not parse JSON " + str + @" " + ex.Message);
                MessageBox.Show(AppResources.poiParseError);
            }

            if (o == null) return;
		    JArray results = null;
		    if (o[@"pois"][@"poi"] is JArray)
		        results = o[@"pois"][@"poi"] as JArray;
		    else
		    {
		        if (o[@"pois"][@"poi"] is JObject)
		        {
		            results = new JArray {o[@"pois"][@"poi"] as JObject};
		        }
		    }
		    Pois = new ObservableCollection<POI>();
            int id = 1;
            const string col1 = @"#7F000000";
            const string col2 = @"#3F000000";
            bool swap = true;
		    if (results == null )
		    {
		        no_poi.Visibility = Visibility.Visible;
                App.networkStatus.NetworkIsBusy = false;
		        return;
		    }
            foreach( var poi in results )
            {
				POI item = new POI {Name = poi[@"name"].ToString()};
                GeoCoordinate g = new GeoCoordinate();
                g.Longitude = float.Parse(poi[@"longitude"].ToString());
                g.Latitude = float.Parse(poi[@"latitude"].ToString());
			    item.Position = g;
				double dist = _center.GetDistanceTo( item.Position ) * 0.000621371192;
				item.Distance = String.Format(AppResources.MetresShort,dist.ToString( @"0.00" ));
				item.PinID = "" + ( id++ );
				item.BgColour = swap ? col1 : col2;
				swap = !swap;
				Pois.Add( item );
			}

			poiList.ItemsSource = Pois;
            App.networkStatus.NetworkIsBusy = false;
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