using System;
using System.Collections.Generic;
using System.Device.Location;
using Cyclestreets.Common;

namespace Cyclestreets.Pages
{
    public class RouteSection : BindableBase
    {
        public readonly List<GeoCoordinate> Points = new List<GeoCoordinate>();
        public List<int> Distances = new List<int>();
        public List<int> Height = new List<int>();

        public string Description { get; set; }
        public dynamic Distance;
        public dynamic Bearing;
        private int _time;

        public int Time
        {
            get { return _time; }
            set { SetProperty(ref _time, value); OnPropertyChanged("TimeString"); }
        }

        public string TimeString
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds( Time );

                return t.Hours > 0 ? string.Format("{0}h {1:D2}m {2:D2}s", t.Hours, t.Minutes, t.Seconds) : string.Format("{0}m {1:D2}s:", t.Minutes, t.Seconds);
            }
        }
        public bool Walking { get; set; }
    }
}
