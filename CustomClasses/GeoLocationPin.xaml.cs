using System.Collections.Generic;
using System.Device.Location;
using System.Windows;
using Cyclestreets.Utils;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Tasks;

namespace Cyclestreets.CustomClasses
{
    public partial class GeoLocationPin
    {
        private GeoCoordinate _location;

        private string _locationName;

        private bool LocationFound
        {
            set
            {
                if (value)
                {
                    progress.Visibility = Visibility.Collapsed;
             
                pinText.Visibility = Visibility.Visible;
            }
                else
                {
                    pinText.Visibility = Visibility.Collapsed;

                    progress.Visibility = Visibility.Visible;
                }
        }
        }

        private string LocationName
        {
            get { return _locationName; }
            set
            {
                _locationName = value;
                pinText.Text = value;
            }
        }

        private GeoCoordinate Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public GeoLocationPin(GeoCoordinate coord)
        {
            InitializeComponent();

            this.Location = coord;

            pushpin.Location = coord;
            LocationFound = false;

            LookupLocation(coord);
        }

        private async void LookupLocation(GeoCoordinate coord)
        {
            MapLocation loc = await GeoUtils.StartReverseGeocode(coord);
        
            SmartDispatcher.BeginInvoke(() =>
            {
                LocationFound = true;

                if (loc == null || loc.GeoCoordinate == null ) return;
             
                if (string.IsNullOrWhiteSpace(loc.Information.Address.Street))
                    LocationName = loc.Information.Address.City + @", " + loc.Information.Address.PostalCode;
                else
                    LocationName = loc.Information.Address.Street;

                pushpin.Location = loc.GeoCoordinate;
            });
        }
    }
}
