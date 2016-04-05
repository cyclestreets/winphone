using Cyclestreets.Managers;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;

namespace Cyclestreets.Pages
{


    public partial class DirectionsResults : PhoneApplicationPage
    {
        public DirectionsResults()
        {
            InitializeComponent();

            //DirectionList.ItemsSource = DirectionsPage.route.segments;
            //  factsList.ItemsSource = DirectionsPage.Facts;

            // hack. See here http://stackoverflow.com/questions/5334574/applicationbariconbutton-is-null/5334703#5334703
            saveButton = ApplicationBar.Buttons[0] as ApplicationBarIconButton;
            saveButton.Text = AppResources.DirectionsResults_DirectionsResults_Save;
            share = ApplicationBar.Buttons[1] as ApplicationBarIconButton;
            share.Text = AppResources.DirectionsResults_DirectionsResults_Share;
            routeFeedback = ApplicationBar.Buttons[2] as ApplicationBarIconButton;
            routeFeedback.Text = AppResources.DirectionsResults_DirectionsResults_Route_Feedback;

            //heightChart.DataContext = DirectionsPage.route;
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            //PhoneApplicationService.Current.State[ "route" ] = currentRouteData;
            NavigationService.Navigate(new Uri("/Pages/SaveRoute.xaml", UriKind.Relative));
        }

        private void share_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/ShareChoice.xaml", UriKind.Relative));
        }

        private void routeFeedback_Click(object sender, EventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

            NavigationService.Navigate(new Uri("/Pages/Feedback.xaml?routeID=" + rm.Overview.RouteNumber , UriKind.Relative));
        }


    }
}