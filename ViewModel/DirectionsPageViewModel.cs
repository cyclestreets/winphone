using GalaSoft.MvvmLight;
using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
namespace Cyclestreets.ViewModel
{
// ReSharper disable once ClassNeverInstantiated.Global
	public class DirectionsPageViewModel : ViewModelBase
	{
        public RouteManager RouteManagerPtr
        {
            get 
            {
                return SimpleIoc.Default.GetInstance<RouteManager>();
            }
        }

        private bool _displayMap;
        public bool DisplayMap
        {
            get { return _displayMap; }
            set { Set(ref _displayMap, value); }
        }

        private bool _canChangeType = true;
        public bool CanChangeRouteType
        {
            get { return _canChangeType; }
            set { Set(ref _canChangeType, value); }
        }

        private string _currentPlan;
        public string CurrentPlan
        {
            get { return _currentPlan; }
            set { Set(ref _currentPlan, value); }
        }

		public DirectionsPageViewModel()
		{
			if( IsInDesignMode )
			{
			    RouteManagerPtr.GenerateDebugData();
                CurrentPlan = @"balanced";
			}
			else
			{
                // Setup route type dropdown
                string plan = SettingManager.instance.GetStringValue(@"defaultRouteType", @"balanced");
                CurrentPlan = plan.Replace(@" route", "");
			}
		}

		
	}
}
