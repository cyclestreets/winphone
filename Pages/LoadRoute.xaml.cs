using Cyclestreets.Managers;
using Cyclestreets.Resources;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Phone.Controls;
using Polenter.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    public partial class LoadRoute
    {
        public LoadRoute()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string[] names = IsolatedStorageFile.GetUserStoreForApplication().GetFileNames("*.route");
            saveFiles.ItemsSource = names.Select(n=>n.Remove(n.Length - 6)).ToList();
        }

        private void saveFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

            IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {

                var stream = new IsolatedStorageFileStream(e.AddedItems[0] + ".route", FileMode.Open, myFile);

                if (!myFile.FileExists(e.AddedItems[0].ToString()))
                {
                    MessageBox.Show(AppResources.SaveFileNotFound, AppResources.Error, MessageBoxButton.OK);
                    return;
                }

                var serializer = new SharpSerializer(true);

                // deserialize (to check the serialization)
                var res = serializer.Deserialize(stream);
                rm.RouteCacheForSaving = (Dictionary<string, string>)res;
                stream.Close();

                NavigationService.Navigate(new Uri("/Pages/RouteOverview.xaml?plan=saved", UriKind.Relative));
                NavigationService.RemoveBackEntry();
            }
            catch
            {
                MessageBox.Show(AppResources.SaveFileNotFound, AppResources.Error, MessageBoxButton.OK);
            }

            

        }
    }
}