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

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = @"823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = @"xrQJghWalYn52fTfnUhWPQ";

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                string routeData = (string)PhoneApplicationService.Current.State[@"loadedRoute"];
                rm.ParseRouteData(routeData, _viewModel.CurrentPlan, false);
            }

            var newplan = rm.HasCachedRoute(_viewModel.CurrentPlan);
            if (newplan == null) return;
            _viewModel.CurrentPlan = newplan;
            //StartRouting();

            LocationManager.instance.LockToMyPos(true);
            LocationManager.instance.SetMap(MyMap);
            MyMap.ZoomLevel = 18;

            PlotRoute();
            positionChangedHandler(null, null);

            SetMapStyle();
        }

        private void SetMapStyle()
        {
            MyTileSource ts;
            switch (SettingManager.instance.GetStringValue(@"mapStyle", MapUtils.MapStyle[0]))
            {
                case "OpenStreetMap":
                    ts = new OSMTileSource();
                    break;
                case "OpenCycleMap":
                    ts = new OCMTileSource();
                    break;
                default:
                    ts = null;
                    break;
            }
            MyMap.TileSources.Clear();
            if (ts != null)
                MyMap.TileSources.Add(ts);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LocationManager.instance.StopTracking();
            LocationManager.instance.StartTracking(30,1000);

            if (e.NavigationMode == NavigationMode.New)
            {
                LocationManager.instance.trackingGeolocator.PositionChanged += positionChangedHandler;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            LocationManager.instance.trackingGeolocator.PositionChanged -= positionChangedHandler;
        }

        private MapOverlay _myLocationOverlay;
        private MapOverlay _myLocationOverlay2;
        private Ellipse _accuracyEllipse;
        private DateTime _timeLastMoved;
        private void positionChangedHandler(Geolocator sender, PositionChangedEventArgs args)
        {
            if (args == null || args.Position == null)
                return;

            if ( args.Position.Coordinate != null && args.Position.Coordinate.Speed!=null)
                _lrViewModel.MetresPerSecond = (double)args.Position.Coordinate.Speed;

            

            SmartDispatcher.BeginInvoke(() =>
            {
                if (args.Position.Coordinate.Accuracy > 50)
                {
                    VisualStateManager.GoToState(this, @"OnScreen", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, @"OffScreen", true);
                }

                if (LocationManager.instance.MyGeoPosition == null) return;
                double myAccuracy = LocationManager.instance.MyGeoPosition.Coordinate.Accuracy;
                GeoCoordinate myCoordinate = CoordinateConverter.ConvertGeocoordinate(LocationManager.instance.MyGeoPosition.Coordinate);
                if (_myLocationOverlay == null)
                {
                    Ellipse myCircle = new Ellipse
                    {
                        Fill = new SolidColorBrush(Colors.Black),
                        Height = 20,
                        Width = 20,
                        Opacity = 30
                    };
                    Binding myBinding = new Binding(@"Visible") {Source = new MyPositionDataSource(MyMap)};
                    myCircle.Visibility = Visibility.Visible;
                    myCircle.SetBinding(VisibilityProperty, myBinding);

                    MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;

                    _accuracyEllipse = new Ellipse
                    {
                        Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0)),
                        Visibility = Visibility.Visible
                    };
                    _accuracyEllipse.SetBinding(VisibilityProperty, myBinding);

                    // Create a MapOverlay to contain the circle.
                    _myLocationOverlay = new MapOverlay
                    {
                        Content = myCircle,
                        PositionOrigin = new Point(0.5, 0.5),
                        GeoCoordinate = myCoordinate
                    };

                    _myLocationOverlay2 = new MapOverlay
                    {
                        Content = _accuracyEllipse,
                        PositionOrigin = new Point(0.5, 0.5),
                        GeoCoordinate = myCoordinate
                    };

                    // Create a MapLayer to contain the MapOverlay.
                    MapLayer myLocationLayer = new MapLayer {_myLocationOverlay, _myLocationOverlay2};

                    MyMap.Layers.Add(myLocationLayer);
                }

                _myLocationOverlay.GeoCoordinate = myCoordinate;
                _myLocationOverlay2.GeoCoordinate = myCoordinate;

                double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
                double radius = myAccuracy / metersPerPixels;
                _accuracyEllipse.Width = radius * 2;
                _accuracyEllipse.Height = radius * 2;

                if ( DateTime.Now - _timeLastMoved > new TimeSpan(  0,0,20))
                    MyMap.SetView(myCoordinate, MyMap.ZoomLevel);//, (double)args.Position.Coordinate.Heading, 75.0);
            });
        }

        private void MyMap_ZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            double myAccuracy = LocationManager.instance.MyGeoPosition.Coordinate.Accuracy;
            GeoCoordinate myCoordinate = CoordinateConverter.ConvertGeocoordinate(LocationManager.instance.MyGeoPosition.Coordinate);
            double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = myAccuracy / metersPerPixels;
            _accuracyEllipse.Width = radius * 2;
            _accuracyEllipse.Height = radius * 2;
        }

        private async void PlotRoute()
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
            }
        }

        private void MyMap_CenterChanged(object sender, MapCenterChangedEventArgs e)
        {
            _timeLastMoved = DateTime.Now;
        }
    }
}