using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace Cyclestreets
{
	public partial class Settings : PhoneApplicationPage
	{
		public Settings()
		{
			InitializeComponent();

			string defaultRouteTypeSetting = Directions.RouteType[0];
			if( IsolatedStorageSettings.ApplicationSettings.Contains( "defaultRouteType" ) )
				defaultRouteTypeSetting = (string)IsolatedStorageSettings.ApplicationSettings["defaultRouteType"];

			defaultRouteType.ItemsSource = Directions.RouteType;
			defaultRouteType.SelectedItem = defaultRouteTypeSetting;

			string cycleSpeedSetting = Directions.CycleSpeed[0];
			if( IsolatedStorageSettings.ApplicationSettings.Contains( "cycleSpeed" ) )
				cycleSpeedSetting = (string)IsolatedStorageSettings.ApplicationSettings["cycleSpeed"];

			cycleSpeed.ItemsSource = Directions.CycleSpeed;
			cycleSpeed.SelectedItem = cycleSpeedSetting;

			string locationEnabledSetting = Directions.EnabledDisabled[0];
			if( IsolatedStorageSettings.ApplicationSettings.Contains( "LocationConsent" ) )
			{
				if( (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == false )
					locationEnabledSetting = Directions.EnabledDisabled[1];
			}


			locationEnabled.ItemsSource = Directions.EnabledDisabled;
			locationEnabled.SelectedItem = locationEnabledSetting;
		}

		private void defaultRouteType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{

		}

		private void cycleSpeed_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{

		}

		private void Button_Click( object sender, RoutedEventArgs e )
		{
			string plan = (string)defaultRouteType.SelectedItem;

			if( IsolatedStorageSettings.ApplicationSettings.Contains( "defaultRouteType" ) )
				IsolatedStorageSettings.ApplicationSettings["defaultRouteType"] = plan;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( "defaultRouteType", plan );

			plan = (string)cycleSpeed.SelectedItem;

			if( IsolatedStorageSettings.ApplicationSettings.Contains( "cycleSpeed" ) )
				IsolatedStorageSettings.ApplicationSettings["cycleSpeed"] = plan;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( "cycleSpeed", plan );

			NavigationService.GoBack();
		}

		private void Hyperlink_Click( object sender, RoutedEventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://www.cyclestreets.net/" );
			url.Show();
		}

		private void Hyperlink_Click_1( object sender, RoutedEventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://www.rwscripts.com/" );
			url.Show();
		}

		private void Hyperlink_Click_2( object sender, RoutedEventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask();
			url.Uri = new System.Uri( "http://forum.rwscripts.com/" );
			url.Show();
		}
	}
}