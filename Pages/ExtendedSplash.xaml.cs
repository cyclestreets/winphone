using System;
using System.Windows.Navigation;
using Cyclestreets.Managers;
using Microsoft.Phone.Shell;

namespace Cyclestreets.Pages
{
    public partial class ExtendedSplash
    {
        public ExtendedSplash()
        {
            InitializeComponent();

            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);

            var prog = new ProgressIndicator {IsVisible = true, IsIndeterminate = true, Text = "Getting location..."};

            SystemTray.SetProgressIndicator(this, prog);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (SettingManager.instance.GetBoolValue(@"LocationConsent", true))
            {
                LocationManager.Instance.StartTracking();
                LocationManager.Instance.PositionChanged += Instance_PositionChanged;
            }
            else
            {
                NavigationService.Navigate(new Uri(@"/Pages/MainPage.xaml"));
            }
        }

        void Instance_PositionChanged(Windows.Devices.Geolocation.Geolocator sender, Windows.Devices.Geolocation.PositionChangedEventArgs args)
        {
            SmartDispatcher.BeginInvoke(() => NavigationService.Navigate(new Uri(@"/Pages/MainPage.xaml", UriKind.RelativeOrAbsolute)));
        }
    }
}