using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cyclestreets.CustomMapTypes
{
	public static class BindingHelpers
	{
		//Used for binding a single TileSource object to a Bing Maps control
		#region TileSourceProperty

		// Name, Property type, type of object that hosts the property, method to call when anything changes
		public static readonly DependencyProperty TileSourceProperty =
			DependencyProperty.RegisterAttached("TileSource", typeof(TileSource),
			typeof(BindingHelpers), new PropertyMetadata(SetTileSourceCallback));

		// Called when TileSource is retrieved
		public static TileSource GetTileSource(DependencyObject obj)
		{
			return obj.GetValue(TileSourceProperty) as TileSource;
		}

		// Called when TileSource is set
		public static void SetTileSource(DependencyObject obj, TileSource value)
		{
			obj.SetValue(TileSourceProperty, value);
		}

		//Called when TileSource is set
		private static void SetTileSourceCallback(object sender, DependencyPropertyChangedEventArgs args)
		{

			var map = sender as Map;
			var newSource = args.NewValue as TileSource;
			if (newSource == null || map == null) return;

			// Remove existing layer(s)
			for (var i = map.Children.Count - 1; i >= 0; i--)
			{
				var tileLayer = map.Children[i] as MapTileLayer;
				if (tileLayer != null)
				{
					map.Children.RemoveAt(i);
				}
			}

			var newLayer = new MapTileLayer();
			newLayer.TileSources.Add(newSource);
			map.Children.Add(newLayer);
		}

		#endregion
	}
}
