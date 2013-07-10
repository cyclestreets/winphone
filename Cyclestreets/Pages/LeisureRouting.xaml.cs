using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Linq;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;

namespace Cyclestreets.Pages
{
	public partial class LeisureRouting : PhoneApplicationPage
	{
		private ObservableCollection<POIItem> items = new ObservableCollection<POIItem>();
		AsyncWebRequest _request;

		public LeisureRouting()
		{
			InitializeComponent();

			progress.DataContext = App.networkStatus;
			routeType.SelectionChanged += routeType_SelectionChanged;

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

		private void RouteFound( byte[] data )
		{
			if( data == null )
				return;

			UTF8Encoding enc = new UTF8Encoding();

			PhoneApplicationService.Current.State[ "loadedRoute" ] = enc.GetString( data, 0, data.Length );
			NavigationService.Navigate( new Uri( "/Pages/Directions.xaml?plan=leisure", UriKind.Relative ) );

			App.networkStatus.networkIsBusy = false;
		}

		private void btn_cancel_Click( object sender, RoutedEventArgs e )
		{
			pleaseWait.IsOpen = false;
			App.networkStatus.networkIsBusy = false;
			if( _request != null )
			{
				_request.Stop();
			}
		}

		private void PhoneApplicationPage_BackKeyPress( object sender, System.ComponentModel.CancelEventArgs e )
		{
			if( pleaseWait.IsOpen )
				pleaseWait.IsOpen = false;
			else
				base.OnBackKeyPress( e );
		}

		private void Panorama_Loaded( object sender, RoutedEventArgs e )
		{

		}

		private void findRoute_Click( object sender, EventArgs e )
		{
			this.Focus();

			if( LocationManager.instance.MyGeoPosition != null )
			{
				App.networkStatus.networkIsBusy = true;

				Geoposition coord = LocationManager.instance.MyGeoPosition;

				string extra;
				if( ( (ListBoxItem)routeType.SelectedItem ).Content.Equals( "Target Time" ) )
				{
					int val = 0;
					int.TryParse( valueEntry.Text, out val );

					extra = "&duration=" + ( val * 60 );
				}
				else
				{
					int val = 0;
					int.TryParse( valueEntry.Text, out val );

					extra = "&distance=" + (int)( val * 1609.344 );
				}
				string poiNames = "";
				foreach( POIItem item in items )
				{
					if( item.POIEnabled )
						poiNames += item.POILabel + ",";
				}
				poiNames = HttpUtility.UrlEncode( poiNames.TrimEnd( ',' ) );

				string speedSetting = SettingManager.instance.GetStringValue( "cycleSpeed", "12mph" );
				int speed = Util.getSpeedFromString( speedSetting );

				if( !string.IsNullOrWhiteSpace( poiNames ) )
					extra += "&poitypes=" + poiNames;

				_request = new AsyncWebRequest( "http://www.cyclestreets.net/api/journey.json?key=" + App.apiKey + "&plan=leisure&itinerarypoints=" + coord.Coordinate.Longitude + "," + coord.Coordinate.Latitude + "&speed=" + speed + extra, RouteFound );
				_request.Start();

				pleaseWait.IsOpen = true;
				popupPanel.Width = Application.Current.Host.Content.ActualWidth;
			}
			else
			{
				Util.showLocationDialog();
			}
		}

		private void routeType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				string selection = ( (ListBoxItem)e.AddedItems[ 0 ] ).Content.ToString();
				if( selection.Equals( "Target Time" ) )
				{
					valueDescription.Text = "Enter time in minutes";
				}
				else
				{
					valueDescription.Text = "Enter distance in miles";
				}
			}
		}
	}
}