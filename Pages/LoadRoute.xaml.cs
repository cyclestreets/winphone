using System.Windows;
using Windows.Storage;
using Cyclestreets.Annotations;
using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Polenter.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Controls;

namespace Cyclestreets.Pages
{
    [UsedImplicitly]
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
            saveFiles.ItemsSource = names.Select(n => n.Remove(n.Length - 6)).ToList();
        }

        private async void saveFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RouteManager rm = SimpleIoc.Default.GetInstance<RouteManager>();

#if false
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
#else
            try
            {
                // Get the local folder.
                StorageFolder local = ApplicationData.Current.LocalFolder;
                if (local != null)
                {
                    // Get the file.

                    // Read the data.
                    using (var file = await local.OpenStreamForReadAsync(e.AddedItems[0] + @".route"))
                    {
                        var serializer = new SharpSerializer(false);

                        file.Seek(0, SeekOrigin.Begin);
                        // deserialize (to check the serialization)
                        var res = serializer.Deserialize(file);
                        rm.RouteCacheForSaving = (Dictionary<string, string>)res;

                        NavigationService.Navigate(new Uri("/Pages/RouteOverview.xaml?plan=saved", UriKind.Relative));
                        NavigationService.RemoveBackEntry();
                    }
                }
            }
            catch (Exception ex)
            {
                BugSense.BugSenseHandler.Instance.LogException(ex, @"SaveData", @"Could not load");
                MessageBox.Show("Could not load route. Either the save file is corrupt or it is from an earlier version of CycleStreets and is no longer supported.", 
                    "Load Error", MessageBoxButton.OK);
            }

#endif
        }
    }
}