using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Cyclestreets.Managers;
using Cyclestreets.Utils;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Cyclestreets
{
    public partial class RouteOverview : PhoneApplicationPage
    {
        private string currentPlan;
        public RouteOverview()
        {
            InitializeComponent();

            // Setup route type dropdown
            string plan = SettingManager.instance.GetStringValue("defaultRouteType", "balanced");
            currentPlan = plan.Replace(" route", "");
        }

        private async void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "823e41bf-889c-4102-863f-11cfee11f652";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "xrQJghWalYn52fTfnUhWPQ";

            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();
            if (PhoneApplicationService.Current.State.ContainsKey("loadedRoute") && PhoneApplicationService.Current.State["loadedRoute"] != null)
            {
                string routeData = (string)PhoneApplicationService.Current.State["loadedRoute"];
                rm.ParseRouteData(routeData, currentPlan, false);
            }

            var newplan = rm.HasCachedRoute(currentPlan);
            if (newplan == null) return;
            currentPlan = newplan;
            bool result = await rm.FindRoute(newplan, false);
            if (!result)
            {
                MarkedUp.AnalyticClient.Error("Route Planning Error");

                MessageBox.Show(
                    "Could not parse route data information from server. Please let us know about this error with the route you were trying to plan");
            }
            else
            {
                MapUtils.PlotCachedRoute(MyMap,currentPlan);
            }
        }

        private void MyMap_Tap(object sender, GestureEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void details_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/DirectionsResults.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}