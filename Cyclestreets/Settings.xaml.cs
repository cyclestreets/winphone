using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace Cyclestreets
{
	public partial class Settings : PhoneApplicationPage
	{
		public Settings()
		{
			InitializeComponent();

			string defaultRouteTypeSetting = Directions.RouteType[ 0 ];
			if( IsolatedStorageSettings.ApplicationSettings.Contains( "defaultRouteType" ) )
				defaultRouteTypeSetting = (string)IsolatedStorageSettings.ApplicationSettings[ "defaultRouteType" ];

			defaultRouteType.ItemsSource = Directions.RouteType;
			defaultRouteType.SelectedItem = defaultRouteTypeSetting;

			string cycleSpeedSetting = Directions.CycleSpeed[ 0 ];
			if( IsolatedStorageSettings.ApplicationSettings.Contains( "cycleSpeed" ) )
				cycleSpeedSetting = (string)IsolatedStorageSettings.ApplicationSettings[ "cycleSpeed" ];

			cycleSpeed.ItemsSource = Directions.CycleSpeed;
			cycleSpeed.SelectedItem = cycleSpeedSetting;
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
				IsolatedStorageSettings.ApplicationSettings[ "defaultRouteType" ] = plan;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( "defaultRouteType", plan );

			plan = (string)cycleSpeed.SelectedItem;

			if( IsolatedStorageSettings.ApplicationSettings.Contains( "cycleSpeed" ) )
				IsolatedStorageSettings.ApplicationSettings[ "cycleSpeed" ] = plan;
			else
				IsolatedStorageSettings.ApplicationSettings.Add( "cycleSpeed", plan );

			NavigationService.GoBack();
		}
	}
}