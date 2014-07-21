using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cyclestreets.Common;
using Cyclestreets.Resources;

namespace Cyclestreets.Objects
{
    public class RouteOverview : BindableBase
    {
        public int RouteNumber { get; set; }
        public int RouteLength { get; set; }

        public string RouteLengthString
        {
            get 
            {
                return RouteLength < 1000 ? String.Format(AppResources.MetresShort, RouteLength) : String.Format(AppResources.Miles, (int)(RouteLength*0.00062137));
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

        public int signalledJunctions { get; set; }
        public int signalledCrossings { get; set; }
        public int grammesCO2saved { get; set; }
        public int calories { get; set; }

        public RouteOverview()
        {
            
        }
    }
}
