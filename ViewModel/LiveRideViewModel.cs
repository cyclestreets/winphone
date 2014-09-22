using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                if (SettingManager.instance.GetStringValue(@"speed", @"mph") == @"mph")
                {
                    return (MetresPerSecond*2.23693629).ToString(@"0.00");
                }
                else
                {
                    return (MetresPerSecond*3.6).ToString(@"0.00");
                }
                //return MetresPerSecond.ToString();
            }
        }
        public LiveRideViewModel()
        {
            
        }
    }
}
