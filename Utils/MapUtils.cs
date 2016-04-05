using System.Collections.Generic;
using System.Device.Location;
using System.Windows.Media;
using Cyclestreets.Managers;
using Cyclestreets.Pages;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Maps.Controls;
using System.Diagnostics;
using System.Linq;

namespace Cyclestreets.Utils
{
    static class MapUtils
    {
        public static string[] MapStyle = { @"OpenStreetMap", @"OpenCycleMap", @"Windows System" };

        public static async void PlotCachedRoute(Map myMap, string currentPlan)
        {
            Debug.Assert(myMap != null);

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            myMap.MapElements.Clear();

            IEnumerable<RouteSection> sections = await rm.GetRouteSections(currentPlan);
			if (sections == null)
				return;

            foreach (var routeSection in sections.Where(routeSection => routeSection.Points != null))
            {
                DrawMapMarker(myMap, routeSection.Points.ToArray(), routeSection.Walking ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 127, 0, 255), routeSection.Walking);
            }
            
			myMap.SetView(rm.GetRouteBounds(), MapAnimationKind.Parabolic);
            //SmartDispatcher.BeginInvoke(() => myMap.SetView(rm.GetRouteBounds()));
        }

        private static void DrawMapMarker(Map myMap, IEnumerable<GeoCoordinate> coordinate, Color color, bool dashed)
        {
            // Create a map marker
            MapPolyline polygon = new MapPolyline
            {
                StrokeColor = color,
                StrokeThickness = 3,
                StrokeDashed = dashed,
                Path = new GeoCoordinateCollection()
            };

			foreach (GeoCoordinate t in coordinate)
			{
				polygon.Path.Add(t);
			}

			myMap.MapElements.Add(polygon);
        }
    }
}
