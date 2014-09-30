using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Xml.Linq;
using Cyclestreets.Annotations;
using Cyclestreets.Utils;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class POIList
	{
		private readonly ObservableCollection<POIItem> items = new ObservableCollection<POIItem>();

		float longitude;
		float latitude;

		public POIList()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;

			AsyncWebRequest _request = new AsyncWebRequest( string.Format(@"http://www.cyclestreets.net/api/poitypes.xml?key={0}&icons=32", App.apiKey), POIFound );
			_request.Start();

			App.networkStatus.NetworkIsBusy = true;
		}

		private void POIFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();
			string str = enc.GetString( data, 0, data.Length );

			XDocument xml = XDocument.Parse( str.Trim() );

			var poi = xml.Descendants( @"poitype" )
									.Where( e => e.Parent != null && e.Parent.Name.LocalName == @"poitypes" );

			foreach( XElement p in poi )
			{
				POIItem item = new POIItem();
			    var xElement = p.Element( @"name" );
			    if (xElement != null) item.POILabel = xElement.Value;
			    item.POIEnabled = false;
			    var element = p.Element( @"key" );
			    if (element != null) item.POIName = element.Value;

			    items.Add( item );
			}

			poiList.ItemsSource = items;

			App.networkStatus.NetworkIsBusy = false;
		}

		protected override void OnNavigatedTo( System.Windows.Navigation.NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			longitude = float.Parse( NavigationContext.QueryString[ @"longitude" ] );
			latitude = float.Parse( NavigationContext.QueryString[ @"latitude" ] );
		}

		private void CheckBox_Tap_1( object sender, System.Windows.Input.GestureEventArgs e )
		{
		    POIItem item = ( (CheckBox)sender ).DataContext as POIItem;

		    if (item != null)
		        NavigationService.Navigate( new Uri( "/Pages/POIResults.xaml?longitude=" + longitude + "&latitude=" + latitude + "&POIName=" + item.POIName, UriKind.Relative ) );
		}
	}
}