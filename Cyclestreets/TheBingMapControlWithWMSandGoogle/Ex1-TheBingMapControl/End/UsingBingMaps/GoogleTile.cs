/// Deep Earth is a community project available under the Microsoft Public License (Ms-PL)
/// Code is provided as is and with no warrenty – Use at your own risk
/// View the project and the latest code at http://codeplex.com/deepearth/
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls.Maps;
using System.Text;


namespace UsingBingMaps
{
    public enum GoogleMapModes
    {
        Street,
        Satellite,
        SatelliteHybrid,
        Physical,
        PhysicalHybrid, 
        StreetOverlay,
        StreetWaterOverlay
    }

    public class GoogleTile : TileSource 
    {       
        private const string TilePathBase = @"http://mt{0}.google.com/vt/lyrs={1}&z={2}&x={3}&y={4}";

        private const string charStreet = "m";
        private const string charSatellite = "s";
        private const string charSatelliteHybrid = "y";
        private const string charPhysical = "t";
        private const string charPhysicalHybrid = "p";
        private const string charStreetOverlay = "h";
        private const string charStreetWaterOverlay = "r";
 
        private GoogleMapModes MapMode = GoogleMapModes.SatelliteHybrid;

        private int server_rr = 0;

        //Constructor Called by XAML instanciation; Wait for MapMode to be set to initialize services
        public GoogleTile()
        {
        }

        public override Uri GetUri(int tilePositionX, int tilePositionY, int tileLevel) {
                int zoom = tileLevel;
                

                string url = string.Empty;
                server_rr = (server_rr + 1) % 4;

                switch (MapMode)
                {
                    case GoogleMapModes.Street:                        
                        url = XYZUrl(TilePathBase, server_rr, charStreet, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.Satellite:
                        url = XYZUrl(TilePathBase, server_rr, charSatellite, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.SatelliteHybrid:
                        url = XYZUrl(TilePathBase, server_rr, charSatelliteHybrid, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.Physical:
                        url = XYZUrl(TilePathBase, server_rr, charPhysical, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.PhysicalHybrid:
                        url = XYZUrl(TilePathBase, server_rr, charPhysicalHybrid, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.StreetOverlay:
                        url = XYZUrl(TilePathBase, server_rr, charStreetOverlay, zoom, tilePositionX, tilePositionY);
                        break;
                    case GoogleMapModes.StreetWaterOverlay:
                        url = XYZUrl(TilePathBase, server_rr, charStreetWaterOverlay, zoom, tilePositionX, tilePositionY);
                        break; 
                }

                return new Uri(url);
   
        }

        private static string XYZUrl(string url, int server, string mapmode, int zoom, int tilePositionX, int tilePositionY)
        {
            url = string.Format(url, server, mapmode, zoom, tilePositionX, tilePositionY);

            return url;
        }     

        
    }
}