﻿#pragma checksum "C:\Users\Dave\Documents\Visual Studio 2012\Projects\Cyclestreets\Cyclestreets\Pages\RouteOverview.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "EB39FB9A96FEC3A0661BC69F97EC86D9"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Cyclestreets {
    
    
    public partial class RouteOverview : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal Microsoft.Phone.Shell.ApplicationBarIconButton details;
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal Microsoft.Phone.Maps.Controls.Map MyMap;
        
        internal System.Windows.Controls.TextBlock shortest;
        
        internal System.Windows.Controls.TextBlock balanced;
        
        internal System.Windows.Controls.TextBlock fastest;
        
        internal System.Windows.Shapes.Rectangle fastest1;
        
        internal System.Windows.Shapes.Rectangle balanced1;
        
        internal System.Windows.Shapes.Rectangle fastest1_Copy;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/Cyclestreets;component/Pages/RouteOverview.xaml", System.UriKind.Relative));
            this.details = ((Microsoft.Phone.Shell.ApplicationBarIconButton)(this.FindName("details")));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.MyMap = ((Microsoft.Phone.Maps.Controls.Map)(this.FindName("MyMap")));
            this.shortest = ((System.Windows.Controls.TextBlock)(this.FindName("shortest")));
            this.balanced = ((System.Windows.Controls.TextBlock)(this.FindName("balanced")));
            this.fastest = ((System.Windows.Controls.TextBlock)(this.FindName("fastest")));
            this.fastest1 = ((System.Windows.Shapes.Rectangle)(this.FindName("fastest1")));
            this.balanced1 = ((System.Windows.Shapes.Rectangle)(this.FindName("balanced1")));
            this.fastest1_Copy = ((System.Windows.Shapes.Rectangle)(this.FindName("fastest1_Copy")));
        }
    }
}

