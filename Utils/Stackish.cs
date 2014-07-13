using System.Collections.Generic;

namespace Cyclestreets.Utils
{
	public class Stackish<T> : List<T>
	{
		//private List<T> items = new List<T>();

		public void Push( T item )
		{
			this.Add( item );
		}
		public T Pop()
		{
			if( this.Count > 0 )
			{
				T temp = this[ this.Count - 1 ];
				this.RemoveAt( this.Count - 1 );
				return temp;
			}
			else
				return default( T );
		}

		public T Peek()
		{
			if( this.Count > 0 )
			{
				T temp = this[ this.Count - 1 ];
				return temp;
			}
			else
				return default( T );
		}
		public void Remove( int itemAtPosition )
		{
			this.RemoveAt( itemAtPosition );
		}
	}
}
