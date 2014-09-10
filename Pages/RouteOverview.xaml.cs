using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Cyclestreets.Managers;
using CycleStreets.Util;
using Cyclestreets.Utils;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Shell;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Cyclestreets.ViewModel;

namespace Cyclestreets
{
    public partial class RouteOverview : PhoneApplicationPage
    {
        DirectionsPageViewModel viewModel;

        public RouteOverview()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel = SimpleIoc.Default.GetInstance<DirectionsPageViewModel>();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            viewModel.DisplayMap = false;
        }

        private async void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (PhoneApplicationService.Current.State.ContainsKey("loadedRoute") && PhoneApplicationService.Current.State["loadedRoute"] != null)
            {
                string routeData = (string)PhoneApplicationService.Current.State["loadedRoute"];
                rm.ParseRouteData(routeData, viewModel.CurrentPlan, false);
            }

            var newplan = rm.HasCachedRoute(viewModel.CurrentPlan);
            if (newplan == null) return;
            viewModel.CurrentPlan = newplan;
            StartRouting();
        }

        private void MyMap_Tap(object sender, GestureEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void details_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/DirectionsResults.xaml", UriKind.RelativeOrAbsolute));
        }

        private void shortest1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (viewModel.CurrentPlan == "shortest")
                return;
            viewModel.CurrentPlan = "shortest";
            StartRouting();
        }

        private async void StartRouting()
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            bool result = await rm.FindRoute(viewModel.CurrentPlan, false);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error("Route Planning Error");

                MessageBox.Show(
                    "Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
            }
            else
            {
                MapUtils.PlotCachedRoute(MyMap, viewModel.CurrentPlan);
                viewModel.DisplayMap = true;
            }
        }

        private void balanced1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (viewModel.CurrentPlan == "balanced")
                return;
            viewModel.CurrentPlan = "balanced";
            StartRouting();
        }

        private void fastest1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (viewModel.CurrentPlan == "fastest")
                return;
            viewModel.CurrentPlan = "fastest";
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
    }
}