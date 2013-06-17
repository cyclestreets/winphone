using System;
namespace Cyclestreets.Utils
{
	public class UtilTime
	{
		public const int ONE_SECOND = 1;
		public const int SECONDS = 60;

		public const int ONE_MINUTE = ONE_SECOND * SECONDS;
		public const int MINUTES = 60;

		public const int ONE_HOUR = ONE_MINUTE * MINUTES;
		public const int HOURS = 24;

		public const int ONE_DAY = ONE_HOUR * HOURS;

		/// <summary>
		/// Splits time (in milliseconds) into 4 values: (w) days, (x) hours, (y) minutes and (z) seconds"
		/// </summary>
		/// <param name="inDuration">The time to convert</param>
		/// <param name="timesOut">a previously allocated array of 4 ints where the output is stored.
		/// Array indices key: 0=day,1=hours,2=minutes,3=seconds</param>
		/// <returns>the first index filled in the array</returns>
		public static int secsToIntArray( double inDuration, int[] timesOut )
		{
			//RFDebug.AssertTrue( timesOut != null && timesOut.Length >= 4, "Unexpected" );

			timesOut[ 0 ] = 0;
			timesOut[ 1 ] = 0;
			timesOut[ 2 ] = 0;
			timesOut[ 3 ] = 0;

			int firstId = -1;

			int temp = 0;
			int duration = (int)inDuration;
			if( duration >= ONE_SECOND )
			{
				temp = duration / ONE_DAY;
				if( temp > 0 )
				{
					duration -= temp * ONE_DAY;
					timesOut[ 0 ] = (int)temp;
					firstId = 0;
				}

				temp = duration / ONE_HOUR;
				if( temp > 0 )
				{
					duration -= temp * ONE_HOUR;
					timesOut[ 1 ] = (int)temp;
					if( firstId == -1 )
						firstId = 1;
				}

				temp = duration / ONE_MINUTE;
				if( temp > 0 )
				{
					duration -= temp * ONE_MINUTE;
					timesOut[ 2 ] = (int)temp;
					if( firstId == -1 )
						firstId = 2;
				}

				temp = duration / ONE_SECOND;
				if( temp > 0 )
				{
					timesOut[ 3 ] = (int)temp;
					if( firstId == -1 )
						firstId = 3;
				}
			}
			else
			{
				// less than 1 second
				firstId = -1;
			}
			return firstId;
		}

		/// <summary>
		/// Converts time (in seconds) into a readable string "2HRS 30MINS" (w) days, (x) hours, (y) minutes and (z) seconds
		/// </summary>
		/// <param name="duration">The time to convert</param>
		/// <param name="numSignificantValues">set the max info to display, 1DAY,0HRS,30MINS,30SECS with numSignificantValues set to 2 will return the string "1DAY 30MINS" - 2 significant values</param>
		/// <returns>the formatted string</returns>
		public static string secsToShortString( double duration, int numSignificantValues )
		{
			int[] times = new int[ 4 ];

			duration = Math.Ceiling( (float)duration );

			int firstId = secsToIntArray( duration, times );
			string output = "";

			if( firstId != -1 )
			{
				int i = firstId;
				int count = 0;

				while( count < numSignificantValues && i < 4 )
				{
					if( times[ i ] > 0 ) // If this time has a value
					{
						if( count > 0 )
							output += " "; // add a separator
						output += times[ i ];
						switch( i )
						{
							case 0:
								output += "Day";
								break;
							case 1:
								output += "Hour";
								break;
							case 2:
								output += "Minute";
								break;
							case 3:
								output += "Second";
								break;
						}
						count++;
					}
					i++;
				}
			}
			else
			{
				output += "0"; // for values less than 1 second
			}
			times = null;
			return output;

		}

		/// <summary>
		/// converts time (in seconds) to human-readable format (w) days, (x) hours, (y) minutes and (z) seconds
		/// </summary>
		/// <param name="duration">time in seconds</param>
		/// <param name="timesOut">a previously allocated array of empty strings where the output is stored. 
		/// Array indices key: 0=day,1=hours,2=minutes,3=seconds</param>
		/// <returns>the number of full elements in the array</returns>
		public static int secsToStringArray( double duration, string[] timesOut )
		{
			//RFDebug.AssertTrue( timesOut != null && timesOut.Length >= 4, "Unexpected" );

			timesOut[ 0 ] = null;
			timesOut[ 1 ] = null;
			timesOut[ 2 ] = null;
			timesOut[ 3 ] = null;

			int count = 0;

			int temp = 0;
			if( duration >= ONE_SECOND )
			{
				temp = (int)Math.Floor( duration / ONE_DAY );
				if( temp > 0 )
				{
					duration -= temp * ONE_DAY;
					timesOut[ 0 ] = "" + temp;
					timesOut[ 0 ] += " " + "Day";
					timesOut[ 0 ] += ( ( temp > 1 ) ? "s" : "" );
					count++;
					// times[0] += ( ( duration >= ONE_MINUTE ) ? ", " : "" );
				}

				temp = (int)Math.Floor( duration / ONE_HOUR );
				if( temp > 0 )
				{
					duration -= temp * ONE_HOUR;
					timesOut[ 1 ] = "" + temp;
					timesOut[ 1 ] += " " + "Hour";
					timesOut[ 1 ] += ( ( temp > 1 ) ? "s" : "" );
					count++;
					// res = res + ( ( duration >= ONE_MINUTE ) ? ", " : "" );
				}

				temp = (int)Math.Floor( duration / ONE_MINUTE );
				if( temp > 0 )
				{
					duration -= temp * ONE_MINUTE;
					timesOut[ 2 ] = "" + temp;
					timesOut[ 2 ] += " " + "Minute";
					timesOut[ 2 ] += ( ( temp > 1 ) ? "s" : "" );
					count++;
				}

				temp = (int)Math.Floor( duration / ONE_SECOND );
				if( temp > 0 )
				{
					timesOut[ 3 ] = "" + temp;
					timesOut[ 3 ] += " " + "Second";
					timesOut[ 3 ] += ( ( temp > 1 ) ? "s" : "" );
					count++;
				}
			}
			else
			{
				timesOut[ 0 ] = null;
				timesOut[ 1 ] = null;
				timesOut[ 2 ] = null;
				timesOut[ 3 ] = "0 " + "Second";
				count = 1;
			}
			return count;
		}

		/**
		 * converts time (in milliseconds) to human-readable format (w) days, (x) hours, (y) minutes and (z) seconds
		 */
		public static string millisToLongDHMS( double duration )
		{
			return secsToLongDHMS( duration, 2 );
		}

		/// <summary>
		/// converts time (in seconds) to human-readable format (w) days, (x) hours, (y) minutes and (z) seconds
		/// </summary>
		/// <param name="duration">the time in seconds</param>
		/// <param name="maxValuesToDisplay">number of values to display starting with the largest possible.</param>
		/// <returns>the formatted string</returns>
		public static string secsToLongDHMS( double duration, int maxValuesToDisplay = 4 )
		{
			string[] times = new string[ 4 ];

			int total = secsToStringArray( duration, times );

			string res = "";
			int found = 0;
			for( int i = 0; i < times.Length; i++ )
			{
				if( times[ i ] != null )
				{
					res += times[ i ];

					found++;

					if( found < total && found < maxValuesToDisplay )
					{
						res += ", ";
					}
					else
					{
						break;
					}
				}
			}

			return res;
		}


	}
}
