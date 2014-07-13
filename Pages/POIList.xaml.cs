using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Linq;
using Cyclestreets.Utils;
using Microsoft.Phone.Controls;

namespace Cyclestreets
{
	public partial class POIList : PhoneApplicationPage
	{
		private ObservableCollection<POIItem> items = new ObservableCollection<POIItem>();

		float longitude;
		float latitude;

		public POIList()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;

			AsyncWebRequest _request = new AsyncWebRequest( "http://www.cyclestreets.net/api/poitypes.xml?key=" + App.apiKey + "&icons=32", POIFound );
			_request.Start();

			App.networkStatus.networkIsBusy = true;
		}

		private void POIFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			var poi = xml.Descendants( "poitype" )
									.Where( e => (string)e.Parent.Name.LocalName == "poitypes" );

			foreach( XElement p in poi )
			{
				POIItem item = new POIItem();
				item.POILabel = p.Element( "name" ).Value;
				item.POIEnabled = false;
				item.POIName = p.Element( "key" ).Value;

				items.Add( item );
			}

			poiList.ItemsSource = items;

			App.networkStatus.networkIsBusy = false;
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			longitude = float.Parse( NavigationContext.QueryString[ "longitude" ] );
			latitude = float.Parse( NavigationContext.QueryString[ "latitude" ] );
		}

		private void CheckBox_Tap_1( object sender, System.Windows.Input.GestureEventArgs e )
		{
			POIItem item = ( (CheckBox)sender ).DataContext as POIItem;

			NavigationService.Navigate( new Uri( "/Pages/POIResults.xaml?longitude=" + longitude + "&latitude=" + latitude + "&POIName=" + item.POIName, UriKind.Relative ) );
		}
	}
}