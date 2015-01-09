using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Command;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;

namespace Cyclestreets.Utils
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

// 	public static class ReverseGeocodingExtensions
// 	{
// 		public static Task<T> ExecuteAsync<T>( this Query<T> query )
// 		{
// 			var taskSource = new TaskCompletionSource<T>();
// 
// 			EventHandler<QueryCompletedEventArgs<T>> handler = null;
// 
// 			handler = ( s, e ) =>
// 			{
// 				query.QueryCompleted -= handler;
// 
// 				if( e.Cancelled )
// 					taskSource.SetCanceled();
// 				else if( e.Error != null )
// 					taskSource.SetException( e.Error );
// 				else
// 					taskSource.SetResult( e.Result );
// 			};
// 
// 			query.QueryCompleted += handler;
// 
// 			query.QueryAsync();
// 
// 			return taskSource.Task;
// 		}
// 	}

	class Util
	{
		public enum EConnectionType
		{
			EConnected,
			ENoConnection,
		}

		public static string GetHardwareId()
		{
			byte[] uniqueId = (byte[])DeviceExtendedProperties.GetValue( @"DeviceUniqueId" );
			return BitConverter.ToString( uniqueId );
		}

		public static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
		{
			// Unix timestamp is seconds past epoch
			DateTime dtDateTime = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
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
		    return EConnectionType.EConnected;
		}

		public static bool ResultContainsErrors( string webResponse, string actionPerformed )
		{
			if( webResponse == "" )
				return false;
			try
			{
				XDocument xml = XDocument.Parse( webResponse.Trim() );
				var error = xml.Descendants( @"root" );
				foreach( XElement e in error )
				{
					if( e.Element( @"error" ) != null )
					{
					    var xElement = e.Element( @"error_code" );
					    if (xElement != null)
					    {
					        int errorCode = Int32.Parse( xElement.Value );

					        //AnalyticClient.Error( e.Element( "error" ).Value );

					        SmartDispatcher.BeginInvoke( () =>
					        {
					            MessageBox.Show( string.Format(AppResources.Util_ResultContainsErrors_, actionPerformed, errorCode, e.Element( @"error" ).Value), AppResources.Error, MessageBoxButton.OK );

					        } );
					    }

					    return true;
					}
				}

				return false;
			}
			catch( System.Xml.XmlException ex )
			{
				FlurryWP8SDK.Api.LogError( string.Format(@"Invalid XML: '{0}'", webResponse), ex );
				return true;
			}

		}

		internal static void storeFailure()
		{
			SmartDispatcher.BeginInvoke( () =>
			{
				MessageBoxResult result = MessageBox.Show( AppResources.StoreErrorMsg, AppResources.Error, MessageBoxButton.OK );
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
				MessageBoxResult result = MessageBox.Show( AppResources.DataDownloadError, AppResources.Error, MessageBoxButton.OK );
				if( result == MessageBoxResult.OK )
				{

				}
			} );
		}

		internal static void networkFailure()
		{
			SmartDispatcher.BeginInvoke( () =>
			{
			    MessageBox.Show( AppResources.NoNetConnection, AppResources.NoInternetTitle, MessageBoxButton.OK );
			    //if( result == MessageBoxResult.OK )
				{
					// 					Uri nUri = null;
					// 					if( App.UserSession != null )
					// 						nUri = new Uri( string.Format( "/MainPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					else
					// 						nUri = new Uri( string.Format( "/SplashPage.xaml?Refresh=true&random={0}", Guid.NewGuid() ), UriKind.Relative );
					// 					App.RootFrame.Navigate( nUri );
				}
			});
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
			if( SettingManager.instance.GetBoolValue( @"LocationConsent", true ) == false )
			{
				MessageBoxResult result =
								MessageBox.Show( AppResources.LocationPermsBody,
								AppResources.Location,
								MessageBoxButton.OKCancel );
				if( result == MessageBoxResult.OK )
				{
					( (PhoneApplicationFrame)Application.Current.RootVisual ).Navigate( new Uri( "/Pages/Settings.xaml", UriKind.Relative ) );
				}
			}
			else
			{
				MessageBox.Show( AppResources.CantFindYou,
				    AppResources.Location,
				    MessageBoxButton.OK );
			}
		}

        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Converter<TInput, TOutput> converter)
        {
            if (array == null)
                throw new ArgumentException();

            return (from item in array select converter(item)).ToArray();
        }
	}
}
