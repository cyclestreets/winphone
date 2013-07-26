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
using System.Windows.Input;
using System.Windows.Threading;

namespace UsingBingMaps.Helpers
{
    /// <summary>
    /// Helper class for simplifying the process of handling mouse events to mimic: Tap, Move and Hold gestures.
    /// </summary>
    public class TouchBehavior
    {
        #region Fields
        /// <value>The last touch point.</value>
        private Point _touchPoint;        

        /// <value>Touch and hold timer.</value>
        private DispatcherTimer _holdTimer; 
        #endregion

        #region Properties
        /// <summary>
        /// Gets the last touch point.
        /// </summary>
        public Point LastTouchPoint
        {
            get { return _touchPoint; }
        }
        #endregion

        #region Ctor
        public TouchBehavior(UIElement element)
        {
            element.MouseLeftButtonDown += new MouseButtonEventHandler(Element_MouseLeftButtonDown);
            element.MouseLeftButtonUp += new MouseButtonEventHandler(Element_MouseLeftButtonUp);
            element.MouseMove += Element_MouseMove;

            _holdTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };

            _holdTimer.Tick += HandleHold;
        }
        #endregion

        #region Events

        /// <summary>
        /// The user tapped on time on the screen.
        /// </summary>
        public event EventHandler<TouchBehaviorEventArgs> Tap = delegate { };

        /// <summary>
        /// The user moved her finger while touching the scren.
        /// </summary>
        public event EventHandler<TouchBehaviorEventArgs> Move = delegate { };

        /// <summary>
        /// The user touched the screen, waited a while and then raised his finger.
        /// </summary>
        public event EventHandler<TouchBehaviorEventArgs> Hold = delegate { };
        
        #endregion

        #region Privates
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _touchPoint = e.GetPosition(sender as UIElement);
            _holdTimer.Start();
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_holdTimer.IsEnabled)
            {
                _holdTimer.Stop();
                Tap(this, new TouchBehaviorEventArgs(_touchPoint));
            }
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.GetPosition(sender as UIElement) != _touchPoint)
            {
                _holdTimer.Stop();
                Move(this, new TouchBehaviorEventArgs(_touchPoint));
            }
        }

        private void HandleHold(object sender, EventArgs e)
        {
            _holdTimer.Stop();
            Hold(this, new TouchBehaviorEventArgs(_touchPoint));
        } 
        #endregion
    }
}
