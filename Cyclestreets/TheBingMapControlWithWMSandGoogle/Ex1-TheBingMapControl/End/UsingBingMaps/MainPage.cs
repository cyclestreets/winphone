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
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

using UsingBingMaps.Helpers;

namespace UsingBingMaps
{
    /// <summary>
    /// This partial class contains startup code for implementing the bing maps lab,
    /// so you can concentrate on the Bing map control and services and not on the
    /// UI behavior or application flow.
    /// <remarks>
    /// User code should be added to the MainPage.xaml.cs file only.
    /// </remarks>
    public partial class MainPage : INotifyPropertyChanged
    {
        #region Fields

        /// <value>Helper for handling mouse (touch) events.</value>
        private readonly TouchBehavior _touchBehavior;

        /// <value>Helper for handling visual states.</value>
        private readonly VisualStates _visualStates;

        #endregion

        #region Ctor

        public MainPage()
        {
            InitializeDefaults();
            InitializeComponent();

            _touchBehavior = new TouchBehavior(this);
            _touchBehavior.Tap += touchBehavior_Tap;
            _touchBehavior.Move += touchBehavior_Move;
            _touchBehavior.Hold += touchBehavior_Hold;

            _visualStates = new VisualStates(this);

            DataContext = this;
        }

        #endregion

        #region Event Handlers

        private void ButtonLocation_Click(object sender, EventArgs e)
        {
            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;

            CenterLocation();
        }

        private void ButtonRoute_Click(object sender, EventArgs e)
        {
            // Display or hide route panel.
            if (_visualStates.RouteState == VisualStates.HideRoute)
            {
                _visualStates.RouteState = VisualStates.ShowRoute;
            }
            else
            {
                _visualStates.RouteState = VisualStates.HideRoute;
            }

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void ButtonDirections_Click(object sender, EventArgs e)
        {
            // Display or hide directions panel.
            if (_visualStates.DirectionsState == VisualStates.HideDirections)
            {
                if (HasDirections)
                {
                    _visualStates.DirectionsState = VisualStates.ShowDirections;
                }
            }
            else
            {
                _visualStates.DirectionsState = VisualStates.HideDirections;
            }

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void ButtonMode_Click(object sender, EventArgs e)
        {
            ChangeMapMode();

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void touchBehavior_Tap(object sender, TouchBehaviorEventArgs e)
        {
            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void touchBehavior_Move(object sender, TouchBehaviorEventArgs e)
        {
            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void touchBehavior_Hold(object sender, TouchBehaviorEventArgs e)
        {
            CenterPushpinsPopup(e.TouchPoint);

            // Show pushpins panel.
            _visualStates.PushpinsState = VisualStates.ShowPushpins;
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            CalculateRoute();

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void ButtonZoomIn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Zoom += 1;

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void ButtonZoomOut_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Zoom -= 1;

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        private void ListBoxPushpinCatalog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = sender as Selector;
            if (selector == null || selector.SelectedItem == null)
            {
                return;
            }

            CreateNewPushpin(selector.SelectedItem, _touchBehavior.LastTouchPoint);

            // Reset the selected item so we can pick it again next time.
            selector.SelectedItem = null;

            // Hide pushpins panel.
            _visualStates.PushpinsState = VisualStates.HidePushpins;
        }

        #endregion

        #region Privates

        /// <summary>
        /// Hides the route view and display the directions view.
        /// </summary>
        private void ShowDirectionsView()
        {
            _visualStates.RouteState = VisualStates.HideRoute;

            if (HasDirections)
            {
                _visualStates.DirectionsState = VisualStates.ShowDirections;
            }
        }

        #endregion

        #region Property Changed

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Visual States
        /// <summary>
        /// An internal helper class for handling MainPage visual state transitions.
        /// </summary>
        private class VisualStates
        {
            #region Predefined Visual States
            // Route States
            public const string ShowRoute = "ShowRoute";
            public const string HideRoute = "HideRoute";

            // Directions States
            public const string ShowDirections = "ShowDirections";
            public const string HideDirections = "HideDirections";

            // Pushpins States
            public const string ShowPushpins = "ShowPushpins";
            public const string HidePushpins = "HidePushpins";
            #endregion

            #region Fields
            private readonly Control _control;
            private string _routeState = VisualStates.HideRoute;
            private string _directionsState = VisualStates.HideDirections;
            private string _pushpinsState = VisualStates.HidePushpins;
            #endregion

            #region Properties
            /// <summary>
            /// Change the route panel visual state.
            /// </summary>
            public string RouteState
            {
                get { return _routeState; }
                set
                {
                    if (_routeState != value)
                    {
                        _routeState = value;
                        VisualStateManager.GoToState(_control, value, true);
                    }
                }
            }

            /// <summary>
            /// Change the directions panel visual state.
            /// </summary>
            public string DirectionsState
            {
                get { return _directionsState; }
                set
                {
                    if (_directionsState != value)
                    {
                        _directionsState = value;
                        VisualStateManager.GoToState(_control, value, true);
                    }
                }
            }

            /// <summary>
            /// Change the pushpins popup visual state.
            /// </summary>
            public string PushpinsState
            {
                get { return _pushpinsState; }
                set
                {
                    if (_pushpinsState != value)
                    {
                        _pushpinsState = value;
                        VisualStateManager.GoToState(_control, value, true);
                    }
                }
            }
            #endregion

            #region Ctor
            /// <summary>
            /// Initializes a new instance of this class.
            /// </summary>
            /// <param name="control">The target control.</param>
            public VisualStates(Control control)
            {
                _control = control;
            }
            #endregion
        }
        #endregion
    }
}
