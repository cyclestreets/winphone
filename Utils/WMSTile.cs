using System;
using System.Text;
using System.Windows;
using Microsoft.Phone.Maps.Controls;

namespace CycleStreets.Util
{

	public static class Constants
	{

		public const double EarthMinLatitude = -85.05112878D;
		public const double EarthMaxLatitude = 85.05112878D;
		public const double EarthMinLongitude = -180D;
		public const double EarthMaxLongitude = 180D;
		public const double EarthCircumference = EarthRadius * 2 * Math.PI;
		public const double HalfEarthCircumference = EarthCircumference / 2;
		public const double EarthRadius = 6378137;

		public const double ProjectionOffset = EarthCircumference * 0.5;

		public const double INCH_TO_METER = 0.0254D;
		public const double METER_TO_INCH = 39.3700787D;
		public const double METER_TO_MILE = 0.000621371192D;
		public const double MILE_TO_METER = 1609.344D;
	}
	/// <summary>
	/// BBOx
	/// </summary>
	public class BBox
	{
		public int x;
		public int y;
		public int width;
		public int height;

		public BBox( int x, int y, int width, int height )
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}
	}

	/// <summary>
	/// WMSTile
	/// </summary>
	public class WMSTile : TileSource
	{
		public const int TILE_SIZE = 256;

		public WMSTile()
			: base( @"http://mapserver-slp.mendelu.cz/cgi-bin/mapserv?map=/var/local/slp/krtinyWMS.map&REQUEST=GetMap&SERVICE=wms&VERSION=1.1.1&SRS=EPSG:4326&WIDTH={4}&HEIGHT={4}&FORMAT=image/png&BBOX={0},{1},{2},{3}&LAYERS=typologie,hm2003&Transparent=true" )
		{
			//:base(@"http://wms.cuzk.cz/wms.asp?REQUEST=GetMap&SERVICE=wms&VERSION=1.1.1&SRS=EPSG:4326&WIDTH={4}&HEIGHT={4}&FORMAT=image/png&BBOX={0},{1},{2},{3}&LAYERS=KN") {


		}

		private Point Mercator( double lon, double lat )
		{
			/* spherical mercator for Google, VE, Yahoo etc
			 * epsg:900913 R= 6378137
			 * x = long
			 * y= R*ln(tan(pi/4 +lat/2)
			 */
			double x = 6378137.0 * Math.PI / 180 * lon;
			double y = 6378137.0 * Math.Log( Math.Tan( Math.PI / 180 * ( 45 + lat / 2.0 ) ) );
			return new Point( x, y );
		}
		/// <summary>
		///  Routine from DeepEarth 
		///  http://deepearth.codeplex.com/sourcecontrol/changeset/view/37324?projectName=deepearth#583728
		///  modified
		/// </summary>
		public override Uri GetUri( int tilePositionX, int tilePositionY, int tileLevel )
		{

			int zoom = tileLevel; //SSU tileLevel would be same as zoom in Bing control
			string quadKey = TileXYToQuadKey( tilePositionX, tilePositionY, zoom );

			// Use the quadkey to determine a bounding box for the requested tile
			BBox boundingBox = QuadKeyToBBox( quadKey );

			double deltaX = 0.00135; //SSU deltaX for SLP WMS
			double deltaY = 0.00058; //SSU deltaY for SLP WMS
			// Get the lat longs of the corners of the box
			double lon = XToLongitudeAtZoom( boundingBox.x * TILE_SIZE, 18 ) + deltaX;
			double lat = YToLatitudeAtZoom( boundingBox.y * TILE_SIZE, 18 ) + deltaY;

			double lon2 = XToLongitudeAtZoom( ( boundingBox.x + boundingBox.width ) * TILE_SIZE, 18 ) + deltaX;
			double lat2 = YToLatitudeAtZoom( ( boundingBox.y - boundingBox.height ) * TILE_SIZE, 18 ) + deltaY;

			Point mercPointA = Mercator( lon, lat );
			Point mercPointB = Mercator( lon2, lat2 );

			string wmsUrl = string.Format( this.UriFormat, lon, lat, lon2, lat2, TILE_SIZE );

			return new Uri( wmsUrl );
		}


		public BBox QuadKeyToBBox( string quadKey )
		{
			const int x = 0;
			const int y = 262144;
			return QuadKeyToBBox( quadKey, x, y, 1 );
		}

		/// <summary>
		/// Returns the bounding BBox for a grid square represented by the given quad key
		/// </summary>
		/// <param name="quadKey"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="zoomLevel"></param>
		/// <returns></returns>
		public BBox QuadKeyToBBox( string quadKey, int x, int y, int zoomLevel )
		{
			char c = quadKey[ 0 ];

			int tileSize = 2 << ( 18 - zoomLevel - 1 );

			if( c == '0' )
			{
				y = y - tileSize;
			}

			else if( c == '1' )
			{
				y = y - tileSize;
				x = x + tileSize;
			}

			else if( c == '3' )
			{
				x = x + tileSize;
			}

			if( quadKey.Length > 1 )
			{
				return QuadKeyToBBox( quadKey.Substring( 1 ), x, y, zoomLevel + 1 );
			}
			return new BBox( x, y, tileSize, tileSize );
		}

		/// <summary>
		/// Converts radians to degrees
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public double RadToDeg( double d )
		{
			return d / Math.PI * 180.0;
		}


		/// <summary>
		/// Converts a grid row to Latitude
		/// </summary>
		/// <param name="y"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public double YToLatitudeAtZoom( int y, int zoom )
		{
			double arc = Constants.EarthCircumference / ( ( 1 << zoom ) * TILE_SIZE );
			double metersY = Constants.HalfEarthCircumference - ( y * arc );
			double a = Math.Exp( metersY * 2 / Constants.EarthRadius );
			double result = RadToDeg( Math.Asin( ( a - 1 ) / ( a + 1 ) ) );
			return result;
		}

		/// <summary>
		/// Converts a grid column to Longitude
		/// </summary>
		/// <param name="x"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public double XToLongitudeAtZoom( int x, int zoom )
		{
			double arc = Constants.EarthCircumference / ( ( 1 << zoom ) * TILE_SIZE );
			double metersX = ( x * arc ) - Constants.HalfEarthCircumference;
			double result = RadToDeg( metersX / Constants.EarthRadius );
			return result;
		}
		private static string TileXYToQuadKey( int tileX, int tileY, int levelOfDetail )
		{
			var quadKey = new StringBuilder();
			for( int i = levelOfDetail; i > 0; i-- )
			{
				char digit = '0';
				int mask = 1 << ( i - 1 );
				if( ( tileX & mask ) != 0 )
				{
					digit++;
				}
				if( ( tileY & mask ) != 0 )
				{
					digit++;
					digit++;
				}
				quadKey.Append( digit );
			}
			return quadKey.ToString();
		}

	}



}
