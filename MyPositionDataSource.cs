using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Phone.Maps.Controls;

namespace Cyclestreets
{
	public class MyPositionDataSource : INotifyPropertyChanged
	{
		private Map MyMap;

		public MyPositionDataSource( Map MyMap )
		{
			// TODO: Complete member initialization
			this.MyMap = MyMap;
			MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;
		}

		private void MyMap_ZoomLevelChanged( object sender, MapZoomLevelChangedEventArgs e )
		{
			Visible = _isVisible;
		}

		private Visibility _isVisible = Visibility.Visible;
		public Visibility Visible
		{
			set
			{
				if( MyMap.ZoomLevel > 12 && _isVisible == Visibility.Collapsed )
				{
					_isVisible = Visibility.Visible;
					NotifyPropertyChanged( @"Visible" );
				}
				else if( MyMap.ZoomLevel <= 12 && _isVisible == Visibility.Visible )
				{
					_isVisible = Visibility.Collapsed;
					NotifyPropertyChanged( @"Visible" );
				}
			}
			get
			{
				return _isVisible;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged( String propertyName )
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if( null != handler )
			{
				handler( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
	}
}