using System.Collections.Generic;
using System.Device.Location;
using Cyclestreets.Common;
using Cyclestreets.Managers;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets.Objects
{
    public class POI : BindableBase, ReverseGeocodeHandler
    {
        public string Name { get; set; }

        GeoCoordinate _position;
        public GeoCoordinate Position
        {
            get { return _position; }
            set
            {
                _position = value;

                ReverseGeocodeQueryManager.Instance.Add( this );
            }
        }

        public GeoCoordinate GetGeoCoordinate()
        {
            return _position;
        }

        public void geoQ_QueryCompleted( object sender, QueryCompletedEventArgs<IList<MapLocation>> e )
        {
            MapLocation loc = e.Result[ 0 ];
            Location = loc.Information.Address.Street + ", " + loc.Information.Address.PostalCode;
        }

        public string Distance { get; set; }
        private string _location = "...";

        private string Location
        {
            get
            { return _location; }

            set
            {
                this.SetProperty( ref this._location, value.TrimStart( new[] { ',', ' ' } ) );
            }
        }

        public string PinID { get; set; }

        public string BgColour { get; set; }
    }
}