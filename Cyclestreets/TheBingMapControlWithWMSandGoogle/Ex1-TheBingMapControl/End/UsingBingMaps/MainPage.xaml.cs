// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Device.Location;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;

namespace UsingBingMaps
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region Consts

        /// <value>Default map zoom level.</value>
        private const double DefaultZoomLevel = 14.0;

        /// <value>Maximum map zoom level allowed.</value>
        private const double MaxZoomLevel = 21.0;

        /// <value>Minimum map zoom level allowed.</value>
        private const double MinZoomLevel = 1.0;

        #endregion

        #region Fields

        /// <value>Provides credentials for the map control.</value>
        private readonly CredentialsProvider _credentialsProvider = new ApplicationIdCredentialsProvider(App.Id);

        /// <value>Default location coordinate.</value>
        private static readonly GeoCoordinate DefaultLocation = new GeoCoordinate(49.320574, 16.68942);

        /// <value>Map zoom level.</value>
        private double _zoom;

        /// <value>Map center coordinate.</value>
        private GeoCoordinate _center;

        #endregion

        #region Properties

        public bool HasDirections
        {
            get
            {
                // TODO : Return true only if has directions.

                return true;
            }
        }

        /// <summary>
        /// Gets the credentials provider for the map control.
        /// </summary>
        public CredentialsProvider CredentialsProvider
        {
            get { return _credentialsProvider; }
        }

        /// <summary>
        /// Gets or sets the map zoom level.
        /// </summary>
        public double Zoom
        {
            get { return _zoom; }
            set
            {
                var coercedZoom = Math.Max(MinZoomLevel, Math.Min(MaxZoomLevel, value));
                if (_zoom != coercedZoom)
                {
                    _zoom = value;
                    NotifyPropertyChanged("Zoom");
                }
            }
        }

        /// <summary>
        /// Gets or sets the map center location coordinate.
        /// </summary>
        public GeoCoordinate Center
        {
            get { return _center; }
            set
            {
                if (_center != value)
                {
                    _center = value;
                    NotifyPropertyChanged("Center");
                }
            }
        }

        #endregion

        #region Tasks

        private void InitializeDefaults()
        {
            // TODO : Initialize default properties.
        }

        private void ChangeMapMode()
        {
            // TODO : Change map view mode.
            if (Map.Mode is AerialMode)
            {
                Map.Mode = new RoadMode();
            }
            else
            {
                Map.Mode = new AerialMode(true);
            }
        }

        private void CenterLocation()
        {
            // Center map to default location.
            Center = DefaultLocation;

            // Reset zoom default level.
            Zoom = DefaultZoomLevel;
        }

        private void CenterPushpinsPopup(Point touchPoint)
        {
            // TODO : Center pushpins popup to the touch point.
        }

        private void CreateNewPushpin(object selectedItem, Point point)
        {
            // TODO : Create a new pushpin.
        }

        private void CalculateRoute()
        {
            // TODO : Calculate a route.
        }

        #endregion
    }
}
