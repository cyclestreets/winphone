using System;

namespace Cyclestreets
{
	public class Geodesy
	{
		public static double RadToDeg( double radians )
		{
			return radians * ( 180 / Math.PI );
		}

		public static double DegToRad( double degrees )
		{
			return degrees * ( Math.PI / 180 );
		}

		public static double Bearing( double lat1, double long1, double lat2, double long2 )
		{
			//Convert input values to radians  
			lat1 = Geodesy.DegToRad( lat1 );
			long1 = Geodesy.DegToRad( long1 );
			lat2 = Geodesy.DegToRad( lat2 );
			long2 = Geodesy.DegToRad( long2 );

			double deltaLong = long2 - long1;

			double y = Math.Sin( deltaLong ) * Math.Cos( lat2 );
			double x = Math.Cos( lat1 ) * Math.Sin( lat2 ) -
					Math.Sin( lat1 ) * Math.Cos( lat2 ) * Math.Cos( deltaLong );
			double bearing = Math.Atan2( y, x );
			return Geodesy.ConvertToBearing( Geodesy.RadToDeg( bearing ) );
		}

		public static double ConvertToBearing( double deg )
		{
			return ( deg + 360 ) % 360;
		}
	}

	class Utils
	{
	}
}
