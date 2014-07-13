using System.Device.Location;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets
{
	interface ReverseGeocodeHandler
	{
		void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<System.Collections.Generic.IList<MapLocation>> e );
		GeoCoordinate GetGeoCoordinate();
	}
}
