using Cyclestreets.Utils;
using Microsoft.Phone.Maps.Controls;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	class LocationManager
	{
		public static LocationManager Instance;

		public Geoposition MyGeoPosition
		{
			get; private set;
		}
		public Geolocator TrackingGeolocator;

		private readonly Map _myMap;
		private bool _lockToMyPos;

		public LocationManager( Map map )
		{
			Instance = this;
			_myMap = map;
		}

		public void LockToMyPos( bool doLock )
		{
			_lockToMyPos = doLock;
		}

		public void StartTracking(double interval = 30000 )
		{
			if( this.TrackingGeolocator != null )
			{
				return;
			}

			this.TrackingGeolocator = new Geolocator {ReportInterval = (uint) interval, DesiredAccuracy = PositionAccuracy.High};
		    //this.TrackingGeolocator.DesiredAccuracyInMeters = (uint)accuracy;

			// this implicitly starts the tracking operation
			this.TrackingGeolocator.PositionChanged += positionChangedHandler;
		}

		public void StopTracking()
		{
			if( this.TrackingGeolocator == null )
			{
				return;
			}

			this.TrackingGeolocator = null;
			MyGeoPosition = null;
		}

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
			if( _lockToMyPos )
			{
				SmartDispatcher.BeginInvoke( () =>
				{
					_myMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 14 );
				} );
			}
		}


	}
}
