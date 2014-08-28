using Windows.Devices.Geolocation;

namespace Cyclestreets.Managers
{
	class LocationManager
	{
		public static LocationManager Instance;

		public Geoposition MyGeoPosition
		{
			get; private set;
		}
		public Geolocator TrackingGeolocator;

	    public LocationManager()
		{
			Instance = this;
		}

		public void StartTracking( uint accuracy = 30, double interval = 30000 )
		{
			if( TrackingGeolocator != null )
			{
				return;
			}

            TrackingGeolocator = new Geolocator {ReportInterval = (uint) interval, DesiredAccuracyInMeters = accuracy, DesiredAccuracy = PositionAccuracy.High};

		    // this implicitly starts the tracking operation
			TrackingGeolocator.PositionChanged += positionChangedHandler;
		}

		public void StopTracking()
		{
			if( TrackingGeolocator == null )
			{
				return;
			}

			TrackingGeolocator = null;
			MyGeoPosition = null;
		}

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
		}


	}
}
