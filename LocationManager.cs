using Cyclestreets.Utils;
using Microsoft.Phone.Maps.Controls;
using Windows.Devices.Geolocation;

namespace Cyclestreets
{
	class LocationManager
	{
		public static LocationManager instance;

		public Geoposition MyGeoPosition
		{
			get;
			set;
		}
		public Geolocator trackingGeolocator;

		private Map MyMap;
		private bool lockToMyPos;

		public LocationManager( Map map )
		{
			instance = this;
			MyMap = map;
		}

		public void LockToMyPos( bool doLock )
		{
			lockToMyPos = doLock;
		}

		public void StartTracking( uint accuracy = 30, double interval = 30000 )
		{
			if( this.trackingGeolocator != null )
			{
				return;
			}

			this.trackingGeolocator = new Geolocator();
			this.trackingGeolocator.ReportInterval = (uint)interval;
			//this.trackingGeolocator.DesiredAccuracy = PositionAccuracy.High;
			this.trackingGeolocator.DesiredAccuracyInMeters = accuracy;

			// this implicitly starts the tracking operation
			this.trackingGeolocator.PositionChanged += positionChangedHandler;
		}

		public void StopTracking()
		{
			if( this.trackingGeolocator == null )
			{
				return;
			}

			this.trackingGeolocator = null;
			MyGeoPosition = null;
		}

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
			if( lockToMyPos )
			{
				SmartDispatcher.BeginInvoke( () =>
				{
					MyMap.SetView( CoordinateConverter.ConvertGeocoordinate( MyGeoPosition.Coordinate ), 14 );
				} );
			}
		}


	}
}
