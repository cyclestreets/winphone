using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets
{
	public partial class Settings : PhoneApplicationPage
	{
		public Settings()
		{
			InitializeComponent();

			string defaultRouteTypeSetting = SettingManager.instance.GetStringValue( "defaultRouteType", Directions.RouteType[ 0 ] );
			defaultRouteType.ItemsSource = Directions.RouteType;
			defaultRouteType.SelectedItem = defaultRouteTypeSetting;

			string cycleSpeedSetting = SettingManager.instance.GetStringValue( "cycleSpeed", Directions.CycleSpeed[ 0 ] );
			cycleSpeed.ItemsSource = Directions.CycleSpeed;
			cycleSpeed.SelectedItem = cycleSpeedSetting;

			string locationEnabledSetting = Directions.EnabledDisabled[ 0 ];
			if( !SettingManager.instance.GetBoolValue( "LocationConsent", true ) )
				locationEnabledSetting = Directions.EnabledDisabled[ 1 ];

			locationEnabled.ItemsSource = Directions.EnabledDisabled;
			locationEnabled.SelectedItem = locationEnabledSetting;
			locationEnabled.SelectionChanged += locationEnabled_SelectionChanged;

			string tutorialEnabledSetting = Directions.EnabledDisabled[ 0 ];
			if( SettingManager.instance.GetBoolValue( "tutorialEnabled", true ) == false )
				tutorialEnabledSetting = Directions.EnabledDisabled[ 1 ];

			tutorialEnabled.ItemsSource = Directions.EnabledDisabled;
			tutorialEnabled.SelectedItem = tutorialEnabledSetting;
			tutorialEnabled.SelectionChanged += tutorialEnabled_SelectionChanged;

			string preventSleepSetting = Directions.EnabledDisabled[ 0 ];
			if( SettingManager.instance.GetBoolValue( "PreventSleep", true ) == false )
				preventSleepSetting = Directions.EnabledDisabled[ 1 ];

			preventSleep.ItemsSource = Directions.EnabledDisabled;
			preventSleep.SelectedItem = preventSleepSetting;
			preventSleep.SelectionChanged += preventSleep_SelectionChanged;
		}

		private void preventSleep_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				bool enabled = e.AddedItems[ 0 ].Equals( "Enabled" ) ? true : false;
				SettingManager.instance.SetBoolValue( "PreventSleep", enabled );
				if ( enabled )
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
				else
					PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
			}
		}

		private void tutorialEnabled_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				bool oldValue = SettingManager.instance.GetBoolValue( "tutorialEnabled", false );
				bool enabled = e.AddedItems[ 0 ].Equals( "Enabled" ) ? true : false;
				SettingManager.instance.SetBoolValue( "tutorialEnabled", enabled );

				if( enabled && !oldValue )
				{
					SettingManager.instance.SetBoolValue( "shownTutorial", false );
					SettingManager.instance.SetBoolValue( "shownTutorialPin", false );
					SettingManager.instance.SetBoolValue( "shownTutorialRouteType", false );
				}
				else if( !enabled && oldValue )
				{
					SettingManager.instance.SetBoolValue( "shownTutorial", true );
					SettingManager.instance.SetBoolValue( "shownTutorialPin", true );
					SettingManager.instance.SetBoolValue( "shownTutorialRouteType", true );
				}
			}
		}

		private void defaultRouteType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				SettingManager.instance.SetStringValue( "defaultRouteType", (string)e.AddedItems[ 0 ] );
			}
		}

		private void cycleSpeed_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				SettingManager.instance.SetStringValue( "cycleSpeed", (string)e.AddedItems[ 0 ] );
			}
		}

		private void locationEnabled_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				bool enabled = e.AddedItems[ 0 ].Equals( "Enabled" ) ? true : false;
				SettingManager.instance.SetBoolValue( "LocationConsent", enabled );

				if( enabled )
					LocationManager.instance.StartTracking();
				else
					LocationManager.instance.StopTracking();
			}
		}

		private void Button_Click( object sender, RoutedEventArgs e )
		{
			string plan = (string)defaultRouteType.SelectedItem;

			SettingManager.instance.SetStringValue( "defaultRouteType", plan );

			plan = (string)cycleSpeed.SelectedItem;

			SettingManager.instance.SetStringValue( "cycleSpeed", plan );

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