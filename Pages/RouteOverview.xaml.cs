using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Navigation;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using Cyclestreets.Resources;
using CycleStreets.Util;
using Cyclestreets.Utils;
using Cyclestreets.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Shell;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class RouteOverview
    {
        DirectionsPageViewModel _viewModel;

        public RouteOverview()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _viewModel = SimpleIoc.Default.GetInstance<DirectionsPageViewModel>();

            if (NavigationContext.QueryString.ContainsKey(@"mode"))
            {
                switch(NavigationContext.QueryString[@"mode"])
                {
                    case "routeTo":
                        {
                            var rm = SimpleIoc.Default.GetInstance<RouteManager>();
                            rm.RouteTo(double.Parse(NavigationContext.QueryString[@"longitude"]),
                                        double.Parse(NavigationContext.QueryString[@"latitude"]));
                            break;
                        }
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _viewModel.DisplayMap = false;
        }

        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = @"823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = @"xrQJghWalYn52fTfnUhWPQ";

            var rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                var routeData = (string)PhoneApplicationService.Current.State[@"loadedRoute"];
                rm.ParseRouteData(routeData, _viewModel.CurrentPlan, false);
            }

            var newplan = rm.HasCachedRoute(_viewModel.CurrentPlan);
            if (newplan == null) return;
            _viewModel.CurrentPlan = newplan;
            StartRouting();
        }

        private void MyMap_Tap(object sender, GestureEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void details_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/DirectionsResults.xaml", UriKind.RelativeOrAbsolute));
        }

        private void shortest1_Tap(object sender, GestureEventArgs e)
        {
            if (_viewModel.CurrentPlan == @"quietest")
                return;
            _viewModel.CurrentPlan = @"quietest";
            StartRouting();
        }

        private async void StartRouting()
        {
            var rm = SimpleIoc.Default.GetInstance<RouteManager>();
            bool result = await rm.FindRoute(_viewModel.CurrentPlan, false);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error(@"Route Planning Error");

                MessageBox.Show(
                    AppResources.RouteParseError);
            }
            else
            {
                MapUtils.PlotCachedRoute(MyMap, _viewModel.CurrentPlan);
                _viewModel.DisplayMap = true;
                SmartDispatcher.BeginInvoke(() => MyMap.SetView(rm.GetRouteBounds()));
            }
        }

        private void balanced1_Tap(object sender, GestureEventArgs e)
        {
            if (_viewModel.CurrentPlan == @"balanced")
                return;
            _viewModel.CurrentPlan = @"balanced";
            StartRouting();
        }

        private void fastest1_Tap(object sender, GestureEventArgs e)
        {
            if (_viewModel.CurrentPlan == @"fastest")
                return;
            _viewModel.CurrentPlan = @"fastest";
            StartRouting();
        }

        private async void mylocation_Click(object sender, EventArgs e)
        {
            if (LocationManager.Instance.MyGeoPosition != null)
            {
                GeoCoordinate geo = CoordinateConverter.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                MapLocation loc = await GeoUtils.StartReverseGeocode(geo);

                SmartDispatcher.BeginInvoke(() => MyMap.SetView(loc.GeoCoordinate, 16));
            }
            else
            {
                Util.showLocationDialog();
            }
        }

        private void liveride_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LiveRide.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}