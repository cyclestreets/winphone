using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cyclestreets.CustomMapTypes
{
	class OpenStreetMap : TileSource
    {
		public OpenStreetMap()
        {
            //MapType = GoogleType.Street;
			UriFormat = "http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png";
        }

		private readonly static string[] TilePathPrefixes = new[] { "a", "b", "c" };
 
        public override Uri GetUri(int x, int y, int zoomLevel)
        {
			if (zoomLevel > 0)
			{
				var url = string.Format(UriFormat, TilePathPrefixes[y % 3], zoomLevel, x, y);
				return new Uri(url);
			}
			return null;
        }
    } 
}
