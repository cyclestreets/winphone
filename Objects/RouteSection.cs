using System.Collections.Generic;
using System.Device.Location;

namespace Cyclestreets.Pages
{
    class RouteSection
    {
        public List<GeoCoordinate> Points = new List<GeoCoordinate>();
        public List<int> Distances = new List<int>();
        public List<int> Height = new List<int>();

        public string Description;
        public dynamic Distance;
        public dynamic Bearing;
        public bool Walking { get; set; }
    }
}
