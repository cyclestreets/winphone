using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Navigation;
using System.Xml.Linq;
using Cyclestreets;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;

namespace CycleStreets.Util
{
	public static class EnumerableExtensions
	{
		public static ObservableCollection<T> ToObservableCollection<T>( this IEnumerable<T> collection )
		{
			ObservableCollection<T> observableCollection = new ObservableCollection<T>();
			foreach( T item in collection )
			{
				observableCollection.Add( item );
			}

			return observableCollection;
		}
	}

	class Util
	{
		public enum EConnectionType
		{
			EConnected,
			ENoConnection,
		}

		public static string GetHardwareId()
		{
			byte[] uniqueId = (byte[])DeviceExtendedProperties.GetValue( "DeviceUniqueId" );
			return BitConverter.ToString( uniqueId );
		}

		public static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
		{
			// Unix timestamp is seconds past epoch
			System.DateTime dtDateTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
			dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
			return dtDateTime;
		}

		public static EConnectionType getConnectionStatus()
		{
			var _isNetworkAvailable = Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.IsNetworkAvailable;
			//ConnectionProfile profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
			if( !_isNetworkAvailable )
			{
				return EConnectionType.ENoConnection;
			}
			else
			{
				return EConnectionType.EConnected;
			}
		}

		public static bool ResultContainsErrors( string webResponse, string actionPerformed )
		{
			if( webResponse == "" )
				return false;
			try
			{
				XDocument xml = XDocument.Parse( webResponse.Trim() );
				var error = xml.Descendants( "root" );
				foreach( XElement e in error )
				{
					if( e.Element( "error" ) != null )
					{
						int errorCode = Int32.Parse( e.Element( "error_code" ).Value );

						//MarkedUp.AnalyticClient.Error( e.Element( "error" ).Value );

						SmartDispatcher.BeginInvoke( () =>
						{
							MessageBoxResult result = MessageBox.Show( "Server reported an error.\nAction: " + actionPerformed + "\nCode " + errorCode + "\nMessage: " + e.Element( "error" ).Value + ".\nPlease try again later or report to dave@rwscripts.com if it continues. Thanks.\n", "Error", MessageBoxButton.OK );

						} );

						return true;
					}
				}

				return false;
			}
			catch( System.Xml.XmlException ex )
			{
				FlurryWP8SDK.Api.LogError( "Invalid XML: \"" + webResponse + "\"", ex );
				return true;
			}

		}

		internal static void storeFailure()
		{
			SmartDispatcher.BeginInvoke( () =>
			{
				MessageBoxResult result = MessageBox.Show( "There was an error while downloading the available store items. Please try again. If this problem persists, please contact dave@rwscripts.com", "Error", MessageBoxButton.OK );
				if( result == MessageBoxResult.OK )
				{
					// 					Uri nUri = null;
					// 					if( App.UserSession != null )
					// 						nUri = new Uri( string.Format( "/MainPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					else
					// 						nUri = new Uri( string.Format( "/SplashPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					App.RootFrame.Navigate( nUri );
				}
			} );
		}

		internal static void dataFailure()
		{
			SmartDispatcher.BeginInvoke( () =>
			{
				MessageBoxResult result = MessageBox.Show( "There was an error while downloading data from the server. Please try again.", "Error", MessageBoxButton.OK );
				if( result == MessageBoxResult.OK )
				{

				}
			} );
		}

		internal static void networkFailure()
		{
			SmartDispatcher.BeginInvoke( () =>
			{
				MessageBoxResult result = MessageBox.Show( "No Internet connection available. A connection to the Internet is required to use this app. Check flight mode is not enabled and try again.", "No Internet", MessageBoxButton.OK );
				//if( result == MessageBoxResult.OK )
				{
					// 					Uri nUri = null;
					// 					if( App.UserSession != null )
					// 						nUri = new Uri( string.Format( "/MainPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					else
					// 						nUri = new Uri( string.Format( "/SplashPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					App.RootFrame.Navigate( nUri );
				}
			} );
		}

		public static int getSpeedFromString( string speedVal )
		{
			switch( speedVal )
			{
				case "10mph":
					return 16;
				case "12mph":
					return 20;
				case "15mph":
					return 24;
			}
			return 20;
		}

		internal static void showLocationDialog()
		{
			if( SettingManager.instance.GetBoolValue( "LocationConsent", true ) == false )
			{
				MessageBoxResult result =
								MessageBox.Show( "You have denied CycleStreets permission to access your location. Press OK to change this or Cancel to cancel.",
								"Location",
								MessageBoxButton.OKCancel );
				if( result == MessageBoxResult.OK )
				{
					( (PhoneApplicationFrame)Application.Current.RootVisual ).Navigate( new Uri( "/Pages/Settings.xaml", UriKind.Relative ) );
				}
			}
			else
			{
				MessageBoxResult result =
									MessageBox.Show( "Unable to retrieve your location. Please check location services are enabled on this device.",
									"Location",
									MessageBoxButton.OK );
			}
		}
	}
}
