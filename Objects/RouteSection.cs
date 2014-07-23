using Cyclestreets.Common;
using Cyclestreets.Resources;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Windows.Media.Imaging;

namespace Cyclestreets.Pages
{

    public class StartPoint : RouteSection
    {
        public override string VoiceDescription
        {
            get { return string.Format(AppResources.VoiceDescriptionStart, Description ); }
        }
    }

    public class EndPoint : RouteSection
    {
        public override string VoiceDescription
        {
            get { return string.Format(AppResources.VoiceDescriptionEnd, DistanceString); }
        }
    }

    public class RouteSection : BindableBase
    {
        public readonly List<GeoCoordinate> Points = new List<GeoCoordinate>();
        public List<int> Distances = new List<int>();
        public List<int> Height = new List<int>();

        public string Description { get; set; }

        public virtual string VoiceDescription
        {
            get { return string.Format(AppResources.VoiceDescription, DistanceString, Turn, Description); }
        }

        public string DistanceString
        {
            get
            {
                return Distance < 1000 ? String.Format(AppResources.MetresShort, Distance) : String.Format(AppResources.Miles, (int)(Distance * 0.00062137));
            }
        }
        public int Distance;
        public dynamic Bearing;
        private int _time;
        private string _turn;

        public string Turn
        {
            get { return _turn; }
            set
            {
                SetProperty(ref _turn, value);
                OnPropertyChanged("TurnImage");
            }
        }

        public BitmapImage TurnImage
        {
            get
            {
                if (Turn == null)
                    return null;

                string filename = Turn.Replace(' ', '_');
                return new BitmapImage(new Uri("/Assets/navigation/" + filename + ".png", UriKind.RelativeOrAbsolute));
            }
        }

        public int Time
        {
            get { return _time; }
            set { SetProperty(ref _time, value); OnPropertyChanged("TimeString"); }
        }

        public string TimeString
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(Time);

                return t.Hours > 0 ? string.Format("{0}h {1:D2}m {2:D2}s", t.Hours, t.Minutes, t.Seconds) : string.Format("{0}m {1:D2}s:", t.Minutes, t.Seconds);
            }
        }
        public bool Walking { get; set; }
    }
}
