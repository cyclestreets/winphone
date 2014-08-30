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

        public void StartTracking(PositionAccuracy accuracy = PositionAccuracy.High, double interval = 30000)
		{
			if( TrackingGeolocator != null )
			{
				return;
			}

            TrackingGeolocator = new Geolocator {ReportInterval = (uint) interval, DesiredAccuracy = accuracy};

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
