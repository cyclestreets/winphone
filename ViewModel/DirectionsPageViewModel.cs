//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using System.Collections.Generic;
using System.Device.Location;
using Cyclestreets.Utils;
using GalaSoft.MvvmLight;
using Microsoft.Phone.Maps.Toolkit;
using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
namespace Cyclestreets.ViewModel
{
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

			}
			else
			{
			
			}
		}

		
	}
}
