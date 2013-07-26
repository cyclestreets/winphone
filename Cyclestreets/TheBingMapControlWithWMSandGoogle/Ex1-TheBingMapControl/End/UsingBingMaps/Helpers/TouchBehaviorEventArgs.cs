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

namespace UsingBingMaps.Helpers
{
    /// <summary>
    /// Touch behavior event arguments.
    /// </summary>
    public class TouchBehaviorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the touch point.
        /// </summary>
        public Point TouchPoint { get; private set; }

        /// <summary>
        /// Initializes a new instance of this type.
        /// </summary>
        /// <param name="touchPoint">The touch point.</param>
        public TouchBehaviorEventArgs(Point touchPoint)
        {
            TouchPoint = touchPoint;
        }
    }
}
