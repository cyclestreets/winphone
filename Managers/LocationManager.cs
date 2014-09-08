using System.ComponentModel;
using Windows.Devices.Geolocation;
using Windows.Foundation;

namespace Cyclestreets.Managers
{
	class LocationManager
	{
		public static LocationManager Instance = new LocationManager();

		public Geoposition MyGeoPosition
		{
			get; private set;
		}
		public Geolocator TrackingGeolocator;

	    private LocationManager()
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

        public event TypedEventHandler<Geolocator, PositionChangedEventArgs> PositionChanged;

		private void positionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
			MyGeoPosition = args.Position;
            if (PositionChanged != null)
                PositionChanged(sender, args);
		}


	}
}
