using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using Cyclestreets.ViewModel;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using GalaSoft.MvvmLight.Ioc;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class LiveRide
    {
        private readonly DirectionsPageViewModel _viewModel;
        private readonly LiveRideViewModel _lrViewModel;

        public LiveRide()
        {
            InitializeComponent();

            _viewModel = SimpleIoc.Default.GetInstance<DirectionsPageViewModel>();
            _lrViewModel = SimpleIoc.Default.GetInstance<LiveRideViewModel>();
        }

        

       

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LocationManager.Instance.StopTracking();
            LocationManager.Instance.StartTracking(PositionAccuracy.High, 1000);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void reroute_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }
    }
}