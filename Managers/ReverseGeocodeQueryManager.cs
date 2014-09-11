using Cyclestreets.Objects;
using Microsoft.Phone.Maps.Services;
using System.Collections.Generic;

namespace Cyclestreets.Managers
{
    /// <summary>
    /// Used for bulk reverse geocoding
    /// </summary>
    class ReverseGeocodeQueryManager
    {
        readonly List<ReverseGeocodeObject> _handlers = new List<ReverseGeocodeObject>();
        public static readonly ReverseGeocodeQueryManager Instance = new ReverseGeocodeQueryManager();

        private ReverseGeocodeQueryManager()
        {
        }

        public void Add(ReverseGeocodeHandler h)
        {
            ReverseGeocodeObject obj = new ReverseGeocodeObject(h);
            obj.query.QueryCompleted += geoQ_QueryCompleted;
            _handlers.Add(obj);
            if (!_handlers[0].query.IsBusy)
            {
                _handlers[0].query.QueryAsync();
                App.networkStatus.NetworkIsBusy = true;
            }
        }

        private void geoQ_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            ReverseGeocodeObject obj = _handlers[0];
            _handlers.Remove(obj);
            if (_handlers.Count > 0 && !_handlers[0].query.IsBusy)
            {
                _handlers[0].query.QueryAsync();
                App.networkStatus.NetworkIsBusy = true;
            }
            else
            {
                App.networkStatus.NetworkIsBusy = false;
            }

            obj.handler.geoQ_QueryCompleted(sender, e);
        }
    }
}