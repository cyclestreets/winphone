using Cyclestreets.Annotations;
using GalaSoft.MvvmLight;

namespace Cyclestreets.ViewModel
{
    [UsedImplicitly]
    public class LiveRideViewModel : ViewModelBase
    {
        private double _metresPerSecond;

        public double MetresPerSecond
        {
            private get { return _metresPerSecond; }
            set
            {
                SmartDispatcher.BeginInvoke(() =>
                {
                    if (  double.IsNaN(value) )
// ReSharper disable once RedundantArgumentDefaultValue
                      Set(ref _metresPerSecond, 0);
                    else
                        Set(ref _metresPerSecond, value);
                    RaisePropertyChanged(@"Speed");
                });
            }
        }

        public string Speed
        {
            get
            {
                return SettingManager.instance.GetStringValue(@"speed", @"mph") == @"mph" ? (MetresPerSecond*2.23693629).ToString(@"0.00") : (MetresPerSecond*3.6).ToString(@"0.00");
                //return MetresPerSecond.ToString();
            }
        }
    }
}
