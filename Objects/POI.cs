using System.Collections.Generic;
using System.Device.Location;
using Cyclestreets.Common;
using Cyclestreets.Managers;
using Microsoft.Phone.Maps.Services;

namespace Cyclestreets.Objects
{
    public class POI : BindableBase
    {
        public string Name { get; set; }

        GeoCoordinate _position;
        public GeoCoordinate Position
        {
            get { return _position; }
            set
            {
                _position = value;
            }
        }

        public GeoCoordinate GetGeoCoordinate()
        {
            return _position;
        }

        public string Distance { get; set; }
        private string _location = @"...";

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