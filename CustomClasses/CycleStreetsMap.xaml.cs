using Cyclestreets.Managers;
using Cyclestreets.Pages;
using Cyclestreets.Resources;
using Cyclestreets.Utils;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;

namespace Cyclestreets.CustomClasses
{
    public partial class CycleStreetsMap : IDisposable
    {
        // Use this from XAML to control whether animation is on or off
        #region DefaultPlan Dependency Property
        public static readonly DependencyProperty DefaultPlanProperty =
            DependencyProperty.Register("DefaultPlan", typeof(string), typeof(CycleStreetsMap), new PropertyMetadata(null, null));

        public string DefaultPlan
        {
            get
            {
                return (string)GetValue(DefaultPlanProperty);
            }
            set
            {
                SetValue(DefaultPlanProperty, value);
            }
        }
        #endregion

        public GeoCoordinate Center
        {
            get
            {
                return MyMap.Center;
            }
            set { MyMap.Center = value; }
        }

        public Map Map
        {
            get
            {
                return MyMap;
            }
        }

        public CycleStreetsMap()
        {
            InitializeComponent();

            LocationManager.Instance.PositionChanged += positionChangedHandler;
        }

        ~CycleStreetsMap()
        {
            LocationManager.Instance.PositionChanged -= positionChangedHandler;
            if (MyMap != null)
            {
                MyMap.ZoomLevelChanged -= MyMap_ZoomLevelChanged;
                MyMap.Tap -= MyMap_Tap;
            }
        }

        public void Dispose()
        {
            LocationManager.Instance.PositionChanged -= positionChangedHandler;
            if (MyMap != null)
            {
                MyMap.ZoomLevelChanged -= MyMap_ZoomLevelChanged;
                MyMap.Tap -= MyMap_Tap;
            }
        }

        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = @"823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = @"xrQJghWalYn52fTfnUhWPQ";

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (PhoneApplicationService.Current.State.ContainsKey(@"loadedRoute") && PhoneApplicationService.Current.State[@"loadedRoute"] != null)
            {
                string routeData = (string)PhoneApplicationService.Current.State[@"loadedRoute"];
                rm.ParseRouteData(routeData, DefaultPlan, false);
            }

            var newplan = rm.HasCachedRoute(DefaultPlan);
            if (newplan == null) return;
            DefaultPlan = newplan;

            MyMap.ZoomLevel = 18;

            MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;
            MyMap.Tap += MyMap_Tap;

            PlotRoute();

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

        private MapOverlay _myLocationOverlay;
        private MapOverlay _myLocationOverlay2;
        private Ellipse _accuracyEllipse;
        private DateTime _timeLastMoved;

        private void positionChangedHandler(Geolocator sender, PositionChangedEventArgs args)
        {
            if (args == null || args.Position == null)
                return;

            SmartDispatcher.BeginInvoke(() =>
            {
                PositionChanged(args.Position);

            });
        }

        private void PositionChanged(Geoposition geoposition)
        {
            if (geoposition.Coordinate.Accuracy > 50)
            {
                VisualStateManager.GoToState(this, @"OnScreen", true);
            }
            else
            {
                VisualStateManager.GoToState(this, @"OffScreen", true);
            }

            if (LocationManager.Instance.MyGeoPosition == null) return;
            double myAccuracy = LocationManager.Instance.MyGeoPosition.Coordinate.Accuracy;
            GeoCoordinate myCoordinate = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
            if (_myLocationOverlay == null)
            {
                Ellipse myCircle = new Ellipse
                {
                    Fill = new SolidColorBrush(Colors.Black),
                    Height = 20,
                    Width = 20,
                    Opacity = 30
                };
                Binding myBinding = new Binding(@"Visible") { Source = new MyPositionDataSource(MyMap) };
                myCircle.Visibility = Visibility.Visible;
                myCircle.SetBinding(VisibilityProperty, myBinding);

                _accuracyEllipse = new Ellipse
                {
                    Fill = new SolidColorBrush(Color.FromArgb(75, 200, 0, 0)),
                    Visibility = Visibility.Visible
                };
                _accuracyEllipse.SetBinding(VisibilityProperty, myBinding);

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
                MapLayer myLocationLayer = new MapLayer { _myLocationOverlay, _myLocationOverlay2 };

                MyMap.Layers.Add(myLocationLayer);
            }

            _myLocationOverlay.GeoCoordinate = myCoordinate;
            _myLocationOverlay2.GeoCoordinate = myCoordinate;

            double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double radius = myAccuracy / metersPerPixels;
            _accuracyEllipse.Width = radius * 2;
            _accuracyEllipse.Height = radius * 2;

            if (DateTime.Now - _timeLastMoved > new TimeSpan(0, 0, 20))
                MyMap.Center = myCoordinate;
        }

        private void MyMap_ZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            if (LocationManager.Instance.MyGeoPosition != null)
            {
                double myAccuracy = LocationManager.Instance.MyGeoPosition.Coordinate.Accuracy;
                GeoCoordinate myCoordinate = GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate);
                double metersPerPixels = (Math.Cos(myCoordinate.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
                double radius = myAccuracy / metersPerPixels;
                if (_accuracyEllipse != null)
                {
                    _accuracyEllipse.Width = radius * 2;
                    _accuracyEllipse.Height = radius * 2;
                }
            }
        }

        private async void PlotRoute()
        {
            var rm = SimpleIoc.Default.GetInstance<RouteManager>();
            bool result = await rm.FindRoute(DefaultPlan, false);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error(@"Route Planning Error");

                MessageBox.Show(
                    AppResources.RouteParseError);
            }
            else
            {
                MapUtils.PlotCachedRoute(MyMap, DefaultPlan);
            }
        }

        private void MyMap_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _timeLastMoved = DateTime.Now;
        }

        private void MyMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _timeLastMoved = DateTime.Now;
        }

        private LocationRectangle GetMapBounds()
        {
            GeoCoordinate topLeft = MyMap.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            GeoCoordinate bottomRight = MyMap.ConvertViewportPointToGeoCoordinate(new Point(MyMap.Width, MyMap.Height));

            return LocationRectangle.CreateBoundingRectangle(new[] { topLeft, bottomRight });
        }

        private void ApplicationBarMenuItem_ToggleAerialView(object sender)
        {
            ApplicationBarMenuItem item = sender as ApplicationBarMenuItem;
            if (MyMap.CartographicMode == MapCartographicMode.Hybrid)
            {
                if (item != null)
                    item.Text = AppResources.MainPage_ApplicationBarMenuItem_ToggleAerialView_Enable_aerial_view;
                MyMap.CartographicMode = MapCartographicMode.Road;
            }
            else
            {
                if (item != null)
                    item.Text = AppResources.MainPage_ApplicationBarMenuItem_ToggleAerialView_Disable_aerial_view;
                MyMap.CartographicMode = MapCartographicMode.Hybrid;
            }
        }

        public void AddLayer(MapLayer l)
        {
            MyMap.Layers.Add(l);
        }

        public double ZoomLevel { get { return MyMap.ZoomLevel; } set { MyMap.ZoomLevel = value; } }
    }
}

