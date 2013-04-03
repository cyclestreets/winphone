using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cyclestreets
{
	public class SmartDispatcher
	{
		public static void BeginInvoke( Action action )
		{
			if( Deployment.Current.Dispatcher.CheckAccess()
				|| DesignerProperties.IsInDesignTool )
			{
				action();
			}
			else
			{
				Deployment.Current.Dispatcher.BeginInvoke( action );
			}
		}
	}
}
