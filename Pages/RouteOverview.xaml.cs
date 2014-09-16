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
        private bool _mapReady = false;

        public RouteOverview()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _viewModel = SimpleIoc.Default.GetInstance<DirectionsPageViewModel>();

            var rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (NavigationContext.QueryString.ContainsKey(@"mode"))
            {
                switch(NavigationContext.QueryString[@"mode"])
                {
                    case "planroute":
                        {
                            bool result = await rm.FindRoute(_viewModel.CurrentPlan, true);
                            if (!result)
                            {
                                MarkedUp.AnalyticClient.Error(@"Route Planning Error");

                                MessageBox.Show(AppResources.RouteParseError);
                            }
                            else
                            {
                                if (_mapReady)
                                    StartRouting();
                            }
                            break;
                        }
                    case "routeTo":
                        {
                            
                            bool result = await rm.RouteTo(double.Parse(NavigationContext.QueryString[@"longitude"]),
                                        double.Parse(NavigationContext.QueryString[@"latitude"]),
                                        _viewModel.CurrentPlan);
                            if (!result)
                            {
                                MarkedUp.AnalyticClient.Error(@"Route Planning Error");

                                MessageBox.Show(AppResources.RouteParseError);
                            }
                            else
                            {
                                if (_mapReady)
                                    StartRouting();
                            }
                            break;
                        }
                    case "leisure":
                        {
                            int duration = -1;
                            int distance = -1;
                            string poiTypes = null;

                            if (NavigationContext.QueryString.ContainsKey(@"duration"))
                                duration = int.Parse(NavigationContext.QueryString[@"duration"]);
                            if (NavigationContext.QueryString.ContainsKey(@"distance"))
                                distance = int.Parse(NavigationContext.QueryString[@"distance"]);
                            if (NavigationContext.QueryString.ContainsKey(@"poitypes"))
                                poiTypes = NavigationContext.QueryString[@"poitypes"];

                            bool result = await rm.FindLeisureRoute(duration,distance,poiTypes);
                            if (!result)
                            {
                                MarkedUp.AnalyticClient.Error(@"Route Planning Error");

                                MessageBox.Show(AppResources.RouteParseError);
                            }
                            else
                            {
                                if (_mapReady)
                                    StartRouting();
                            }
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

            _mapReady = true;

            var newplan = rm.HasCachedRoute(_viewModel.CurrentPlan);
            if (newplan == null) 
                return;
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
                if (LocationManager.Instance.MyGeoPosition != null)
                {
                    MyMap.Pitch = 0;
                    MyMap.Heading = 0;
                    MyMap.Center = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                    MyMap.ZoomLevel = 10;
                }
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
                GeoCoordinate geo = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                MapLocation loc = await GeoUtils.StartReverseGeocode(geo);

                SmartDispatcher.BeginInvoke(() => {
                       MyMap.Pitch = 0;
                       MyMap.Heading = 0;
                       MyMap.SetView(loc.GeoCoordinate, 16);
                });
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

        private void routeList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                RouteSection rs = e.AddedItems[0] as RouteSection;
                if (rs != null)
                {
                    if ( rs is StartPoint )
                    {
                        var rm = SimpleIoc.Default.GetInstance<RouteManager>();
                        MyMap.SetView(rm.GetRouteBounds());
                        MyMap.Pitch = 0;
                        MyMap.Heading = 0;
                    }
                    else if (rs.Points.Count > 0)
                    {
                        MyMap.SetView(rs.Points[0], 20, (rs.Bearing==null)?0:rs.Bearing, 75);

                        MyMap.TileSources.Clear();
                    }
                }
            }
        }
    }
}