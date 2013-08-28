﻿using System;
using Microsoft.Phone.Maps.Controls;

namespace Cyclestreets.Pages
{
	public abstract class MyTileSource : TileSource
	{
		protected MyTileSource( string uriFormat )
			: base( uriFormat )
		{

		}

		public override Uri GetUri( int x, int y, int zoomLevel )
		{
			return new Uri( UriFormat.
				Replace( "{x}", x.ToString() ).
				Replace( "{y}", y.ToString() ).
				Replace( "{z}", zoomLevel.ToString() ) );
		}
	}

	public class OCMTileSource : MyTileSource
	{
		public OCMTileSource()
			: base( "http://tile.cyclestreets.net/opencyclemap/{z}/{x}/{y}.png" )
		{

		}
	}

	public class OSMTileSource : MyTileSource
	{
		public OSMTileSource()
			: base( "http://tile.cyclestreets.net/mapnik/{z}/{x}/{y}.png" )
		{

		}
	}
}