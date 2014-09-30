using Cyclestreets.Common;
using Cyclestreets.Resources;
using System;
using System.Globalization;

namespace Cyclestreets.Objects
{
    public class RouteOverviewObject : BindableBase
    {

        public int RouteNumber { get; set; }
        public int RouteLength { get; set; }

        private int _routeDuration;
        public int RouteDuration
        {
            private get { return _routeDuration; }
            set
            {
                SetProperty(ref _routeDuration, value);
                OnPropertyChanged(@"RouteDurationString");
            }
        }

        public string RouteDurationString
        {
            get { return (RouteDuration / 60).ToString(CultureInfo.InvariantCulture); }
        }

        public string RouteLengthString
        {
            get
            {
                return RouteLength < 1000 ? String.Format(AppResources.MetresShort, RouteLength) : String.Format(AppResources.Miles, (int)(RouteLength * 0.00062137));
            }
        }

        public string RouteLengthStringMiles
        {
            get
            {
                return (RouteLength * 0.00062137).ToString(@"0.0");
            }
        }

        public int Quietness { get; set; }
        public string QuietnessString
        {
            get
            {
                if (Quietness > 80)
                    return AppResources.Quiet;
                if (Quietness > 60)
                    return AppResources.QuiteQuiet;
                if (Quietness > 40)
                    return AppResources.QuiteBusy;
                return Quietness > 20 ? AppResources.Busy : AppResources.VeryBusy;
            }
        }

        public int SignalledJunctions { get; set; }
        public int SignalledCrossings { get; set; }
        public int GrammesCo2Saved { get; set; }
        public int calories { get; set; }

        public RouteOverviewObject()
        {

        }
    }
}
