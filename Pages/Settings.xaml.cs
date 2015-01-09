using System;
using System.Windows;
using System.Windows.Controls;
using Cyclestreets.Managers;
using Cyclestreets.Objects;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Cyclestreets.Pages
{
	public partial class Settings
	{
		public Settings()
		{
			InitializeComponent();

			string defaultMapStyleSetting = SettingManager.instance.GetStringValue( @"mapStyle", MapUtils.MapStyle[0] );
            mapStyle.ItemsSource = MapUtils.MapStyle;
			mapStyle.SelectedItem = defaultMapStyleSetting;

			string defaultRouteTypeSetting = SettingManager.instance.GetStringValue( @"defaultRouteType", DirectionsPage.RouteType[0].Value );
			defaultRouteType.ItemsSource = DirectionsPage.RouteType;
            defaultRouteType.DisplayMemberPath = @"DisplayName";
            int idx = Array.FindIndex(DirectionsPage.RouteType, v => v.Value.Equals(defaultRouteTypeSetting));
            defaultRouteType.SelectedIndex = idx == -1 ? 0 : idx;

			string cycleSpeedSetting = SettingManager.instance.GetStringValue( @"cycleSpeed", DirectionsPage.CycleSpeed[0] );
			cycleSpeed.ItemsSource = DirectionsPage.CycleSpeed;
			cycleSpeed.SelectedItem = cycleSpeedSetting;

			string locationEnabledSetting = DirectionsPage.EnabledDisabled[0];
			if( !SettingManager.instance.GetBoolValue( @"LocationConsent", true ) )
				locationEnabledSetting = DirectionsPage.EnabledDisabled[1];

			locationEnabled.ItemsSource = DirectionsPage.EnabledDisabled;
			locationEnabled.SelectedItem = locationEnabledSetting;
			locationEnabled.SelectionChanged += locationEnabled_SelectionChanged;
            
			string preventSleepSetting = DirectionsPage.EnabledDisabled[0];
			if( SettingManager.instance.GetBoolValue( @"PreventSleep", true ) == false )
				preventSleepSetting = DirectionsPage.EnabledDisabled[1];

			preventSleep.ItemsSource = DirectionsPage.EnabledDisabled;
			preventSleep.SelectedItem = preventSleepSetting;
			preventSleep.SelectionChanged += preventSleep_SelectionChanged;
		}

		private void preventSleep_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				bool enabled = e.AddedItems[0].Equals( AppResources.Enabled );
				SettingManager.instance.SetBoolValue( @"PreventSleep", enabled );
				PhoneApplicationService.Current.UserIdleDetectionMode = enabled ? IdleDetectionMode.Disabled : IdleDetectionMode.Enabled;
			}
		}

		private void defaultRouteType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				SettingManager.instance.SetStringValue( @"defaultRouteType", ((ListBoxPair)e.AddedItems[0]).Value );
			}
		}

		private void cycleSpeed_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				SettingManager.instance.SetStringValue( @"cycleSpeed", (string)e.AddedItems[0] );
			}
		}

		private void locationEnabled_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				bool enabled = e.AddedItems[0].Equals( AppResources.Enabled );
				SettingManager.instance.SetBoolValue( @"LocationConsent", enabled );

				if( enabled )
					LocationManager.Instance.StartTracking();
				else
					LocationManager.Instance.StopTracking();
			}
		}

		private void Button_Click( object sender, RoutedEventArgs e )
		{
			string plan = ((ListBoxPair)defaultRouteType.SelectedItem).Value;

			SettingManager.instance.SetStringValue( @"defaultRouteType", plan );

			plan = (string)cycleSpeed.SelectedItem;

			SettingManager.instance.SetStringValue( @"cycleSpeed", plan );

			NavigationService.GoBack();
		}

		private void Hyperlink_Click( object sender, RoutedEventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask
			{
				Uri = new Uri( "http://www.cyclestreets.net/" )
			};
			url.Show();
		}

		private void Hyperlink_Click_1( object sender, RoutedEventArgs e )
		{
			WebBrowserTask url = new WebBrowserTask
			{
				Uri = new Uri( "http://www.rwscripts.com/" )
			};
			url.Show();
		}

		private void Hyperlink_Click_2( object sender, RoutedEventArgs e )
		{
		    NavigationService.Navigate(new Uri("/Pages/Feedback.xaml", UriKind.RelativeOrAbsolute));
		}

		private void mapStyle_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.AddedItems.Count > 0 )
			{
				SettingManager.instance.SetStringValue( @"mapStyle", (string)e.AddedItems[0] );
			}
		}

        private void resetTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            SettingManager.instance.SetBoolValue(@"tutorialEnabled", true);
            SettingManager.instance.SetBoolValue(@"shownTutorial", false);
            SettingManager.instance.SetBoolValue(@"shownTutorialPin", false);
            SettingManager.instance.SetBoolValue(@"shownTutorialRouteType", false);
        }
	}
}