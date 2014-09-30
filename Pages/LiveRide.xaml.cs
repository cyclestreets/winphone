using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using Cyclestreets.Utils;
using Cyclestreets.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Maps.Controls;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
    public partial class LiveRide
    {
        private readonly DirectionsPageViewModel _viewModel;
        private readonly LiveRideViewModel _lrViewModel;

        private readonly List<Point> _routeSegments = new List<Point>();

        private int _currentTargetPoint = -1;
        private MapOverlay _myLocationOverlay;
        private MapLayer _myLocationLayer;

        public LiveRide()
        {
            InitializeComponent();

            _viewModel = SimpleIoc.Default.GetInstance<DirectionsPageViewModel>();
            _lrViewModel = SimpleIoc.Default.GetInstance<LiveRideViewModel>();

            
        }

        private void positionChangedHandler(Geolocator sender, PositionChangedEventArgs args)
        {
            GenerateLineSegments();
            if (args.Position != null && args.Position.Coordinate != null && args.Position.Coordinate.Speed != null)
                _lrViewModel.MetresPerSecond = args.Position.Coordinate.Speed.GetValueOrDefault();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LocationManager.Instance.StopTracking();
            LocationManager.Instance.StartTracking(PositionAccuracy.High, 1000);

            GenerateLineSegments();

            LocationManager.Instance.PositionChanged += positionChangedHandler;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            LocationManager.Instance.PositionChanged -= positionChangedHandler;
        }

        private void GenerateLineSegments()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                _routeSegments.Clear();

                var rm = SimpleIoc.Default.GetInstance<RouteManager>();
                IEnumerable<RouteSection> sections = rm.GetRouteSections(_viewModel.CurrentPlan);
                foreach (var routeSection in sections.Where(routeSection => routeSection.Points != null))
                {
                    _routeSegments.AddRange(
                        routeSection.Points.Select(p => MyMap.Map.ConvertGeoCoordinateToViewportPoint(p)).ToList());
                }
                FindClosestPointOnLine();
            });
        }

        private void FindClosestPointOnLine()
        {
            int startPointIdx = 0;
            int endPointIdx = _routeSegments.Count - 1;
            if (_currentTargetPoint != -1)
            {
                endPointIdx = Math.Min(_routeSegments.Count - 2, _currentTargetPoint + 5);
                startPointIdx = Math.Min(0, _currentTargetPoint - 2);
            }

            SmartDispatcher.BeginInvoke(() =>
            {
                if (LocationManager.Instance.MyGeoPosition == null)
                    return;

                Point myPoint =
                    MyMap.Map.ConvertGeoCoordinateToViewportPoint(
                        GeoUtils.ConvertGeocoordinate(LocationManager.Instance.MyGeoPosition.Coordinate));

                Point? closest = null;
                double closestDist = double.MaxValue;
                for (int i = startPointIdx; i < endPointIdx; i++)
                {
                    Point closestTemp;
                    var d = FindDistanceToSegment(myPoint, _routeSegments[i], _routeSegments[i + 1], out closestTemp);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closest = closestTemp;
                    }
                }

                DrawMyLocationOnMap(closest);
            });
        }

        private void DrawMyLocationOnMap(Point? closest)
        {
            if (closest == null) return;
            GeoCoordinate myCoordinate = MyMap.Map.ConvertViewportPointToGeoCoordinate(closest.GetValueOrDefault());
            if (_myLocationOverlay == null)
            {
                Ellipse myCircle = new Ellipse
                {
                    Fill = new SolidColorBrush(Colors.Blue),
                    Height = 20,
                    Width = 20,
                    Opacity = 30
                };

                // Create a MapOverlay to contain the circle.
                _myLocationOverlay = new MapOverlay
                {
                    Content = myCircle,
                    PositionOrigin = new Point(0.5, 0.5),
                    GeoCoordinate = myCoordinate
                };

                // Create a MapLayer to contain the MapOverlay.
                if (_myLocationLayer != null)
                    MyMap.Map.Layers.Remove(_myLocationLayer);

                _myLocationLayer = new MapLayer { _myLocationOverlay };

                MyMap.Map.Layers.Add(_myLocationLayer);
            }

            _myLocationOverlay.GeoCoordinate = myCoordinate;
        }

        private double FindDistanceToSegment(Point pt, Point p1, Point p2, out Point closest)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((Math.Abs(dx) < 0.001) && (Math.Abs(dy) < 0.001))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void reroute_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }
    }
}