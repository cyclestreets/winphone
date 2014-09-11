using Microsoft.Phone.Maps.Services;

namespace Cyclestreets.Objects
{
	class ReverseGeocodeObject
	{
		public ReverseGeocodeHandler handler;
		public ReverseGeocodeQuery query;

		public ReverseGeocodeObject( ReverseGeocodeHandler handler )
		{
			this.handler = handler;

			query = new ReverseGeocodeQuery();
			query.GeoCoordinate = handler.GetGeoCoordinate();
		}

		
		
	}
}
