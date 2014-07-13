using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets
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
	class ReverseGeocodeQueryManager
	{
		public static ReverseGeocodeQueryManager Instance { get; set; }
		List<ReverseGeocodeObject> handlers = new List<ReverseGeocodeObject>();

		public ReverseGeocodeQueryManager()
		{
			Instance = this;
		}

		public void Add( ReverseGeocodeHandler h )
		{
			ReverseGeocodeObject obj = new ReverseGeocodeObject(h);
			obj.query.QueryCompleted += geoQ_QueryCompleted;
			handlers.Add( obj );
			if( !handlers[ 0 ].query.IsBusy )
			{
				handlers[ 0 ].query.QueryAsync();
				App.networkStatus.networkIsBusy = true;
			}
		}

		private void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
		{
			ReverseGeocodeObject obj = handlers[ 0 ];
			handlers.Remove( obj );
			if( handlers.Count > 0 && !handlers[ 0 ].query.IsBusy )
			{
				handlers[ 0 ].query.QueryAsync();
				App.networkStatus.networkIsBusy = true;
			}
			else
			{
				App.networkStatus.networkIsBusy = false;
			}

			obj.handler.geoQ_QueryCompleted( sender, e );
		}
	}
}
