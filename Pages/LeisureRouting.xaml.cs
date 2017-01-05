using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Linq;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using CycleStreets.Util;
using Microsoft.Phone.Controls;
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

			AsyncWebRequest _request = new AsyncWebRequest( string.Format( @"http://www.cyclestreets.net/api/poitypes.xml?key={0}&icons=32", App.apiKey ), POIFound );
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
									.Where( e => (string)e.Parent.Name.LocalName == @"poitypes" );

			foreach( XElement p in poi )
			{
				POIItem item = new POIItem();
				item.POILabel = p.Element( @"name" ).Value;
				item.POIEnabled = false;
				item.POIName = p.Element( @"key" ).Value;

				items.Add( item );
			}

			poiList.ItemsSource = items;

			App.networkStatus.NetworkIsBusy = false;
		}

		private void btn_cancel_Click( object sender, RoutedEventArgs e )
		{
			pleaseWait.IsOpen = false;
			App.networkStatus.NetworkIsBusy = false;
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

		private void findRoute_Click( object sender, EventArgs e )
		{
			Focus();

			if( LocationManager.Instance.MyGeoPosition != null )
			{

				string extra;
				if( ( (ListBoxItem)routeType.SelectedItem ).Content.Equals( AppResources.TargetTime ) )
				{
					int val = 0;
					int.TryParse( valueEntry.Text, out val );
					if( val > 0 )
						extra = @"&duration=" + val;
					else
					{
						MessageBoxResult result =
						MessageBox.Show( "Invalid duration. Value needs to be greater than zero", "Invalid Data",
						MessageBoxButton.OK );
						return;
					}
				}
				else
				{
					int val = 0;
					int.TryParse( valueEntry.Text, out val );

					if( val > 0 )
						extra = @"&distance=" + val;
					else
					{
						MessageBoxResult result =
						MessageBox.Show( "Invalid distance. Value needs to be greater than zero", "Invalid Data",
						MessageBoxButton.OK );
						return;
					}
				}
				string poiNames = "";
				foreach( POIItem item in items )
				{
					if( item.POIEnabled )
						poiNames += item.POILabel + @",";
				}
				poiNames = HttpUtility.UrlEncode( poiNames.TrimEnd( ',' ) );

				if( !string.IsNullOrWhiteSpace( poiNames ) )
					extra += @"&poitypes=" + poiNames;

				NavigationService.Navigate( new Uri( "/Pages/RouteOverview.xaml?mode=leisure" + extra, UriKind.Relative ) );
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
				string selection = ( (ListBoxItem)e.AddedItems[0] ).Content.ToString();
				if( selection.Equals( AppResources.TargetTime ) )
				{
					valueDescription.Text = AppResources.EnterTime;
				}
				else
				{
					valueDescription.Text = AppResources.EnterDistance;
				}
			}
		}
	}
}