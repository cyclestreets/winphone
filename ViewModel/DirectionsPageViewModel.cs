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
		
		public DirectionsPageViewModel()
		{
			if( IsInDesignMode )
			{
			    RouteManagerPtr.GenerateDebugData();

			}
			else
			{
			
			}
		}

		
	}
}
